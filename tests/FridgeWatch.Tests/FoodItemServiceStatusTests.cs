using AutoMapper;
using FluentAssertions;
using Moq;
using Xunit;
using FridgeWatch.Application.DTOs;
using FridgeWatch.Application.Interfaces;
using FridgeWatch.Application.Mappings;
using FridgeWatch.Application.Services;
using FridgeWatch.Domain.Entities;
using FridgeWatch.Domain.Enums;
using FridgeWatch.Domain.Interfaces;

namespace FridgeWatch.Tests;

public class FoodItemServiceStatusTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;
    private readonly Mock<IExpiryAlertSyncService> _alertSyncServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly FoodItemService _foodItemService;

    private readonly List<FoodItem> _addedFoodItems = new();

    private static DateTime Today => DateTime.UtcNow.Date;

    public FoodItemServiceStatusTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _alertSyncServiceMock = new Mock<IExpiryAlertSyncService>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _auditLogServiceMock = new Mock<IAuditLogService>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = config.CreateMapper();

        _foodItemService = new FoodItemService(_unitOfWorkMock.Object, _mapper, _alertSyncServiceMock.Object, _fileStorageServiceMock.Object, _auditLogServiceMock.Object);

        _unitOfWorkMock.Setup(u => u.HouseholdMembers.IsHouseholdMemberAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        _unitOfWorkMock.Setup(u => u.FoodItems.AddAsync(It.IsAny<FoodItem>()))
            .Callback<FoodItem>(_addedFoodItems.Add)
            .Returns((FoodItem item) => Task.FromResult(item));

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);

        _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = 1, Username = "testuser" });
    }

    private static FoodItemCreateDto BuildCreateDto(DateTime expiryDate, decimal quantity = 1)
    {
        return new FoodItemCreateDto
        {
            HouseholdId = 1,
            Name = "牛奶",
            Category = "乳制品",
            StorageLocation = StorageLocation.Fridge,
            PurchaseDate = Today.AddDays(-2),
            ExpiryDate = expiryDate,
            Quantity = quantity,
            Unit = "盒"
        };
    }

    [Fact]
    public async Task CreateAsync_ExactlyThreeDaysToExpiry_SetsNearExpiryStatus()
    {
        // Arrange
        var dto = BuildCreateDto(Today.AddDays(3));

        // Act
        var result = await _foodItemService.CreateAsync(dto, 1);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(FoodStatus.NearExpiry);
        _addedFoodItems.Should().HaveCount(1);
        _addedFoodItems[0].Status.Should().Be(FoodStatus.NearExpiry);
        _alertSyncServiceMock.Verify(s => s.SyncAlertsForFoodItemAsync(
            It.Is<FoodItem>(f => f.Status == FoodStatus.NearExpiry)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ExpiresToday_SetsNearExpiryStatus()
    {
        // Arrange
        var dto = BuildCreateDto(Today);

        // Act
        var result = await _foodItemService.CreateAsync(dto, 1);

        // Assert
        result.Status.Should().Be(FoodStatus.NearExpiry);
        _addedFoodItems[0].Status.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public async Task CreateAsync_ExpiredOneDayAgo_SetsExpiredStatus()
    {
        // Arrange
        var dto = BuildCreateDto(Today.AddDays(-1));

        // Act
        var result = await _foodItemService.CreateAsync(dto, 1);

        // Assert
        result.Status.Should().Be(FoodStatus.Expired);
        _addedFoodItems[0].Status.Should().Be(FoodStatus.Expired);
    }

    [Fact]
    public async Task CreateAsync_FourDaysToExpiry_SetsFreshStatus()
    {
        // Arrange
        var dto = BuildCreateDto(Today.AddDays(4));

        // Act
        var result = await _foodItemService.CreateAsync(dto, 1);

        // Assert
        result.Status.Should().Be(FoodStatus.Fresh);
        _addedFoodItems[0].Status.Should().Be(FoodStatus.Fresh);
    }

    [Fact]
    public async Task CreateAsync_SevenDaysToExpiry_SetsFreshStatus()
    {
        // Arrange
        var dto = BuildCreateDto(Today.AddDays(7));

        // Act
        var result = await _foodItemService.CreateAsync(dto, 1);

        // Assert
        result.Status.Should().Be(FoodStatus.Fresh);
        _addedFoodItems[0].Status.Should().Be(FoodStatus.Fresh);
    }

    [Fact]
    public async Task CreateAsync_ZeroQuantity_SetsConsumedStatus()
    {
        // Arrange
        var dto = BuildCreateDto(Today.AddDays(7), quantity: 0);

        // Act
        var result = await _foodItemService.CreateAsync(dto, 1);

        // Assert
        result.Status.Should().Be(FoodStatus.Consumed);
        _addedFoodItems[0].Status.Should().Be(FoodStatus.Consumed);
    }
}
