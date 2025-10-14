using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AutoMapper;
using DariaCMS.Common;
using Entities;
using Microsoft.AspNetCore.Http;
using SharedModels.CustomMapping;

namespace SharedModels.Dtos.Shared
{
    public class GymFileDto : IHaveCustomMapping
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; }
        public string StoredFileName { get; set; }
        public string RelativePath { get; set; }
        public string ContentType { get; set; }
        public long SizeBytes { get; set; }
        public bool IsImage { get; set; }
        public int UploadedByUserId { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public DateTime UploadedAt { get; set; }

        public string RelativeUrl => "/" + RelativePath.Replace("\\", "/");

        public void CreateMappings(Profile profile)
        {
            profile.CreateMap<GymFile, GymFileDto>();
        }
    }

    public class GymFilePagedResult : PagedResult<GymFileDto>
    {
    }

    public class GymFileListRequest
    {
        public Pageres Pager { get; set; } = new() { PageNumber = 1, PageSize = 10 };
    }

    public class GymFileUploadRequest
    {
        [Required]
        public List<IFormFile> Files { get; set; } = new();
    }

    public class GymFileDeleteRequest
    {
        [Required]
        public string StoredFileName { get; set; }
    }
}
