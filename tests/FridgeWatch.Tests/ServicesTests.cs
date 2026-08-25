using Xunit;
using FluentAssertions;
using Moq;
using AutoMapper;
using Microsoft.Extensions.Logging;
using FridgeWatch.Application.Services;
using FridgeWatch.Application.Interfaces;
using FridgeWatch.Domain.Common;
using FridgeWatch.Domain.Entities;
using FridgeWatch.Domain.Interfaces;
using FridgeWatch.Domain.Enums;
using FridgeWatch.Application.DTOs;
using FridgeWatch.Application.Mappings;
using System.Linq.Expressions;

namespace FridgeWatch.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _jwtServiceMock = new Mock<IJwtService>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = config.CreateMapper();

        _authService = new AuthService(_unitOfWorkMock.Object, _mapper, _jwtServiceMock.Object);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenUserExists_ReturnsUserDto()
    {
        // Arrange
        var userId = 1;
        var user = new User
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.GetCurrentUserAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(userId);
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetCurrentUserAsync_WhenUserNotFound_ThrowsException()
    {
        // Arrange
        var userId = 999;
        _unitOfWorkMock.Setup(u => u.Users.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _authService.GetCurrentUserAsync(userId);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("用户不存在");
    }
}

public class HouseholdServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly HouseholdService _householdService;

    public HouseholdServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _auditLogServiceMock = new Mock<IAuditLogService>();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });
        _mapper = config.CreateMapper();

        _householdService = new HouseholdService(_unitOfWorkMock.Object, _mapper, _auditLogServiceMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenHouseholdExists_ReturnsHouseholdDto()
    {
        // Arrange
        var householdId = 1;
        var household = new Household
        {
            Id = householdId,
            Name = "测试家庭",
            InviteCode = "TEST1234",
            CreatedBy = 1,
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWorkMock.Setup(u => u.Households.GetByIdAsync(householdId))
            .ReturnsAsync(household);

        // Act
        var result = await _householdService.GetByIdAsync(householdId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(householdId);
        result.Name.Should().Be("测试家庭");
        result.InviteCode.Should().Be("TEST1234");
    }

    [Fact]
    public async Task GetByIdAsync_WhenHouseholdNotFound_ThrowsException()
    {
        // Arrange
        var householdId = 999;
        _unitOfWorkMock.Setup(u => u.Households.GetByIdAsync(householdId))
            .ReturnsAsync((Household?)null);

        // Act
        Func<Task> act = async () => await _householdService.GetByIdAsync(householdId);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("家庭不存在");
    }
}

public class FoodItemServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;
    private readonly Mock<IExpiryAlertSyncService> _alertSyncServiceMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly FoodItemService _foodItemService;

    public FoodItemServiceTests()
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
    }

    [Fact]
    public async Task GetByIdAsync_WhenFoodItemExists_ReturnsFoodItemDto()
    {
        // Arrange
        var foodItemId = 1;
        var foodItem = new FoodItem
        {
            Id = foodItemId,
            HouseholdId = 1,
            Name = "测试食材",
            Category = "测试分类",
            StorageLocation = StorageLocation.Fridge,
            PurchaseDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            Quantity = 10,
            Unit = "个",
            Status = FoodStatus.Fresh,
            CreatedAt = DateTime.UtcNow
        };

        _unitOfWorkMock.Setup(u => u.FoodItems.GetByIdAsync(foodItemId))
            .ReturnsAsync(foodItem);

        // Act
        var result = await _foodItemService.GetByIdAsync(foodItemId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(foodItemId);
        result.Name.Should().Be("测试食材");
        result.Status.Should().Be(FoodStatus.Fresh);
    }
}

public class CsvParserTests
{
    [Fact]
    public void ParseCsvRecords_SimpleRows_ReturnsAllRecords()
    {
        var lines = new List<string>
        {
            "牛奶,乳制品,冷藏,2026-06-15,2026-06-25,2,盒",
            "鸡胸肉,肉类,冷冻,2026-06-10,2026-07-10,1.5,kg",
            "大米,主食,常温,2026-01-01,2026-12-31,10,kg"
        };

        var result = FoodItemService.ParseCsvRecords(lines);

        result.Should().HaveCount(3);
        result[0][0].Should().Be("牛奶");
        result[0][6].Should().Be("盒");
        result[2][0].Should().Be("大米");
        result[2][5].Should().Be("10");
    }

    [Fact]
    public void ParseCsvRecords_WithHeader_StillParsesAllRows()
    {
        var lines = new List<string>
        {
            "名称,分类,存放位置,购买日期,保质期,数量,单位",
            "牛奶,乳制品,冷藏,2026-06-15,2026-06-25,2,盒",
            "鸡胸肉,肉类,冷冻,2026-06-10,2026-07-10,1.5,kg"
        };

        var result = FoodItemService.ParseCsvRecords(lines);

        result.Should().HaveCount(3);
        result[0][0].Should().Be("名称");
        result[1][0].Should().Be("牛奶");
        result[2][0].Should().Be("鸡胸肉");
    }

    [Fact]
    public void ParseCsvRecords_QuotedFieldWithComma_PreservesComma()
    {
        var lines = new List<string>
        {
            "\"Butter, salted\",乳制品,冷藏,2026-06-15,2026-06-25,1,盒",
            "牛奶,乳制品,冷藏,2026-06-15,2026-06-25,2,盒"
        };

        var result = FoodItemService.ParseCsvRecords(lines);

        result.Should().HaveCount(2);
        result[0].Should().HaveCount(7);
        result[0][0].Should().Be("Butter, salted");
        result[0][1].Should().Be("乳制品");
        result[0][6].Should().Be("盒");
    }

    [Fact]
    public void ParseCsvRecords_DoubleQuotesInsideQuotedField_EscapesCorrectly()
    {
        var lines = new List<string>
        {
            "\"3\"\" 牛奶\",乳制品,冷藏,2026-06-15,2026-06-25,2,盒"
        };

        var result = FoodItemService.ParseCsvRecords(lines);

        result.Should().HaveCount(1);
        result[0][0].Should().Be("3\" 牛奶");
    }

    [Fact]
    public void ParseCsvRecords_MultilineQuotedField_SpansLines()
    {
        var lines = new List<string>
        {
            "\"有机\n牛奶\",乳制品,冷藏,2026-06-15,2026-06-25,2,盒",
            "大米,主食,常温,2026-01-01,2026-12-31,10,kg"
        };

        var result = FoodItemService.ParseCsvRecords(lines);

        result.Should().HaveCount(2);
        result[0][0].Should().Be("有机\n牛奶");
        result[0][1].Should().Be("乳制品");
        result[1][0].Should().Be("大米");
    }

    [Fact]
    public void ParseCsvRecords_EmptyFields_ParsedAsEmpty()
    {
        var lines = new List<string>
        {
            "牛奶,,冷藏,,2026-06-25,2,"
        };

        var result = FoodItemService.ParseCsvRecords(lines);

        result.Should().HaveCount(1);
        result[0].Should().HaveCount(7);
        result[0][0].Should().Be("牛奶");
        result[0][1].Should().Be("");
        result[0][3].Should().Be("");
        result[0][6].Should().Be("");
    }

    [Fact]
    public void ParseCsvRecords_EmptyQuotedField_ParsedAsEmpty()
    {
        var lines = new List<string>
        {
            "牛奶,\"\",冷藏,2026-06-15,2026-06-25,2,盒"
        };

        var result = FoodItemService.ParseCsvRecords(lines);

        result.Should().HaveCount(1);
        result[0][1].Should().Be("");
    }

    [Fact]
    public void ParseCsvRecords_EmptyLines_StillProduceRecords()
    {
        var lines = new List<string>
        {
            "牛奶,乳制品,冷藏,2026-06-15,2026-06-25,2,盒",
            "",
            "大米,主食,常温,2026-01-01,2026-12-31,10,kg"
        };

        var result = FoodItemService.ParseCsvRecords(lines);

        result.Should().HaveCount(3);
        result[0][0].Should().Be("牛奶");
        result[1][0].Should().Be("");
        result[1].Should().HaveCount(1);
        result[2][0].Should().Be("大米");
    }

    [Fact]
    public void IsHeaderRow_ValidHeader_ReturnsTrue()
    {
        var row = new FoodItemImportRowDto
        {
            Name = "名称",
            Category = "分类",
            StorageLocation = "存放位置",
            PurchaseDate = "购买日期",
            ExpiryDate = "保质期",
            Quantity = "数量",
            Unit = "单位"
        };

        FoodItemService.IsHeaderRow(row).Should().BeTrue();
    }

    [Fact]
    public void IsHeaderRow_PartialHeader_ReturnsTrue()
    {
        var row = new FoodItemImportRowDto
        {
            Name = "食物名称",
            Category = "分类",
            StorageLocation = "位置",
            PurchaseDate = "进货日期",
            ExpiryDate = "到期时间",
            Quantity = "数量",
            Unit = "单位"
        };

        FoodItemService.IsHeaderRow(row).Should().BeTrue();
    }

    [Fact]
    public void IsHeaderRow_DataRow_ReturnsFalse()
    {
        var row = new FoodItemImportRowDto
        {
            Name = "牛奶",
            Category = "乳制品",
            StorageLocation = "冷藏",
            PurchaseDate = "2026-06-15",
            ExpiryDate = "2026-06-25",
            Quantity = "2",
            Unit = "盒"
        };

        FoodItemService.IsHeaderRow(row).Should().BeFalse();
    }

    [Fact]
    public void IsHeaderRow_EmptyRow_ReturnsFalse()
    {
        var row = new FoodItemImportRowDto();

        FoodItemService.IsHeaderRow(row).Should().BeFalse();
    }

    [Fact]
    public void ParseCsvRecords_NoHeader_FirstRecordIsData()
    {
        var lines = new List<string>
        {
            "牛奶,乳制品,冷藏,2026-06-15,2026-06-25,2,盒",
            "大米,主食,常温,2026-01-01,2026-12-31,10,kg",
            "鸡胸肉,肉类,冷冻,2026-06-10,2026-07-10,1.5,kg"
        };

        var result = FoodItemService.ParseCsvRecords(lines);

        result.Should().HaveCount(3);
        result[0][0].Should().Be("牛奶");
        result[1][0].Should().Be("大米");
        result[2][0].Should().Be("鸡胸肉");
    }

    [Fact]
    public void ParseCsvRecords_MixedQuotedAndUnquoted_ParsesCorrectly()
    {
        var lines = new List<string>
        {
            "名称,分类,存放位置,购买日期,保质期,数量,单位",
            "\"有机,低脂牛奶\",乳制品,冷藏,2026-06-15,2026-06-25,2,盒",
            "大米,主食,常温,2026-01-01,2026-12-31,10,kg",
            "\"特级\n鸡胸肉\",肉类,冷冻,2026-06-10,2026-07-10,1.5,kg",
            "\"3\"\" 装鸡蛋\",蛋类,冷藏,2026-06-20,2026-07-20,30,个"
        };

        var result = FoodItemService.ParseCsvRecords(lines);

        result.Should().HaveCount(5);
        result[0][0].Should().Be("名称");
        result[1][0].Should().Be("有机,低脂牛奶");
        result[2][0].Should().Be("大米");
        result[3][0].Should().Be("特级\n鸡胸肉");
        result[4][0].Should().Be("3\" 装鸡蛋");
    }
}

public class FoodStatusHelperTests
{
    [Theory]
    [InlineData(-30)]
    [InlineData(-2)]
    [InlineData(-1)]
    public void CalculateStatus_WhenAlreadyExpired_ReturnsExpired(int daysToExpiry)
    {
        // Arrange
        var expiryDate = DateTime.UtcNow.Date.AddDays(daysToExpiry);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.Expired);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CalculateStatus_WhenWithinThreeDays_ReturnsNearExpiry(int daysToExpiry)
    {
        // Arrange
        var expiryDate = DateTime.UtcNow.Date.AddDays(daysToExpiry);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(30)]
    public void CalculateStatus_WhenMoreThanThreeDays_ReturnsFresh(int daysToExpiry)
    {
        // Arrange
        var expiryDate = DateTime.UtcNow.Date.AddDays(daysToExpiry);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.Fresh);
    }

    [Fact]
    public void CalculateStatus_WhenExpiredExactlyOneDay_ReturnsExpired()
    {
        // Arrange
        var expiryDate = DateTime.UtcNow.Date.AddDays(-1);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.Expired);
    }

    [Fact]
    public void CalculateStatus_WhenExpiresToday_ReturnsNearExpiry()
    {
        // Arrange
        var expiryDate = DateTime.UtcNow.Date;

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_WhenExactlyThreeDaysLeft_ReturnsNearExpiry()
    {
        // Arrange
        var expiryDate = DateTime.UtcNow.Date.AddDays(3);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_WhenFourDaysLeft_ReturnsFresh()
    {
        // Arrange
        var expiryDate = DateTime.UtcNow.Date.AddDays(4);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.Fresh);
    }

    [Fact]
    public void CalculateStatus_IgnoresTimeComponent_UsesDateOnly()
    {
        // Arrange：三天后当日的最后一刻仍应处于三天临界点内
        var expiryDate = DateTime.UtcNow.Date.AddDays(3).AddHours(23).AddMinutes(59);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateStatus_WhenQuantityNotPositive_ReturnsConsumed(decimal quantity)
    {
        // Arrange：即使保质期还很新鲜，数量为 0 也应视为已消耗
        var expiryDate = DateTime.UtcNow.Date.AddDays(30);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate, quantity);

        // Assert
        result.Should().Be(FoodStatus.Consumed);
    }

    [Theory]
    [InlineData(-1, FoodStatus.Expired)]
    [InlineData(0, FoodStatus.NearExpiry)]
    [InlineData(3, FoodStatus.NearExpiry)]
    [InlineData(4, FoodStatus.Fresh)]
    public void CalculateStatus_WithPositiveQuantity_FollowsDateRules(int daysToExpiry, FoodStatus expected)
    {
        // Arrange
        var expiryDate = DateTime.UtcNow.Date.AddDays(daysToExpiry);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate, 5m);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(-30, 30)]
    [InlineData(0, 0)]
    [InlineData(3, -3)]
    public void GetDaysExpired_ReturnsDaysSinceExpiry(int daysToExpiry, int expectedDaysExpired)
    {
        // Arrange
        var expiryDate = DateTime.UtcNow.Date.AddDays(daysToExpiry);

        // Act
        var result = FoodStatusHelper.GetDaysExpired(expiryDate);

        // Assert
        result.Should().Be(expectedDaysExpired);
    }

    [Fact]
    public void ShouldBeArchived_WhenDaysExpiredReachesThreshold_ReturnsTrue()
    {
        // Arrange：过期天数刚好等于自动归档阈值
        var expiryDate = DateTime.UtcNow.Date.AddDays(-7);

        // Act
        var result = FoodStatusHelper.ShouldBeArchived(expiryDate, FoodStatus.Expired, 7);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldBeArchived_WhenDaysExpiredBelowThreshold_ReturnsFalse()
    {
        // Arrange：过期天数差一天未达到阈值
        var expiryDate = DateTime.UtcNow.Date.AddDays(-6);

        // Act
        var result = FoodStatusHelper.ShouldBeArchived(expiryDate, FoodStatus.Expired, 7);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(FoodStatus.Archived)]
    [InlineData(FoodStatus.Consumed)]
    public void ShouldBeArchived_WhenAlreadyArchivedOrConsumed_ReturnsFalse(FoodStatus currentStatus)
    {
        // Arrange：即使已过期很久，已归档/已消耗的食材不再处理
        var expiryDate = DateTime.UtcNow.Date.AddDays(-30);

        // Act
        var result = FoodStatusHelper.ShouldBeArchived(expiryDate, currentStatus, 7);

        // Assert
        result.Should().BeFalse();
    }
}

