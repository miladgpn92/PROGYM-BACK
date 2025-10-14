using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common;
using Common.Enums;
using DariaCMS.Common;
using Data;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SharedModels.Dtos.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Services.Services.CMS.Gym
{
    public class GymFileService : IScopedDependency, IGymFileService
    {
        private const long DefaultLimitBytes = 1024L * 1024L * 1024L;
        private const int MaxImageDimension = 1920;

        private readonly ApplicationDbContext _dbContext;
        private readonly IRepository<Entities.Gym> _gymRepo;
        private readonly IRepository<GymUser> _gymUserRepo;
        private readonly IRepository<GymFile> _fileRepo;
        private readonly IRepository<Practice> _practiceRepo;
        private readonly IMapper _mapper;

        public GymFileService(
            ApplicationDbContext dbContext,
            IRepository<Entities.Gym> gymRepo,
            IRepository<GymUser> gymUserRepo,
            IRepository<GymFile> fileRepo,
            IRepository<Practice> practiceRepo,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _gymRepo = gymRepo;
            _gymUserRepo = gymUserRepo;
            _fileRepo = fileRepo;
            _practiceRepo = practiceRepo;
            _mapper = mapper;
        }

        public async Task<ResponseModel<GymFilePagedResult>> GetListAsync(int gymId, int userId, GymFileListRequest request, CancellationToken cancellationToken)
        {
            request ??= new GymFileListRequest();
            request.Pager ??= new Pageres();
            request.Pager.Normalize();

            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<GymFilePagedResult>(false, null, "Access denied");

            var query = _fileRepo.TableNoTracking
                .Where(f => f.GymId == gymId)
                .OrderByDescending(f => f.UploadedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((request.Pager.PageNumber - 1) * request.Pager.PageSize)
                .Take(request.Pager.PageSize)
                .ProjectTo<GymFileDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var result = new GymFilePagedResult
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.Pager.PageNumber,
                PageSize = request.Pager.PageSize
            };

            return new ResponseModel<GymFilePagedResult>(true, result);
        }

        public async Task<ResponseModel<List<GymFileDto>>> UploadAsync(int gymId, int userId, GymFileUploadRequest request, CancellationToken cancellationToken)
        {
            if (request == null || request.Files == null || request.Files.Count == 0)
                return new ResponseModel<List<GymFileDto>>(false, null, "No files provided");

            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel<List<GymFileDto>>(false, null, "Access denied");

            var gym = await _gymRepo.Table.FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);
            if (gym == null)
                return new ResponseModel<List<GymFileDto>>(false, null, "Gym not found");

            if (gym.FileStorageLimitBytes <= 0)
                gym.FileStorageLimitBytes = DefaultLimitBytes;

            var gymRoot = GetGymRoot(gymId);
            Directory.CreateDirectory(gymRoot);

            long usageDelta = 0;
            var savedEntities = new List<GymFile>();

            foreach (var file in request.Files.Where(f => f != null && f.Length > 0))
            {
                var extension = Path.GetExtension(file.FileName);
                var isImage = IsImage(extension);
                var storedFileName = GenerateStoredFileName(isImage, extension);
                var relativePath = BuildRelativePath(gymId, storedFileName);
                var physicalPath = Path.Combine(gymRoot, storedFileName);

                long sizeBytes;
                int? width = null;
                int? height = null;
                string contentType;

                if (isImage)
                {
                    var encoded = await EncodeImageAsync(file, cancellationToken);
                    sizeBytes = encoded.Data.LongLength;
                    if (ExceedsLimit(gym.FileUsageBytes, usageDelta, sizeBytes, gym.FileStorageLimitBytes))
                        return new ResponseModel<List<GymFileDto>>(false, null, "Storage limit exceeded");

                    await System.IO.File.WriteAllBytesAsync(physicalPath, encoded.Data, cancellationToken);
                    width = encoded.Width;
                    height = encoded.Height;
                    contentType = "image/webp";
                }
                else
                {
                    sizeBytes = file.Length;
                    if (ExceedsLimit(gym.FileUsageBytes, usageDelta, sizeBytes, gym.FileStorageLimitBytes))
                        return new ResponseModel<List<GymFileDto>>(false, null, "Storage limit exceeded");

                    await using var output = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await file.CopyToAsync(output, cancellationToken);
                    sizeBytes = new FileInfo(physicalPath).Length;
                    contentType = string.IsNullOrWhiteSpace(file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType;
                }

                usageDelta += sizeBytes;

                var entity = new GymFile
                {
                    GymId = gymId,
                    OriginalFileName = file.FileName,
                    StoredFileName = storedFileName,
                    RelativePath = relativePath,
                    ContentType = contentType,
                    SizeBytes = sizeBytes,
                    IsImage = isImage,
                    Width = width,
                    Height = height,
                    UploadedAt = DateTime.UtcNow,
                    UploadedByUserId = userId
                };

                await _fileRepo.AddAsync(entity, cancellationToken, saveNow: false);
                savedEntities.Add(entity);
            }

            if (usageDelta == 0)
                return new ResponseModel<List<GymFileDto>>(true, new List<GymFileDto>());

            gym.FileUsageBytes += usageDelta;

            await _dbContext.SaveChangesAsync(cancellationToken);

            var dtos = _mapper.Map<List<GymFileDto>>(savedEntities);
            return new ResponseModel<List<GymFileDto>>(true, dtos);
        }

        public async Task<ResponseModel> DeleteAsync(int gymId, int userId, GymFileDeleteRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.StoredFileName))
                return new ResponseModel(false, "File name is required");

            var hasAccess = await _gymUserRepo.TableNoTracking
                .AnyAsync(gu => gu.GymId == gymId && gu.UserId == userId && gu.Role == UsersRole.manager, cancellationToken);
            if (!hasAccess)
                return new ResponseModel(false, "Access denied");

            var gym = await _gymRepo.Table.FirstOrDefaultAsync(g => g.Id == gymId, cancellationToken);
            if (gym == null)
                return new ResponseModel(false, "Gym not found");

            var entity = await _fileRepo.Table
                .FirstOrDefaultAsync(f => f.GymId == gymId && f.StoredFileName == request.StoredFileName, cancellationToken);
            if (entity == null)
                return new ResponseModel(false, "File not found");

            var physicalPath = GetPhysicalPath(entity.RelativePath);
            if (System.IO.File.Exists(physicalPath))
                System.IO.File.Delete(physicalPath);

            gym.FileUsageBytes = Math.Max(0, gym.FileUsageBytes - entity.SizeBytes);

            var referencingPractices = await _practiceRepo.Table
                .Where(p => p.ThumbFileId == entity.Id || p.VideoFileId == entity.Id)
                .ToListAsync(cancellationToken);

            if (referencingPractices.Count > 0)
            {
                foreach (var practice in referencingPractices)
                {
                    if (practice.ThumbFileId == entity.Id)
                        practice.ThumbFileId = null;
                    if (practice.VideoFileId == entity.Id)
                        practice.VideoFileId = null;
                }

                await _practiceRepo.UpdateRangeAsync(referencingPractices, cancellationToken, saveNow: false);
            }

            await _fileRepo.DeleteAsync(entity, cancellationToken, saveNow: false);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return new ResponseModel(true, "");
        }

        private static bool ExceedsLimit(long currentUsage, long pendingUsage, long nextSize, long limit)
        {
            var effectiveLimit = limit > 0 ? limit : DefaultLimitBytes;
            return currentUsage + pendingUsage + nextSize > effectiveLimit;
        }

        private static bool IsImage(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                return false;

            switch (extension.ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                case ".gif":
                case ".webp":
                    return true;
                default:
                    return false;
            }
        }

        private static string GenerateStoredFileName(bool isImage, string extension)
        {
            if (isImage)
                return $"{Guid.NewGuid():N}.webp";

            var safeExtension = string.IsNullOrWhiteSpace(extension)
                ? ".bin"
                : extension.ToLowerInvariant();
            return $"{Guid.NewGuid():N}{safeExtension}";
        }

        private static string GetGymRoot(int gymId)
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "gym-files", gymId.ToString());
        }

        private static string BuildRelativePath(int gymId, string storedFileName)
        {
            var path = Path.Combine("gym-files", gymId.ToString(), storedFileName);
            return path.Replace("\\", "/");
        }

        private static string GetPhysicalPath(string relativePath)
        {
            relativePath = relativePath.TrimStart('/', '\\');
            var normalized = relativePath.Replace("/", Path.DirectorySeparatorChar.ToString());
            return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", normalized);
        }

        private static async Task<(byte[] Data, int Width, int Height)> EncodeImageAsync(IFormFile file, CancellationToken cancellationToken)
        {
            await using var input = file.OpenReadStream();
            using var image = await Image.LoadAsync<Rgba32>(input, cancellationToken);

            if (image.Width > MaxImageDimension || image.Height > MaxImageDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(MaxImageDimension, MaxImageDimension),
                    Mode = ResizeMode.Max
                }));
            }

            await using var memory = new MemoryStream();
            await image.SaveAsync(memory, new WebpEncoder { Quality = 80 }, cancellationToken);

            return (memory.ToArray(), image.Width, image.Height);
        }
    }
}

