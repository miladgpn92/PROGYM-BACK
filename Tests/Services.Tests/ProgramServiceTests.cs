using AutoMapper;
using Common.Enums;
using Data;
using Data.Repositories;
using Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Services.Services.CMS.Programs;
using SharedModels.CustomMapping;
using SharedModels.Dtos.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Services.Tests
{
    public class ProgramServiceTests
    {
        private const int GymId = 1;
        private const int ManagerUserId = 10;
        private const int PracticeCategoryId = 100;
        private const int PracticeAId = 1000;
        private const int PracticeBId = 1001;
        private const int PracticeCId = 1002;

        private readonly IMapper _mapper;

        public ProgramServiceTests()
        {
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddCustomMappingProfile(
                    typeof(ProgramDto).Assembly,
                    typeof(Entities.Program).Assembly);
            });
            _mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task CreateAsync_CreatesSupersetAndSingleRoutineItems()
        {
            using var context = CreateContext(nameof(CreateAsync_CreatesSupersetAndSingleRoutineItems));
            await SeedReferenceDataAsync(context);
            var service = CreateService(context);

            var dto = BuildDefaultProgramDto();

            var response = await service.CreateAsync(GymId, ManagerUserId, dto, CancellationToken.None);

            Assert.True(response.IsSuccess);
            var program = await context.Set<Entities.Program>()
                .Include(p => p.ProgramRoutineItems)
                    .ThenInclude(ri => ri.ProgramPractices)
                .SingleAsync();

            Assert.Equal(2, program.ProgramRoutineItems.Count);
            Assert.Equal(3, program.CountOfPractice);

            var superset = program.ProgramRoutineItems.Single(ri => ri.ItemType == ProgramRoutineItemType.Superset);
            Assert.Equal("Upper Body Blast", superset.Title);
            Assert.Equal(2, superset.ProgramPractices.Count);
            Assert.Equal(new[] { 1, 2 }, superset.ProgramPractices.OrderBy(pp => pp.InternalOrder).Select(pp => pp.InternalOrder));

            var single = program.ProgramRoutineItems.Single(ri => ri.ItemType == ProgramRoutineItemType.Single);
            Assert.Single(single.ProgramPractices);
            Assert.Equal(2, single.DisplayOrder);
        }

        [Fact]
        public async Task CreateAsync_ReturnsErrorWhenSupersetHasSingleMovement()
        {
            using var context = CreateContext(nameof(CreateAsync_ReturnsErrorWhenSupersetHasSingleMovement));
            await SeedReferenceDataAsync(context);
            var service = CreateService(context);

            var dto = new ProgramDto
            {
                Title = "Invalid Program",
                Type = ProgramTypes.Private,
                RoutineItems = new List<ProgramRoutineItemInputDto>
                {
                    new ProgramRoutineItemInputDto
                    {
                        ItemType = ProgramRoutineItemType.Superset,
                        Title = "Bad Superset",
                        RepeatCount = 2,
                        RestBetweenRepeats = 60,
                        Practices = new List<ProgramPracticeInputDto>
                        {
                            new ProgramPracticeInputDto
                            {
                                PracticeId = PracticeAId,
                                Type = PracticeType.Set,
                                SetCount = 3,
                                MovementCount = 10,
                                Rest = 45
                            }
                        }
                    }
                }
            };

            var response = await service.CreateAsync(GymId, ManagerUserId, dto, CancellationToken.None);

            Assert.False(response.IsSuccess);
            Assert.Contains("must include at least two movements", response.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CreateAsync_ReturnsErrorWhenSetPracticeMissingMovementCount()
        {
            using var context = CreateContext(nameof(CreateAsync_ReturnsErrorWhenSetPracticeMissingMovementCount));
            await SeedReferenceDataAsync(context);
            var service = CreateService(context);

            var dto = new ProgramDto
            {
                Title = "Invalid Practice Data",
                Type = ProgramTypes.Private,
                RoutineItems = new List<ProgramRoutineItemInputDto>
                {
                    new ProgramRoutineItemInputDto
                    {
                        ItemType = ProgramRoutineItemType.Single,
                        Practices = new List<ProgramPracticeInputDto>
                        {
                            new ProgramPracticeInputDto
                            {
                                PracticeId = PracticeAId,
                                Type = PracticeType.Set,
                                SetCount = 3,
                                Rest = 60
                            }
                        }
                    }
                }
            };

            var response = await service.CreateAsync(GymId, ManagerUserId, dto, CancellationToken.None);

            Assert.False(response.IsSuccess);
            Assert.Contains("movement count", response.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ReorderRoutineItemsAsync_ReordersDisplayOrder()
        {
            using var context = CreateContext(nameof(ReorderRoutineItemsAsync_ReordersDisplayOrder));
            await SeedReferenceDataAsync(context);
            var service = CreateService(context);

            var dto = BuildDefaultProgramDto();
            var createResponse = await service.CreateAsync(GymId, ManagerUserId, dto, CancellationToken.None);
            Assert.True(createResponse.IsSuccess);

            var program = await context.Set<Entities.Program>()
                .Include(p => p.ProgramRoutineItems)
                .SingleAsync();

            var originalOrder = program.ProgramRoutineItems
                .OrderBy(ri => ri.DisplayOrder)
                .Select(ri => ri.Id)
                .ToList();

            var desiredOrder = originalOrder.AsEnumerable().Reverse().ToList();

            var reordered = new ProgramRoutineItemReorderDto
            {
                ProgramId = program.Id,
                Items = desiredOrder
                    .Select((id, index) => new ProgramRoutineItemOrderDto
                    {
                        RoutineItemId = id,
                        DisplayOrder = index + 1
                    })
                    .ToList()
            };

            var response = await service.ReorderRoutineItemsAsync(GymId, ManagerUserId, program.Id, reordered, CancellationToken.None);

            Assert.True(response.IsSuccess);
            var refreshedProgram = await context.Set<Entities.Program>()
                .Include(p => p.ProgramRoutineItems)
                .SingleAsync();

            var orders = refreshedProgram.ProgramRoutineItems
                .OrderBy(ri => ri.DisplayOrder)
                .Select(ri => ri.Id)
                .ToList();
            Assert.Equal(desiredOrder, orders);
        }

        [Fact]
        public async Task ReorderSupersetPracticesAsync_ReassignsInternalOrder()
        {
            using var context = CreateContext(nameof(ReorderSupersetPracticesAsync_ReassignsInternalOrder));
            await SeedReferenceDataAsync(context);
            var service = CreateService(context);

            var dto = BuildDefaultProgramDto();
            var createResponse = await service.CreateAsync(GymId, ManagerUserId, dto, CancellationToken.None);
            Assert.True(createResponse.IsSuccess);

            var superset = await context.Set<ProgramRoutineItem>()
                .Include(ri => ri.ProgramPractices)
                .Where(ri => ri.ItemType == ProgramRoutineItemType.Superset)
                .SingleAsync();

            var originalOrder = superset.ProgramPractices
                .OrderBy(pp => pp.InternalOrder)
                .Select(pp => pp.Id)
                .ToList();

            var desiredOrder = originalOrder.AsEnumerable().Reverse().ToList();

            var reorderDto = new ProgramSupersetPracticeReorderDto
            {
                RoutineItemId = superset.Id,
                Practices = desiredOrder
                    .Select((id, index) => new ProgramSupersetPracticeOrderDto
                    {
                        ProgramPracticeId = id,
                        InternalOrder = index + 1
                    })
                    .ToList()
            };

            var response = await service.ReorderSupersetPracticesAsync(GymId, ManagerUserId, reorderDto, CancellationToken.None);

            Assert.True(response.IsSuccess);
            var refreshedSuperset = await context.Set<ProgramRoutineItem>()
                .Include(ri => ri.ProgramPractices)
                .Where(ri => ri.Id == superset.Id)
                .SingleAsync();

            var refreshedOrder = refreshedSuperset.ProgramPractices
                .OrderBy(pp => pp.InternalOrder)
                .Select(pp => pp.Id)
                .ToList();
            Assert.Equal(desiredOrder, refreshedOrder);
        }

        [Fact]
        public async Task UpdateRoutineItemMetadataAsync_UpdatesSupersetFields()
        {
            using var context = CreateContext(nameof(UpdateRoutineItemMetadataAsync_UpdatesSupersetFields));
            await SeedReferenceDataAsync(context);
            var service = CreateService(context);

            var dto = BuildDefaultProgramDto();
            var createResponse = await service.CreateAsync(GymId, ManagerUserId, dto, CancellationToken.None);
            Assert.True(createResponse.IsSuccess);

            var superset = await context.Set<ProgramRoutineItem>()
                .Where(ri => ri.ItemType == ProgramRoutineItemType.Superset)
                .SingleAsync();

            var metadataDto = new ProgramRoutineItemMetadataDto
            {
                RoutineItemId = superset.Id,
                Title = "Updated Title",
                RepeatCount = 4,
                RestBetweenRepeats = 75,
                Notes = "Updated notes"
            };

            var response = await service.UpdateRoutineItemMetadataAsync(GymId, ManagerUserId, metadataDto, CancellationToken.None);

            Assert.True(response.IsSuccess);
            var refreshedSuperset = await context.Set<ProgramRoutineItem>().FindAsync(superset.Id);
            Assert.NotNull(refreshedSuperset);
            Assert.Equal("Updated Title", refreshedSuperset!.Title);
            Assert.Equal(4, refreshedSuperset.RepeatCount);
            Assert.Equal(75, refreshedSuperset.RestBetweenRepeats);
            Assert.Equal("Updated notes", refreshedSuperset.Notes);
        }

        private ProgramDto BuildDefaultProgramDto()
        {
            return new ProgramDto
            {
                Title = "Hybrid Program",
                Type = ProgramTypes.Private,
                RoutineItems = new List<ProgramRoutineItemInputDto>
                {
                    new ProgramRoutineItemInputDto
                    {
                        ItemType = ProgramRoutineItemType.Superset,
                        Title = "Upper Body Blast",
                        RepeatCount = 3,
                        RestBetweenRepeats = 90,
                        Notes = "Focus on pulling and pushing",
                        Practices = new List<ProgramPracticeInputDto>
                        {
                            new ProgramPracticeInputDto
                            {
                                PracticeId = PracticeAId,
                                Type = PracticeType.Set,
                                SetCount = 3,
                                MovementCount = 10,
                                Rest = 60
                            },
                            new ProgramPracticeInputDto
                            {
                                PracticeId = PracticeBId,
                                Type = PracticeType.Time,
                                Duration = 45,
                                Rest = 60
                            }
                        }
                    },
                    new ProgramRoutineItemInputDto
                    {
                        ItemType = ProgramRoutineItemType.Single,
                        Practices = new List<ProgramPracticeInputDto>
                        {
                            new ProgramPracticeInputDto
                            {
                                PracticeId = PracticeCId,
                                Type = PracticeType.Set,
                                SetCount = 4,
                                MovementCount = 8,
                                Rest = 90,
                                Notes = "Focus on tempo"
                            }
                        }
                    }
                }
            };
        }

        private ProgramService CreateService(ApplicationDbContext context)
        {
            return new ProgramService(
                new Repository<Entities.Program>(context),
                new Repository<ProgramRoutineItem>(context),
                new Repository<ProgramPractice>(context),
                new Repository<GymUser>(context),
                new Repository<Practice>(context),
                new Repository<UserProgram>(context),
                _mapper);
        }

        private ApplicationDbContext CreateContext(string databaseName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
            var httpContextAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            var context = new ApplicationDbContext(options, httpContextAccessor);
            context.Database.EnsureCreated();
            return context;
        }

        private async Task SeedReferenceDataAsync(ApplicationDbContext context)
        {
            var manager = new ApplicationUser
            {
                Id = ManagerUserId,
                UserName = "manager",
                NormalizedUserName = "MANAGER",
                Email = "manager@example.com",
                NormalizedEmail = "MANAGER@EXAMPLE.COM",
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PhoneNumber = "00000000000",
                PhoneNumberConfirmed = true,
                EmailConfirmed = true
            };

            var gym = new Gym
            {
                Id = GymId,
                Title = "Test Gym",
                Slug = $"test-gym-{GymId}",
                Address = "123 Test Street",
                CreatorIP = "127.0.0.1",
                CreatorUserId = ManagerUserId,
                CreateDate = DateTime.UtcNow
            };

            var gymUser = new GymUser
            {
                GymId = GymId,
                UserId = ManagerUserId,
                Role = UsersRole.manager,
                JoinDate = DateTime.UtcNow
            };

            var category = new PracticeCategory
            {
                Id = PracticeCategoryId,
                Title = "Strength",
                SubmitterUserId = ManagerUserId
            };

            var practices = new List<Practice>
            {
                new Practice
                {
                    Id = PracticeAId,
                    Name = "Barbell Row",
                    EnTitle = "Barbell Row",
                    PracticeCategoryId = PracticeCategoryId,
                    UserId = ManagerUserId,
                    CreateDate = DateTime.UtcNow
                },
                new Practice
                {
                    Id = PracticeBId,
                    Name = "Assault Bike",
                    EnTitle = "Assault Bike",
                    PracticeCategoryId = PracticeCategoryId,
                    UserId = ManagerUserId,
                    CreateDate = DateTime.UtcNow
                },
                new Practice
                {
                    Id = PracticeCId,
                    Name = "Back Squat",
                    EnTitle = "Back Squat",
                    PracticeCategoryId = PracticeCategoryId,
                    UserId = ManagerUserId,
                    CreateDate = DateTime.UtcNow
                }
            };

            context.Users.Add(manager);
            context.Set<Gym>().Add(gym);
            context.Set<GymUser>().Add(gymUser);
            context.Set<PracticeCategory>().Add(category);
            context.Set<Practice>().AddRange(practices);

            await context.SaveChangesAsync();
        }
    }
}