public class ExpiryAlertSyncServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<ExpiryAlertSyncService>> _loggerMock;
    private readonly ExpiryAlertSyncService _syncService;
    private readonly List<ExpiryAlert> _addedAlerts = new();
    private readonly List<int> _deletedAlertIds = new();

    public ExpiryAlertSyncServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ExpiryAlertSyncService>>();
        _syncService = new ExpiryAlertSyncService(_unitOfWorkMock.Object, _loggerMock.Object);

        _unitOfWorkMock.Setup(u => u.FoodItems.UpdateAsync(It.IsAny<FoodItem>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.ExpiryAlerts.AddAsync(It.IsAny<ExpiryAlert>()))
            .Callback<ExpiryAlert>(alert => _addedAlerts.Add(alert))
            .ReturnsAsync((ExpiryAlert alert) => alert);
        _unitOfWorkMock.Setup(u => u.ExpiryAlerts.DeleteAsync(It.IsAny<int>()))
            .Callback<int>(id => _deletedAlertIds.Add(id))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
    }

    private void SetupHouseholdMembers(params int[] userIds)
    {
        var members = userIds
            .Select(userId => new HouseholdMember { HouseholdId = 1, UserId = userId })
            .ToList();
        _unitOfWorkMock.Setup(u => u.HouseholdMembers.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(members);
    }

    private void SetupExistingAlerts(params ExpiryAlert[] alerts)
    {
        _unitOfWorkMock.Setup(u => u.ExpiryAlerts.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(alerts.ToList());
    }

    private static FoodItem CreateFoodItem(int daysToExpiry, FoodStatus status, decimal quantity = 1m)
    {
        return new FoodItem
        {
            Id = 1,
            HouseholdId = 1,
            Name = "测试食材",
            ExpiryDate = DateTime.UtcNow.Date.AddDays(daysToExpiry),
            Quantity = quantity,
            Status = status
        };
    }

    [Fact]
    public async Task SyncAlertsForFoodItemAsync_WhenExactlyThreeDaysLeft_UpdatesStatusAndCreatesNearExpiryAlerts()
    {
        // Arrange：刚好剩三天，处于临期临界点
        var foodItem = CreateFoodItem(3, FoodStatus.Fresh);
        SetupHouseholdMembers(10, 11);
        SetupExistingAlerts();

        // Act
        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        // Assert
        foodItem.Status.Should().Be(FoodStatus.NearExpiry);
        _unitOfWorkMock.Verify(u => u.FoodItems.UpdateAsync(foodItem), Times.Once);
        _addedAlerts.Should().HaveCount(2);
        _addedAlerts.Should().OnlyContain(a => a.AlertType == AlertType.NearExpiry);
        _addedAlerts.Select(a => a.UserId).Should().BeEquivalentTo(new[] { 10, 11 });
        _deletedAlertIds.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncAlertsForFoodItemAsync_WhenExpiredExactlyOneDay_UpdatesStatusAndCreatesExpiredAlerts()
    {
        // Arrange：刚好过期一天
        var foodItem = CreateFoodItem(-1, FoodStatus.NearExpiry);
        SetupHouseholdMembers(10);
        SetupExistingAlerts();

        // Act
        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        // Assert
        foodItem.Status.Should().Be(FoodStatus.Expired);
        _addedAlerts.Should().HaveCount(1);
        _addedAlerts.Should().OnlyContain(a => a.AlertType == AlertType.Expired);
    }

    [Fact]
    public async Task SyncAlertsForFoodItemAsync_WhenExpiredExactlyOneDay_RemovesOutdatedNearExpiryAlert()
    {
        // Arrange：从临期变为过期后，旧的临期提醒应被移除
        var foodItem = CreateFoodItem(-1, FoodStatus.NearExpiry);
        SetupHouseholdMembers(10);
        SetupExistingAlerts(new ExpiryAlert { Id = 5, FoodItemId = 1, UserId = 10, AlertType = AlertType.NearExpiry });

        // Act
        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        // Assert
        _deletedAlertIds.Should().Contain(5);
        _addedAlerts.Should().HaveCount(1);
        _addedAlerts[0].AlertType.Should().Be(AlertType.Expired);
        _addedAlerts[0].UserId.Should().Be(10);
    }

    [Fact]
    public async Task SyncAlertsForFoodItemAsync_WhenExpiresToday_TreatedAsNearExpiry()
    {
        // Arrange：今天到期，剩余 0 天，属于临期
        var foodItem = CreateFoodItem(0, FoodStatus.Fresh);
        SetupHouseholdMembers(10);
        SetupExistingAlerts();

        // Act
        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        // Assert
        foodItem.Status.Should().Be(FoodStatus.NearExpiry);
        _addedAlerts.Should().OnlyContain(a => a.AlertType == AlertType.NearExpiry);
    }

    [Fact]
    public async Task SyncAlertsForFoodItemAsync_WhenFourDaysLeft_TreatedAsFreshAndRemovesSystemAlerts()
    {
        // Arrange：刚好剩四天，越过三天临界点，属于新鲜
        var foodItem = CreateFoodItem(4, FoodStatus.NearExpiry);
        SetupHouseholdMembers(10);
        SetupExistingAlerts(new ExpiryAlert { Id = 7, FoodItemId = 1, UserId = 10, AlertType = AlertType.NearExpiry });

        // Act
        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        // Assert
        foodItem.Status.Should().Be(FoodStatus.Fresh);
        _addedAlerts.Should().BeEmpty();
        _deletedAlertIds.Should().Contain(7);
    }

    [Fact]
    public async Task SyncAlertsForFoodItemAsync_WhenStatusAlreadyCorrect_DoesNotUpdateFoodItem()
    {
        // Arrange：状态未变化时不应写库
        var foodItem = CreateFoodItem(3, FoodStatus.NearExpiry);
        SetupHouseholdMembers(10);
        SetupExistingAlerts(new ExpiryAlert { Id = 8, FoodItemId = 1, UserId = 10, AlertType = AlertType.NearExpiry });

        // Act
        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        // Assert
        _unitOfWorkMock.Verify(u => u.FoodItems.UpdateAsync(It.IsAny<FoodItem>()), Times.Never);
        _addedAlerts.Should().BeEmpty();
        _deletedAlertIds.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncAlertsForFoodItemAsync_WhenQuantityZero_UpdatesStatusToConsumed()
    {
        // Arrange
        var foodItem = CreateFoodItem(10, FoodStatus.Fresh, quantity: 0m);
        SetupHouseholdMembers(10);
        SetupExistingAlerts();

        // Act
        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        // Assert
        foodItem.Status.Should().Be(FoodStatus.Consumed);
        _unitOfWorkMock.Verify(u => u.FoodItems.UpdateAsync(foodItem), Times.Once);
        _addedAlerts.Should().BeEmpty();
    }

    [Theory]
    [InlineData(FoodStatus.Consumed)]
    [InlineData(FoodStatus.Archived)]
    public async Task SyncAlertsForFoodItemAsync_WhenConsumedOrArchived_RemovesSystemAlertsAndKeepsStatus(FoodStatus status)
    {
        // Arrange：已消耗/已归档的食材不重新计算状态，只清理系统提醒
        var foodItem = CreateFoodItem(3, status);
        SetupHouseholdMembers(10);
        SetupExistingAlerts(new ExpiryAlert { Id = 9, FoodItemId = 1, UserId = 10, AlertType = AlertType.NearExpiry });

        // Act
        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        // Assert
        foodItem.Status.Should().Be(status);
        _deletedAlertIds.Should().Contain(9);
        _addedAlerts.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.FoodItems.UpdateAsync(It.IsAny<FoodItem>()), Times.Never);
    }
}
