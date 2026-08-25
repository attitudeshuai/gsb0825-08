using Xunit;
using FluentAssertions;
using Moq;
using AutoMapper;
using FridgeWatch.Application.Services;
using FridgeWatch.Application.Interfaces;
using FridgeWatch.Domain.Entities;
using FridgeWatch.Domain.Interfaces;
using FridgeWatch.Domain.Enums;
using FridgeWatch.Domain.Common;
using FridgeWatch.Application.DTOs;
using FridgeWatch.Application.Mappings;
using Microsoft.Extensions.Logging;
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
    private static DateTime Today => DateTime.UtcNow.Date;

    [Fact]
    public void CalculateStatus_ExpiryIn4Days_ReturnsFresh()
    {
        var expiryDate = Today.AddDays(4);

        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        result.Should().Be(FoodStatus.Fresh);
    }

    [Fact]
    public void CalculateStatus_ExpiryFarInFuture_ReturnsFresh()
    {
        var expiryDate = Today.AddDays(365);

        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        result.Should().Be(FoodStatus.Fresh);
    }

    [Fact]
    public void CalculateStatus_ExpiryExactly3Days_ReturnsNearExpiry()
    {
        var expiryDate = Today.AddDays(3);

        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_ExpiryIn2Days_ReturnsNearExpiry()
    {
        var expiryDate = Today.AddDays(2);

        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_ExpiryIn1Day_ReturnsNearExpiry()
    {
        var expiryDate = Today.AddDays(1);

        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_ExpiryToday_ReturnsNearExpiry()
    {
        var expiryDate = Today;

        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_ExpiryYesterday_ReturnsExpired()
    {
        var expiryDate = Today.AddDays(-1);

        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        result.Should().Be(FoodStatus.Expired);
    }

    [Fact]
    public void CalculateStatus_ExpiryExactly1DayAgo_ReturnsExpired()
    {
        var expiryDate = Today.AddDays(-1);

        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        result.Should().Be(FoodStatus.Expired);
    }

    [Fact]
    public void CalculateStatus_ExpiryManyDaysAgo_ReturnsExpired()
    {
        var expiryDate = Today.AddDays(-30);

        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        result.Should().Be(FoodStatus.Expired);
    }

    [Fact]
    public void CalculateStatus_WithZeroQuantity_ReturnsConsumed()
    {
        var expiryDate = Today.AddDays(10);

        var result = FoodStatusHelper.CalculateStatus(expiryDate, 0m);

        result.Should().Be(FoodStatus.Consumed);
    }

    [Fact]
    public void CalculateStatus_WithNegativeQuantity_ReturnsConsumed()
    {
        var expiryDate = Today.AddDays(10);

        var result = FoodStatusHelper.CalculateStatus(expiryDate, -3m);

        result.Should().Be(FoodStatus.Consumed);
    }

    [Fact]
    public void CalculateStatus_WithZeroQuantityEvenIfExpired_ReturnsConsumed()
    {
        var expiryDate = Today.AddDays(-5);

        var result = FoodStatusHelper.CalculateStatus(expiryDate, 0m);

        result.Should().Be(FoodStatus.Consumed);
    }

    [Fact]
    public void CalculateStatus_WithPositiveQuantity_UsesDateLogic()
    {
        var expiryDate = Today.AddDays(3);

        var result = FoodStatusHelper.CalculateStatus(expiryDate, 5m);

        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_IgnoresTimeComponent_UsesDateOnly()
    {
        var expiryDate = Today.AddDays(3).AddHours(23).AddMinutes(59).AddSeconds(59);

        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_FourDaysBoundary_JustAboveNearExpiryThreshold_ReturnsFresh()
    {
        var expiryDate = Today.AddDays(4);

        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        result.Should().Be(FoodStatus.Fresh);
    }

    [Fact]
    public void GetDaysExpired_PastDate_ReturnsPositiveDays()
    {
        var expiryDate = Today.AddDays(-5);

        var result = FoodStatusHelper.GetDaysExpired(expiryDate);

        result.Should().Be(5);
    }

    [Fact]
    public void GetDaysExpired_Exactly1DayAgo_ReturnsOne()
    {
        var expiryDate = Today.AddDays(-1);

        var result = FoodStatusHelper.GetDaysExpired(expiryDate);

        result.Should().Be(1);
    }

    [Fact]
    public void GetDaysExpired_Today_ReturnsZero()
    {
        var expiryDate = Today;

        var result = FoodStatusHelper.GetDaysExpired(expiryDate);

        result.Should().Be(0);
    }

    [Fact]
    public void GetDaysExpired_FutureDate_ReturnsNegative()
    {
        var expiryDate = Today.AddDays(3);

        var result = FoodStatusHelper.GetDaysExpired(expiryDate);

        result.Should().Be(-3);
    }

    [Fact]
    public void ShouldBeArchived_AlreadyArchived_ReturnsFalse()
    {
        var result = FoodStatusHelper.ShouldBeArchived(Today.AddDays(-10), FoodStatus.Archived, 7);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldBeArchived_AlreadyConsumed_ReturnsFalse()
    {
        var result = FoodStatusHelper.ShouldBeArchived(Today.AddDays(-10), FoodStatus.Consumed, 7);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldBeArchived_ExpiredExactlyThresholdDays_ReturnsTrue()
    {
        var result = FoodStatusHelper.ShouldBeArchived(Today.AddDays(-7), FoodStatus.Expired, 7);

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldBeArchived_ExpiredBeyondThreshold_ReturnsTrue()
    {
        var result = FoodStatusHelper.ShouldBeArchived(Today.AddDays(-10), FoodStatus.Expired, 7);

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldBeArchived_ExpiredOneDayLessThanThreshold_ReturnsFalse()
    {
        var result = FoodStatusHelper.ShouldBeArchived(Today.AddDays(-6), FoodStatus.Expired, 7);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldBeArchived_NotExpired_ReturnsFalse()
    {
        var result = FoodStatusHelper.ShouldBeArchived(Today.AddDays(2), FoodStatus.NearExpiry, 7);

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldBeArchived_FreshItem_ReturnsFalse()
    {
        var result = FoodStatusHelper.ShouldBeArchived(Today.AddDays(10), FoodStatus.Fresh, 7);

        result.Should().BeFalse();
    }
}

public class ExpiryAlertSyncServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<ExpiryAlertSyncService>> _loggerMock;
    private readonly Mock<IFoodItemRepository> _foodItemRepositoryMock;
    private readonly Mock<IHouseholdMemberRepository> _householdMemberRepositoryMock;
    private readonly Mock<IExpiryAlertRepository> _expiryAlertRepositoryMock;
    private readonly ExpiryAlertSyncService _syncService;

    private static DateTime Today => DateTime.UtcNow.Date;

    public ExpiryAlertSyncServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ExpiryAlertSyncService>>();
        _foodItemRepositoryMock = new Mock<IFoodItemRepository>();
        _householdMemberRepositoryMock = new Mock<IHouseholdMemberRepository>();
        _expiryAlertRepositoryMock = new Mock<IExpiryAlertRepository>();

        _unitOfWorkMock.Setup(u => u.FoodItems).Returns(_foodItemRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.HouseholdMembers).Returns(_householdMemberRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.ExpiryAlerts).Returns(_expiryAlertRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(new List<HouseholdMember>());
        _expiryAlertRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(new List<ExpiryAlert>());

        _syncService = new ExpiryAlertSyncService(_unitOfWorkMock.Object, _loggerMock.Object);
    }

    private static FoodItem CreateFoodItem(DateTime expiryDate, FoodStatus status, decimal quantity = 1m, int householdId = 1, int id = 1)
    {
        return new FoodItem
        {
            Id = id,
            HouseholdId = householdId,
            CreatedByUserId = 1,
            Name = "测试食材",
            Category = "测试分类",
            ExpiryDate = expiryDate,
            Quantity = quantity,
            Status = status
        };
    }

    private static List<HouseholdMember> CreateHouseholdMembers(int householdId, params int[] userIds)
    {
        return userIds.Select(uid => new HouseholdMember
        {
            Id = uid,
            HouseholdId = householdId,
            UserId = uid
        }).ToList();
    }

    [Fact]
    public async Task SyncAlerts_FreshItem_RemovesAllExistingAlertsAndCreatesNone()
    {
        var foodItem = CreateFoodItem(Today.AddDays(10), FoodStatus.Fresh);
        var members = CreateHouseholdMembers(1, 1, 2);
        var existingAlerts = new List<ExpiryAlert>
        {
            new ExpiryAlert { Id = 10, FoodItemId = 1, UserId = 1, AlertType = AlertType.NearExpiry },
            new ExpiryAlert { Id = 11, FoodItemId = 1, UserId = 2, AlertType = AlertType.Expired }
        };

        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(members);
        _expiryAlertRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(existingAlerts);

        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        _expiryAlertRepositoryMock.Verify(r => r.DeleteAsync(10), Times.Once);
        _expiryAlertRepositoryMock.Verify(r => r.DeleteAsync(11), Times.Once);
        _expiryAlertRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ExpiryAlert>()), Times.Never);
        _foodItemRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<FoodItem>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SyncAlerts_NearExpiryExactly3Days_CreatesNearExpiryAlertsForAllMembers()
    {
        var foodItem = CreateFoodItem(Today.AddDays(3), FoodStatus.Fresh);
        var members = CreateHouseholdMembers(1, 1, 2);

        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(members);

        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        _expiryAlertRepositoryMock.Verify(r => r.AddAsync(It.Is<ExpiryAlert>(
            a => a.AlertType == AlertType.NearExpiry && a.UserId == 1)), Times.Once);
        _expiryAlertRepositoryMock.Verify(r => r.AddAsync(It.Is<ExpiryAlert>(
            a => a.AlertType == AlertType.NearExpiry && a.UserId == 2)), Times.Once);
        _expiryAlertRepositoryMock.Verify(r => r.AddAsync(It.Is<ExpiryAlert>(
            a => a.AlertType == AlertType.Expired)), Times.Never);
        _foodItemRepositoryMock.Verify(r => r.UpdateAsync(It.Is<FoodItem>(f => f.Status == FoodStatus.NearExpiry)), Times.Once);
    }

    [Fact]
    public async Task SyncAlerts_NearExpiryToday_CreatesNearExpiryAlerts()
    {
        var foodItem = CreateFoodItem(Today, FoodStatus.Fresh);
        var members = CreateHouseholdMembers(1, 1);

        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(members);

        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        _expiryAlertRepositoryMock.Verify(r => r.AddAsync(It.Is<ExpiryAlert>(
            a => a.AlertType == AlertType.NearExpiry)), Times.Once);
    }

    [Fact]
    public async Task SyncAlerts_Expired1DayAgo_CreatesExpiredAlerts()
    {
        var foodItem = CreateFoodItem(Today.AddDays(-1), FoodStatus.NearExpiry);
        var members = CreateHouseholdMembers(1, 1, 2);

        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(members);

        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        _expiryAlertRepositoryMock.Verify(r => r.AddAsync(It.Is<ExpiryAlert>(
            a => a.AlertType == AlertType.Expired && a.UserId == 1)), Times.Once);
        _expiryAlertRepositoryMock.Verify(r => r.AddAsync(It.Is<ExpiryAlert>(
            a => a.AlertType == AlertType.Expired && a.UserId == 2)), Times.Once);
        _foodItemRepositoryMock.Verify(r => r.UpdateAsync(It.Is<FoodItem>(f => f.Status == FoodStatus.Expired)), Times.Once);
    }

    [Fact]
    public async Task SyncAlerts_TransitionFromNearExpiryToExpired_RemovesNearExpiryAndAddsExpired()
    {
        var foodItem = CreateFoodItem(Today.AddDays(-1), FoodStatus.NearExpiry);
        var members = CreateHouseholdMembers(1, 1);
        var existingAlerts = new List<ExpiryAlert>
        {
            new ExpiryAlert { Id = 20, FoodItemId = 1, UserId = 1, AlertType = AlertType.NearExpiry }
        };

        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(members);
        _expiryAlertRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(existingAlerts);

        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        _expiryAlertRepositoryMock.Verify(r => r.DeleteAsync(20), Times.Once);
        _expiryAlertRepositoryMock.Verify(r => r.AddAsync(It.Is<ExpiryAlert>(
            a => a.AlertType == AlertType.Expired)), Times.Once);
        _expiryAlertRepositoryMock.Verify(r => r.AddAsync(It.Is<ExpiryAlert>(
            a => a.AlertType == AlertType.NearExpiry)), Times.Never);
    }

    [Fact]
    public async Task SyncAlerts_ConsumedItem_RemovesAllSystemAlerts()
    {
        var foodItem = CreateFoodItem(Today.AddDays(-1), FoodStatus.Consumed);
        var existingAlerts = new List<ExpiryAlert>
        {
            new ExpiryAlert { Id = 30, FoodItemId = 1, UserId = 1, AlertType = AlertType.NearExpiry },
            new ExpiryAlert { Id = 31, FoodItemId = 1, UserId = 1, AlertType = AlertType.Expired }
        };

        _expiryAlertRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(existingAlerts);

        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        _expiryAlertRepositoryMock.Verify(r => r.DeleteAsync(30), Times.Once);
        _expiryAlertRepositoryMock.Verify(r => r.DeleteAsync(31), Times.Once);
        _expiryAlertRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ExpiryAlert>()), Times.Never);
        _foodItemRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<FoodItem>()), Times.Never);
        _householdMemberRepositoryMock.Verify(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task SyncAlerts_ArchivedItem_RemovesAllSystemAlerts()
    {
        var foodItem = CreateFoodItem(Today.AddDays(-1), FoodStatus.Archived);
        var existingAlerts = new List<ExpiryAlert>
        {
            new ExpiryAlert { Id = 40, FoodItemId = 1, UserId = 1, AlertType = AlertType.Expired }
        };

        _expiryAlertRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(existingAlerts);

        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        _expiryAlertRepositoryMock.Verify(r => r.DeleteAsync(40), Times.Once);
        _expiryAlertRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ExpiryAlert>()), Times.Never);
    }

    [Fact]
    public async Task SyncAlerts_NearExpiryWithExistingSameTypeAlert_DoesNotDuplicate()
    {
        var foodItem = CreateFoodItem(Today.AddDays(2), FoodStatus.NearExpiry);
        var members = CreateHouseholdMembers(1, 1);
        var existingAlerts = new List<ExpiryAlert>
        {
            new ExpiryAlert { Id = 50, FoodItemId = 1, UserId = 1, AlertType = AlertType.NearExpiry }
        };

        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(members);
        _expiryAlertRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(existingAlerts);

        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        _expiryAlertRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ExpiryAlert>()), Times.Never);
        _expiryAlertRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SyncAlerts_StatusAlreadyCorrect_DoesNotUpdateFoodItem()
    {
        var foodItem = CreateFoodItem(Today.AddDays(10), FoodStatus.Fresh);
        var members = CreateHouseholdMembers(1, 1);

        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(members);

        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        _foodItemRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<FoodItem>()), Times.Never);
    }

    [Fact]
    public async Task SyncAlerts_ZeroQuantityFreshDate_TreatsAsConsumedAndRemovesAlerts()
    {
        var foodItem = CreateFoodItem(Today.AddDays(10), FoodStatus.Fresh, quantity: 0m);
        var existingAlerts = new List<ExpiryAlert>
        {
            new ExpiryAlert { Id = 60, FoodItemId = 1, UserId = 1, AlertType = AlertType.NearExpiry }
        };

        _expiryAlertRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(existingAlerts);

        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        foodItem.Status.Should().Be(FoodStatus.Consumed);
        _expiryAlertRepositoryMock.Verify(r => r.DeleteAsync(60), Times.Once);
        _foodItemRepositoryMock.Verify(r => r.UpdateAsync(It.Is<FoodItem>(f => f.Status == FoodStatus.Consumed)), Times.Once);
    }

    [Fact]
    public async Task ScanAndSyncAllAlerts_ProcessesAllFoodItems()
    {
        var foodItems = new List<FoodItem>
        {
            CreateFoodItem(Today.AddDays(10), FoodStatus.Fresh, id: 1),
            CreateFoodItem(Today.AddDays(2), FoodStatus.Fresh, id: 2),
            CreateFoodItem(Today.AddDays(-1), FoodStatus.NearExpiry, id: 3)
        };

        _foodItemRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(foodItems);
        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(CreateHouseholdMembers(1, 1));

        var result = await _syncService.ScanAndSyncAllAlertsAsync();

        result.TotalFoodItemsProcessed.Should().Be(3);
        result.FoodItemsStatusUpdated.Should().Be(2);
        result.NearExpiryAlertsCreated.Should().Be(1);
        result.ExpiredAlertsCreated.Should().Be(1);
    }

    [Fact]
    public async Task ScanAndSyncAllAlerts_NoItems_ReturnsZeroCounts()
    {
        _foodItemRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<FoodItem>());

        var result = await _syncService.ScanAndSyncAllAlertsAsync();

        result.TotalFoodItemsProcessed.Should().Be(0);
        result.FoodItemsStatusUpdated.Should().Be(0);
        result.AlertsCreated.Should().Be(0);
        result.AlertsRemoved.Should().Be(0);
    }
}

public class StatsServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;
    private readonly Mock<IFoodItemRepository> _foodItemRepositoryMock;
    private readonly Mock<IConsumptionRecordRepository> _consumptionRecordRepositoryMock;
    private readonly Mock<IHouseholdMemberRepository> _householdMemberRepositoryMock;
    private readonly Mock<IExpiryAlertRepository> _expiryAlertRepositoryMock;
    private readonly Mock<IHouseholdRepository> _householdRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly StatsService _statsService;

    private static DateTime Today => DateTime.Today;

    public StatsServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _foodItemRepositoryMock = new Mock<IFoodItemRepository>();
        _consumptionRecordRepositoryMock = new Mock<IConsumptionRecordRepository>();
        _householdMemberRepositoryMock = new Mock<IHouseholdMemberRepository>();
        _expiryAlertRepositoryMock = new Mock<IExpiryAlertRepository>();
        _householdRepositoryMock = new Mock<IHouseholdRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _unitOfWorkMock.Setup(u => u.FoodItems).Returns(_foodItemRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.ConsumptionRecords).Returns(_consumptionRecordRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.HouseholdMembers).Returns(_householdMemberRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.ExpiryAlerts).Returns(_expiryAlertRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Households).Returns(_householdRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);

        _foodItemRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<FoodItem, bool>>>()))
            .ReturnsAsync(new List<FoodItem>());
        _consumptionRecordRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ConsumptionRecord, bool>>>()))
            .ReturnsAsync(new List<ConsumptionRecord>());
        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(new List<HouseholdMember>());
        _expiryAlertRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(new List<ExpiryAlert>());
        _expiryAlertRepositoryMock
            .Setup(r => r.GetUnreadCountAsync(It.IsAny<int>()))
            .ReturnsAsync(0);
        _householdRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Household, bool>>>()))
            .ReturnsAsync(0);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _statsService = new StatsService(_unitOfWorkMock.Object, _mapper);
    }

    [Fact]
    public async Task GetTrendAsync_DatesAreSequential()
    {
        var query = new StatsTrendQueryDto
        {
            TimeRange = TimeRangeType.Last7Days,
            HouseholdId = 1
        };

        var result = await _statsService.GetTrendAsync(query);

        result.Should().NotBeNull();
        result.FoodAddedTrend.Should().HaveCount(7);
        result.ConsumptionTrend.Should().HaveCount(7);
        result.ExpiryTrend.Should().HaveCount(7);

        for (int i = 0; i < 7; i++)
        {
            var expectedDate = Today.AddDays(-6 + i);
            result.FoodAddedTrend[i].Date.Date.Should().Be(expectedDate);
            result.ConsumptionTrend[i].Date.Date.Should().Be(expectedDate);
            result.ExpiryTrend[i].Date.Date.Should().Be(expectedDate);
        }
    }

    [Fact]
    public async Task GetTrendAsync_WithUserIdAndNoDefaultHousehold_DoesNotThrowNullRef()
    {
        _userRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, Username = "user1" });
        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(new List<HouseholdMember>());

        var query = new StatsTrendQueryDto
        {
            TimeRange = TimeRangeType.Last7Days
        };

        var result = await _statsService.GetTrendAsync(query, userId: 1);

        result.Should().NotBeNull();
        result.FoodAddedTrend.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetTrendAsync_Last30Days_Returns30DataPoints()
    {
        var query = new StatsTrendQueryDto
        {
            TimeRange = TimeRangeType.Last30Days,
            HouseholdId = 1
        };

        var result = await _statsService.GetTrendAsync(query);

        result.FoodAddedTrend.Should().HaveCount(30);
        result.ConsumptionTrend.Should().HaveCount(30);
        result.ExpiryTrend.Should().HaveCount(30);
    }

    [Fact]
    public async Task GetTrendAsync_CustomDateRange_ReturnsCorrectDayCount()
    {
        var start = Today.AddDays(-2);
        var end = Today;
        var query = new StatsTrendQueryDto
        {
            TimeRange = TimeRangeType.Custom,
            StartDate = start,
            EndDate = end,
            HouseholdId = 1
        };

        var result = await _statsService.GetTrendAsync(query);

        result.FoodAddedTrend.Should().HaveCount(3);
        result.FoodAddedTrend.First().Date.Date.Should().Be(start);
        result.FoodAddedTrend.Last().Date.Date.Should().Be(end);
    }

    [Fact]
    public async Task GetTrendAsync_NoHouseholdAndNoUser_StillReturnsSequentialDates()
    {
        var query = new StatsTrendQueryDto
        {
            TimeRange = TimeRangeType.Last7Days
        };

        var result = await _statsService.GetTrendAsync(query);

        result.FoodAddedTrend.Should().HaveCount(7);
        result.FoodAddedTrend.All(t => t.Value == 0).Should().BeTrue();
    }

    [Fact]
    public async Task GetTrendAsync_WithUserIdResolvesMemberships_QueriesEachHousehold()
    {
        _userRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, Username = "user1" });
        var members = new List<HouseholdMember>
        {
            new HouseholdMember { HouseholdId = 1, UserId = 1 },
            new HouseholdMember { HouseholdId = 2, UserId = 1 }
        };
        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(members);

        var query = new StatsTrendQueryDto
        {
            TimeRange = TimeRangeType.Last7Days
        };

        var result = await _statsService.GetTrendAsync(query, userId: 1);

        result.Should().NotBeNull();
        result.FoodAddedTrend.Should().HaveCount(7);
    }

    [Fact]
    public async Task GetOverviewAsync_WithHouseholdId_CountsStatusesCorrectly()
    {
        var foodItems = new List<FoodItem>
        {
            new FoodItem { Id = 1, Status = FoodStatus.Fresh, Category = "乳制品", StorageLocation = StorageLocation.Fridge },
            new FoodItem { Id = 2, Status = FoodStatus.Fresh, Category = "蔬菜", StorageLocation = StorageLocation.Fridge },
            new FoodItem { Id = 3, Status = FoodStatus.NearExpiry, Category = "乳制品", StorageLocation = StorageLocation.Fridge },
            new FoodItem { Id = 4, Status = FoodStatus.Expired, Category = "肉类", StorageLocation = StorageLocation.Freezer },
            new FoodItem { Id = 5, Status = FoodStatus.Consumed, Category = "主食", StorageLocation = StorageLocation.Pantry }
        };
        _foodItemRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<FoodItem, bool>>>()))
            .ReturnsAsync(foodItems);

        var query = new StatsOverviewQueryDto { HouseholdId = 1 };

        var result = await _statsService.GetOverviewAsync(query);

        result.TotalFoodItems.Should().Be(5);
        result.FreshCount.Should().Be(2);
        result.NearExpiryCount.Should().Be(1);
        result.ExpiredCount.Should().Be(1);
        result.ConsumedCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOverviewAsync_NoFilter_CountsAcrossAllHouseholds()
    {
        var foodItems = new List<FoodItem>
        {
            new FoodItem { Id = 1, Status = FoodStatus.Fresh, Category = "乳制品", StorageLocation = StorageLocation.Fridge },
            new FoodItem { Id = 2, Status = FoodStatus.Expired, Category = "肉类", StorageLocation = StorageLocation.Freezer }
        };
        _foodItemRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<FoodItem, bool>>>()))
            .ReturnsAsync(foodItems);
        _householdRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Household, bool>>>()))
            .ReturnsAsync(3);

        var query = new StatsOverviewQueryDto();

        var result = await _statsService.GetOverviewAsync(query);

        result.TotalHouseholds.Should().Be(3);
        result.TotalFoodItems.Should().Be(2);
        result.FreshCount.Should().Be(1);
        result.ExpiredCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOverviewAsync_WithUserId_IncludesUnreadAlerts()
    {
        _expiryAlertRepositoryMock.Setup(r => r.GetUnreadCountAsync(1)).ReturnsAsync(5);
        var query = new StatsOverviewQueryDto { HouseholdId = 1 };

        var result = await _statsService.GetOverviewAsync(query, userId: 1);

        result.UnreadAlerts.Should().Be(5);
    }

    [Fact]
    public async Task GetOverviewAsync_CategoryStats_GroupedCorrectly()
    {
        var foodItems = new List<FoodItem>
        {
            new FoodItem { Id = 1, Status = FoodStatus.Fresh, Category = "乳制品", StorageLocation = StorageLocation.Fridge },
            new FoodItem { Id = 2, Status = FoodStatus.Fresh, Category = "乳制品", StorageLocation = StorageLocation.Fridge },
            new FoodItem { Id = 3, Status = FoodStatus.Fresh, Category = "蔬菜", StorageLocation = StorageLocation.Pantry }
        };
        _foodItemRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<FoodItem, bool>>>()))
            .ReturnsAsync(foodItems);

        var query = new StatsOverviewQueryDto { HouseholdId = 1 };
        var result = await _statsService.GetOverviewAsync(query);

        result.CategoryStats.Should().Contain(c => c.Category == "乳制品" && c.Count == 2);
        result.CategoryStats.Should().Contain(c => c.Category == "蔬菜" && c.Count == 1);
    }

    [Fact]
    public async Task GetOverviewAsync_LocationStats_GroupedCorrectly()
    {
        var foodItems = new List<FoodItem>
        {
            new FoodItem { Id = 1, Status = FoodStatus.Fresh, Category = "a", StorageLocation = StorageLocation.Fridge },
            new FoodItem { Id = 2, Status = FoodStatus.Fresh, Category = "b", StorageLocation = StorageLocation.Freezer }
        };
        _foodItemRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<FoodItem, bool>>>()))
            .ReturnsAsync(foodItems);

        var query = new StatsOverviewQueryDto { HouseholdId = 1 };
        var result = await _statsService.GetOverviewAsync(query);

        result.LocationStats.Should().Contain(l => l.Location == "Fridge" && l.Count == 1);
        result.LocationStats.Should().Contain(l => l.Location == "Freezer" && l.Count == 1);
    }

    [Fact]
    public async Task GetOverviewAsync_TotalConsumedQuantity_SumsCorrectly()
    {
        var records = new List<ConsumptionRecord>
        {
            new ConsumptionRecord { ConsumedQuantity = 2.5m },
            new ConsumptionRecord { ConsumedQuantity = 1.5m }
        };
        _consumptionRecordRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ConsumptionRecord, bool>>>()))
            .ReturnsAsync(records);

        var query = new StatsOverviewQueryDto { HouseholdId = 1 };
        var result = await _statsService.GetOverviewAsync(query);

        result.TotalConsumedQuantity.Should().Be(4m);
    }

    [Fact]
    public async Task GetTrendAsync_ExpiringItems_CountedOnExpiryDate()
    {
        var expiryDate = Today.AddDays(-6);
        var foodItems = new List<FoodItem>
        {
            new FoodItem { Id = 1, ExpiryDate = expiryDate, Status = FoodStatus.Expired, Category = "x", StorageLocation = StorageLocation.Fridge }
        };

        _foodItemRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<FoodItem, bool>>>()))
            .ReturnsAsync(foodItems);

        var query = new StatsTrendQueryDto
        {
            TimeRange = TimeRangeType.Last7Days,
            HouseholdId = 1
        };

        var result = await _statsService.GetTrendAsync(query);

        result.ExpiryTrend.Should().NotBeNull();
        result.ExpiryTrend.Sum(t => t.Value).Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetMemberActivityAsync_NotMember_ThrowsUnauthorized()
    {
        _householdMemberRepositoryMock
            .Setup(r => r.IsHouseholdMemberAsync(1, 1))
            .ReturnsAsync(false);

        var query = new MemberActivityQueryDto { HouseholdId = 1 };

        Func<Task> act = async () => await _statsService.GetMemberActivityAsync(query, userId: 1);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetMemberActivityAsync_ReturnsMemberStats()
    {
        _householdMemberRepositoryMock
            .Setup(r => r.IsHouseholdMemberAsync(1, 1))
            .ReturnsAsync(true);
        var members = new List<HouseholdMember>
        {
            new HouseholdMember { HouseholdId = 1, UserId = 1 }
        };
        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(members);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, Username = "testuser" });

        var query = new MemberActivityQueryDto { HouseholdId = 1 };
        var result = await _statsService.GetMemberActivityAsync(query, userId: 1);

        result.Should().HaveCount(1);
        result[0].Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetOverviewAsync_WithUserIdAndDefaultHousehold_UsesThatHousehold()
    {
        _userRepositoryMock.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, DefaultHouseholdId = 5 });

        var query = new StatsOverviewQueryDto();
        await _statsService.GetOverviewAsync(query, userId: 1);

        _foodItemRepositoryMock.Verify(r => r.FindAsync(It.IsAny<Expression<Func<FoodItem, bool>>>()), Times.AtLeastOnce);
    }
}

public class ConsumptionRecordServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;
    private readonly Mock<IAuditLogService> _auditLogServiceMock;
    private readonly Mock<IFoodItemRepository> _foodItemRepositoryMock;
    private readonly Mock<IConsumptionRecordRepository> _consumptionRecordRepositoryMock;
    private readonly Mock<IHouseholdMemberRepository> _householdMemberRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly ConsumptionRecordService _service;

    private static DateTime Today => DateTime.UtcNow.Date;

    public ConsumptionRecordServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _auditLogServiceMock = new Mock<IAuditLogService>();
        _foodItemRepositoryMock = new Mock<IFoodItemRepository>();
        _consumptionRecordRepositoryMock = new Mock<IConsumptionRecordRepository>();
        _householdMemberRepositoryMock = new Mock<IHouseholdMemberRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _unitOfWorkMock.Setup(u => u.FoodItems).Returns(_foodItemRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.ConsumptionRecords).Returns(_consumptionRecordRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.HouseholdMembers).Returns(_householdMemberRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Users).Returns(_userRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new User { Id = 1, Username = "user" });

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _service = new ConsumptionRecordService(_unitOfWorkMock.Object, _mapper, _auditLogServiceMock.Object);
    }

    private static FoodItem CreateFoodItem(decimal quantity = 10m, DateTime? expiryDate = null, FoodStatus status = FoodStatus.Fresh)
    {
        return new FoodItem
        {
            Id = 1,
            HouseholdId = 1,
            Name = "牛奶",
            Category = "乳制品",
            Quantity = quantity,
            Unit = "盒",
            ExpiryDate = expiryDate ?? Today.AddDays(10),
            Status = status
        };
    }

    [Fact]
    public async Task GetByIdAsync_RecordExists_ReturnsDto()
    {
        var record = new ConsumptionRecord { Id = 1, FoodItemId = 1, UserId = 1, ConsumedQuantity = 2 };
        _consumptionRecordRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(record);

        var result = await _service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.ConsumedQuantity.Should().Be(2);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ThrowsBusinessException()
    {
        _consumptionRecordRepositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ConsumptionRecord?)null);

        Func<Task> act = async () => await _service.GetByIdAsync(99);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("消耗记录不存在");
    }

    [Fact]
    public async Task CreateAsync_FullyConsumed_SetsStatusConsumed()
    {
        var foodItem = CreateFoodItem(quantity: 3m);
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(foodItem);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        var dto = new ConsumptionRecordCreateDto { FoodItemId = 1, ConsumedQuantity = 3m };

        var result = await _service.CreateAsync(dto, 1);

        result.Should().NotBeNull();
        foodItem.Quantity.Should().Be(0);
        foodItem.Status.Should().Be(FoodStatus.Consumed);
        _foodItemRepositoryMock.Verify(r => r.UpdateAsync(It.Is<FoodItem>(f => f.Status == FoodStatus.Consumed)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PartialConsumption_KeepsFreshStatus()
    {
        var foodItem = CreateFoodItem(quantity: 10m, expiryDate: Today.AddDays(10), status: FoodStatus.Fresh);
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(foodItem);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        var dto = new ConsumptionRecordCreateDto { FoodItemId = 1, ConsumedQuantity = 3m };

        await _service.CreateAsync(dto, 1);

        foodItem.Quantity.Should().Be(7m);
        foodItem.Status.Should().Be(FoodStatus.Fresh);
    }

    [Fact]
    public async Task CreateAsync_PartialConsumption_KeepsNearExpiryStatus()
    {
        var foodItem = CreateFoodItem(quantity: 5m, expiryDate: Today.AddDays(2), status: FoodStatus.NearExpiry);
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(foodItem);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        var dto = new ConsumptionRecordCreateDto { FoodItemId = 1, ConsumedQuantity = 2m };

        await _service.CreateAsync(dto, 1);

        foodItem.Quantity.Should().Be(3m);
        foodItem.Status.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public async Task CreateAsync_PartialConsumption_KeepsExpiredStatus()
    {
        var foodItem = CreateFoodItem(quantity: 5m, expiryDate: Today.AddDays(-2), status: FoodStatus.Expired);
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(foodItem);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        var dto = new ConsumptionRecordCreateDto { FoodItemId = 1, ConsumedQuantity = 1m };

        await _service.CreateAsync(dto, 1);

        foodItem.Quantity.Should().Be(4m);
        foodItem.Status.Should().Be(FoodStatus.Expired);
    }

    [Fact]
    public async Task CreateAsync_QuantityExceedsRemaining_Throws()
    {
        var foodItem = CreateFoodItem(quantity: 2m);
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(foodItem);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        var dto = new ConsumptionRecordCreateDto { FoodItemId = 1, ConsumedQuantity = 5m };

        Func<Task> act = async () => await _service.CreateAsync(dto, 1);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("消耗数量不能大于剩余数量");
    }

    [Fact]
    public async Task CreateAsync_FoodNotFound_Throws()
    {
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((FoodItem?)null);

        var dto = new ConsumptionRecordCreateDto { FoodItemId = 1, ConsumedQuantity = 1m };

        Func<Task> act = async () => await _service.CreateAsync(dto, 1);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("食材不存在");
    }

    [Fact]
    public async Task CreateAsync_NotMember_Throws()
    {
        var foodItem = CreateFoodItem();
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(foodItem);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(false);

        var dto = new ConsumptionRecordCreateDto { FoodItemId = 1, ConsumedQuantity = 1m };

        Func<Task> act = async () => await _service.CreateAsync(dto, 1);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateAsync_UsesProvidedConsumedAt()
    {
        var foodItem = CreateFoodItem();
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(foodItem);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        var consumedAt = Today.AddDays(-1);
        var dto = new ConsumptionRecordCreateDto { FoodItemId = 1, ConsumedQuantity = 1m, ConsumedAt = consumedAt };

        var result = await _service.CreateAsync(dto, 1);

        result.ConsumedAt.Should().Be(consumedAt);
    }

    [Fact]
    public async Task UpdateAsync_ChangesQuantity_UpdatesFoodItem()
    {
        var record = new ConsumptionRecord { Id = 1, FoodItemId = 1, UserId = 1, ConsumedQuantity = 2m };
        var foodItem = CreateFoodItem(quantity: 8m);
        _consumptionRecordRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(record);
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(foodItem);

        var dto = new ConsumptionRecordUpdateDto { ConsumedQuantity = 5m };

        await _service.UpdateAsync(1, dto, 1);

        foodItem.Quantity.Should().Be(5m);
        foodItem.Status.Should().Be(FoodStatus.Fresh);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_Throws()
    {
        _consumptionRecordRepositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ConsumptionRecord?)null);

        Func<Task> act = async () => await _service.UpdateAsync(99, new ConsumptionRecordUpdateDto(), 1);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("消耗记录不存在");
    }

    [Fact]
    public async Task UpdateAsync_NotOwner_Throws()
    {
        var record = new ConsumptionRecord { Id = 1, FoodItemId = 1, UserId = 2, ConsumedQuantity = 1m };
        _consumptionRecordRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(record);

        Func<Task> act = async () => await _service.UpdateAsync(1, new ConsumptionRecordUpdateDto(), 1);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteAsync_RestoresQuantityAndStatus()
    {
        var record = new ConsumptionRecord { Id = 1, FoodItemId = 1, UserId = 1, ConsumedQuantity = 3m };
        var foodItem = CreateFoodItem(quantity: 7m, expiryDate: Today.AddDays(10), status: FoodStatus.Fresh);
        _consumptionRecordRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(record);
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(foodItem);

        await _service.DeleteAsync(1, 1);

        foodItem.Quantity.Should().Be(10m);
        foodItem.Status.Should().Be(FoodStatus.Fresh);
        _consumptionRecordRepositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_Throws()
    {
        _consumptionRecordRepositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ConsumptionRecord?)null);

        Func<Task> act = async () => await _service.DeleteAsync(99, 1);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("消耗记录不存在");
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_Throws()
    {
        var record = new ConsumptionRecord { Id = 1, FoodItemId = 1, UserId = 2, ConsumedQuantity = 1m };
        _consumptionRecordRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(record);

        Func<Task> act = async () => await _service.DeleteAsync(1, 1);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}

public class ExpiryAlertServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;
    private readonly Mock<IFoodItemRepository> _foodItemRepositoryMock;
    private readonly Mock<IExpiryAlertRepository> _expiryAlertRepositoryMock;
    private readonly Mock<IHouseholdMemberRepository> _householdMemberRepositoryMock;
    private readonly ExpiryAlertService _service;

    public ExpiryAlertServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _foodItemRepositoryMock = new Mock<IFoodItemRepository>();
        _expiryAlertRepositoryMock = new Mock<IExpiryAlertRepository>();
        _householdMemberRepositoryMock = new Mock<IHouseholdMemberRepository>();

        _unitOfWorkMock.Setup(u => u.FoodItems).Returns(_foodItemRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.ExpiryAlerts).Returns(_expiryAlertRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.HouseholdMembers).Returns(_householdMemberRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _service = new ExpiryAlertService(_unitOfWorkMock.Object, _mapper);
    }

    [Fact]
    public async Task GetByIdAsync_Exists_ReturnsDto()
    {
        var alert = new ExpiryAlert { Id = 1, FoodItemId = 1, UserId = 1, AlertType = AlertType.NearExpiry };
        _expiryAlertRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);

        var result = await _service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.AlertType.Should().Be(AlertType.NearExpiry);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_Throws()
    {
        _expiryAlertRepositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ExpiryAlert?)null);

        Func<Task> act = async () => await _service.GetByIdAsync(99);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("提醒不存在");
    }

    [Fact]
    public async Task CreateAsync_Success_CreatesAlert()
    {
        var foodItem = new FoodItem { Id = 1, HouseholdId = 1, Name = "牛奶" };
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(foodItem);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        var dto = new ExpiryAlertCreateDto { FoodItemId = 1, AlertType = AlertType.NearExpiry };

        var result = await _service.CreateAsync(dto, 1);

        result.Should().NotBeNull();
        _expiryAlertRepositoryMock.Verify(r => r.AddAsync(It.Is<ExpiryAlert>(a => a.UserId == 1)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_FoodNotFound_Throws()
    {
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((FoodItem?)null);

        var dto = new ExpiryAlertCreateDto { FoodItemId = 1 };

        Func<Task> act = async () => await _service.CreateAsync(dto, 1);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("食材不存在");
    }

    [Fact]
    public async Task CreateAsync_NotMember_Throws()
    {
        var foodItem = new FoodItem { Id = 1, HouseholdId = 1 };
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(foodItem);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(false);

        var dto = new ExpiryAlertCreateDto { FoodItemId = 1 };

        Func<Task> act = async () => await _service.CreateAsync(dto, 1);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task UpdateAsync_Success_UpdatesAlert()
    {
        var alert = new ExpiryAlert { Id = 1, UserId = 1, AlertType = AlertType.NearExpiry };
        _expiryAlertRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);

        var dto = new ExpiryAlertUpdateDto { AlertType = AlertType.Expired };

        var result = await _service.UpdateAsync(1, dto, 1);

        result.Should().NotBeNull();
        _expiryAlertRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ExpiryAlert>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_Throws()
    {
        _expiryAlertRepositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((ExpiryAlert?)null);

        Func<Task> act = async () => await _service.UpdateAsync(99, new ExpiryAlertUpdateDto(), 1);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("提醒不存在");
    }

    [Fact]
    public async Task UpdateAsync_NotOwner_Throws()
    {
        var alert = new ExpiryAlert { Id = 1, UserId = 2 };
        _expiryAlertRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);

        Func<Task> act = async () => await _service.UpdateAsync(1, new ExpiryAlertUpdateDto(), 1);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteAsync_Success()
    {
        var alert = new ExpiryAlert { Id = 1, UserId = 1 };
        _expiryAlertRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);

        await _service.DeleteAsync(1, 1);

        _expiryAlertRepositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NotOwner_Throws()
    {
        var alert = new ExpiryAlert { Id = 1, UserId = 2 };
        _expiryAlertRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);

        Func<Task> act = async () => await _service.DeleteAsync(1, 1);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task MarkAsReadAsync_Success()
    {
        var alert = new ExpiryAlert { Id = 1, UserId = 1 };
        _expiryAlertRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);

        await _service.MarkAsReadAsync(1, 1);

        _expiryAlertRepositoryMock.Verify(r => r.MarkAsReadAsync(1), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_NotOwner_Throws()
    {
        var alert = new ExpiryAlert { Id = 1, UserId = 2 };
        _expiryAlertRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(alert);

        Func<Task> act = async () => await _service.MarkAsReadAsync(1, 1);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCount()
    {
        _expiryAlertRepositoryMock.Setup(r => r.GetUnreadCountAsync(1)).ReturnsAsync(7);

        var result = await _service.GetUnreadCountAsync(1);

        result.Should().Be(7);
    }

    [Fact]
    public async Task BatchDeleteAsync_DeletesOnlyOwnedAlerts()
    {
        var alerts = new List<ExpiryAlert>
        {
            new ExpiryAlert { Id = 1, UserId = 1 },
            new ExpiryAlert { Id = 2, UserId = 1 }
        };
        _expiryAlertRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(alerts);

        await _service.BatchDeleteAsync(new[] { 1, 2, 3 }, 1);

        _expiryAlertRepositoryMock.Verify(r => r.DeleteByIdsAsync(It.Is<IEnumerable<int>>(ids => ids.Count() == 2)), Times.Once);
    }
}

public class NotificationServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<IExpiryAlertRepository> _expiryAlertRepositoryMock;
    private readonly Mock<IHouseholdMemberRepository> _householdMemberRepositoryMock;
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _expiryAlertRepositoryMock = new Mock<IExpiryAlertRepository>();
        _householdMemberRepositoryMock = new Mock<IHouseholdMemberRepository>();

        _unitOfWorkMock.Setup(u => u.Notifications).Returns(_notificationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.ExpiryAlerts).Returns(_expiryAlertRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.HouseholdMembers).Returns(_householdMemberRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        _notificationRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
            .ReturnsAsync(new List<Notification>());
        _expiryAlertRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(new List<ExpiryAlert>());

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _service = new NotificationService(_unitOfWorkMock.Object, _mapper);
    }

    [Fact]
    public async Task GetByIdAsync_ExistsAndOwned_ReturnsDto()
    {
        var notification = new Notification { Id = 1, UserId = 1, Title = "测试通知" };
        _notificationRepositoryMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(notification);

        var result = await _service.GetByIdAsync(1, 1);

        result.Should().NotBeNull();
        result.Title.Should().Be("测试通知");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_Throws()
    {
        _notificationRepositoryMock.Setup(r => r.GetByIdAsync(99L)).ReturnsAsync((Notification?)null);

        Func<Task> act = async () => await _service.GetByIdAsync(99, 1);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("通知不存在");
    }

    [Fact]
    public async Task GetByIdAsync_WrongUser_Throws()
    {
        var notification = new Notification { Id = 1, UserId = 2 };
        _notificationRepositoryMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(notification);

        Func<Task> act = async () => await _service.GetByIdAsync(1, 1);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateAsync_Success()
    {
        var dto = new NotificationCreateDto
        {
            UserId = 1,
            Type = NotificationType.System,
            Category = NotificationCategory.SystemAnnouncement,
            Title = "欢迎",
            Content = "欢迎使用"
        };

        var result = await _service.CreateAsync(dto);

        result.Should().NotBeNull();
        result.Title.Should().Be("欢迎");
        _notificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Once);
    }

    [Fact]
    public async Task BatchCreateAsync_EmptyUserIds_Throws()
    {
        var dto = new NotificationBatchCreateDto { UserIds = new List<int>() };

        Func<Task> act = async () => await _service.BatchCreateAsync(dto);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("请指定接收通知的用户");
    }

    [Fact]
    public async Task BatchCreateAsync_DuplicateUserIds_Deduplicated()
    {
        var dto = new NotificationBatchCreateDto
        {
            UserIds = new List<int> { 1, 1, 2, 2 },
            Type = NotificationType.System,
            Category = NotificationCategory.SystemAnnouncement,
            Title = "通知",
            Content = "内容"
        };

        await _service.BatchCreateAsync(dto);

        _notificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAsync_WrongUser_Throws()
    {
        var notification = new Notification { Id = 1, UserId = 2 };
        _notificationRepositoryMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(notification);

        Func<Task> act = async () => await _service.UpdateAsync(1, new NotificationUpdateDto(), 1);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task DeleteAsync_WrongUser_Throws()
    {
        var notification = new Notification { Id = 1, UserId = 2 };
        _notificationRepositoryMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(notification);

        Func<Task> act = async () => await _service.DeleteAsync(1, 1);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task MarkAsReadAsync_Success()
    {
        var notification = new Notification { Id = 1, UserId = 1 };
        _notificationRepositoryMock.Setup(r => r.GetByIdAsync(1L)).ReturnsAsync(notification);

        await _service.MarkAsReadAsync(1, 1);

        _notificationRepositoryMock.Verify(r => r.MarkAsReadAsync(1L), Times.Once);
    }

    [Fact]
    public async Task BatchDeleteAsync_NoOwnedNotifications_Throws()
    {
        _notificationRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
            .ReturnsAsync(new List<Notification>());

        Func<Task> act = async () => await _service.BatchDeleteAsync(new long[] { 1, 2 }, 1);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("没有可删除的通知");
    }

    [Fact]
    public async Task GetTotalUnreadCountAsync_ReturnsCount()
    {
        _notificationRepositoryMock.Setup(r => r.GetTotalUnreadCountAsync(1)).ReturnsAsync(3);

        var result = await _service.GetTotalUnreadCountAsync(1);

        result.Should().Be(3);
    }

    [Fact]
    public async Task SyncExpiryAlerts_NearExpiry_CreatesNearExpiryNotification()
    {
        var foodItem = new FoodItem { Id = 1, HouseholdId = 1, Name = "牛奶" };
        var alerts = new List<ExpiryAlert>
        {
            new ExpiryAlert { Id = 1, UserId = 1, AlertType = AlertType.NearExpiry, FoodItem = foodItem, FoodItemId = 1 }
        };
        _expiryAlertRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(alerts);

        await _service.SyncExpiryAlertsToNotificationsAsync(1);

        _notificationRepositoryMock.Verify(r => r.AddAsync(It.Is<Notification>(
            n => n.Category == NotificationCategory.NearExpiry && n.Title == "食材临期提醒")), Times.Once);
    }

    [Fact]
    public async Task SyncExpiryAlerts_Expired_CreatesExpiredNotification()
    {
        var foodItem = new FoodItem { Id = 1, HouseholdId = 1, Name = "鸡胸肉" };
        var alerts = new List<ExpiryAlert>
        {
            new ExpiryAlert { Id = 1, UserId = 1, AlertType = AlertType.Expired, FoodItem = foodItem, FoodItemId = 1 }
        };
        _expiryAlertRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(alerts);

        await _service.SyncExpiryAlertsToNotificationsAsync(1);

        _notificationRepositoryMock.Verify(r => r.AddAsync(It.Is<Notification>(
            n => n.Category == NotificationCategory.Expired && n.Title == "食材过期提醒")), Times.Once);
    }

    [Fact]
    public async Task SyncExpiryAlerts_AlreadySynced_DoesNotDuplicate()
    {
        var alerts = new List<ExpiryAlert>
        {
            new ExpiryAlert { Id = 1, UserId = 1, AlertType = AlertType.NearExpiry }
        };
        var existingNotifications = new List<Notification>
        {
            new Notification { Id = 10, UserId = 1, Type = NotificationType.ExpiryAlert, RelatedEntityId = 1, RelatedEntityType = "ExpiryAlert" }
        };
        _expiryAlertRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .ReturnsAsync(alerts);
        _notificationRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Notification, bool>>>()))
            .ReturnsAsync(existingNotifications);

        await _service.SyncExpiryAlertsToNotificationsAsync(1);

        _notificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task GenerateHouseholdActivity_NotifiesAllMembersExceptOperator()
    {
        var members = new List<HouseholdMember>
        {
            new HouseholdMember { HouseholdId = 1, UserId = 1 },
            new HouseholdMember { HouseholdId = 1, UserId = 2 },
            new HouseholdMember { HouseholdId = 1, UserId = 3 }
        };
        _householdMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .ReturnsAsync(members);

        await _service.GenerateHouseholdActivityNotificationAsync(1, NotificationCategory.FoodItemAdded, "标题", "内容", operatorId: 1);

        _notificationRepositoryMock.Verify(r => r.AddAsync(It.Is<Notification>(n => n.UserId == 2)), Times.Once);
        _notificationRepositoryMock.Verify(r => r.AddAsync(It.Is<Notification>(n => n.UserId == 3)), Times.Once);
        _notificationRepositoryMock.Verify(r => r.AddAsync(It.Is<Notification>(n => n.UserId == 1)), Times.Never);
    }
}

public class ShoppingListServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMapper _mapper;
    private readonly Mock<IExpiryAlertSyncService> _alertSyncServiceMock;
    private readonly Mock<IShoppingListRepository> _shoppingListRepositoryMock;
    private readonly Mock<IShoppingListItemRepository> _shoppingListItemRepositoryMock;
    private readonly Mock<IHouseholdMemberRepository> _householdMemberRepositoryMock;
    private readonly Mock<IFoodItemRepository> _foodItemRepositoryMock;
    private readonly ShoppingListService _service;

    private static DateTime Today => DateTime.UtcNow.Date;

    public ShoppingListServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _alertSyncServiceMock = new Mock<IExpiryAlertSyncService>();
        _shoppingListRepositoryMock = new Mock<IShoppingListRepository>();
        _shoppingListItemRepositoryMock = new Mock<IShoppingListItemRepository>();
        _householdMemberRepositoryMock = new Mock<IHouseholdMemberRepository>();
        _foodItemRepositoryMock = new Mock<IFoodItemRepository>();

        _unitOfWorkMock.Setup(u => u.ShoppingLists).Returns(_shoppingListRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.ShoppingListItems).Returns(_shoppingListItemRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.HouseholdMembers).Returns(_householdMemberRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.FoodItems).Returns(_foodItemRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        _mapper = config.CreateMapper();

        _service = new ShoppingListService(_unitOfWorkMock.Object, _mapper, _alertSyncServiceMock.Object);
    }

    private static ShoppingList CreateShoppingList(bool isCompleted = false, int householdId = 1, int id = 1)
    {
        return new ShoppingList
        {
            Id = id,
            HouseholdId = householdId,
            Name = "周采购",
            IsCompleted = isCompleted,
            Items = new List<ShoppingListItem>()
        };
    }

    [Fact]
    public async Task GetByIdAsync_Exists_ReturnsDto()
    {
        var list = CreateShoppingList();
        _shoppingListRepositoryMock.Setup(r => r.GetWithItemsByIdAsync(1)).ReturnsAsync(list);

        var result = await _service.GetByIdAsync(1);

        result.Should().NotBeNull();
        result.Name.Should().Be("周采购");
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_Throws()
    {
        _shoppingListRepositoryMock.Setup(r => r.GetWithItemsByIdAsync(99)).ReturnsAsync((ShoppingList?)null);

        Func<Task> act = async () => await _service.GetByIdAsync(99);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("采购清单不存在");
    }

    [Fact]
    public async Task CreateAsync_NotMember_Throws()
    {
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(false);

        var dto = new ShoppingListCreateDto { HouseholdId = 1, Name = "清单" };

        Func<Task> act = async () => await _service.CreateAsync(dto, 1);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateAsync_Success()
    {
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        var dto = new ShoppingListCreateDto { HouseholdId = 1, Name = "新清单" };

        var result = await _service.CreateAsync(dto, 1);

        result.Should().NotBeNull();
        result.Name.Should().Be("新清单");
        _shoppingListRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ShoppingList>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CompletedList_Throws()
    {
        var list = CreateShoppingList(isCompleted: true);
        _shoppingListRepositoryMock.Setup(r => r.GetWithItemsByIdAsync(1)).ReturnsAsync(list);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        Func<Task> act = async () => await _service.UpdateAsync(1, new ShoppingListUpdateDto(), 1);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("已完成的采购清单无法修改");
    }

    [Fact]
    public async Task DeleteAsync_Success()
    {
        var list = CreateShoppingList();
        _shoppingListRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(list);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        await _service.DeleteAsync(1, 1);

        _shoppingListRepositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_CompletedList_Throws()
    {
        var list = CreateShoppingList(isCompleted: true);
        _shoppingListRepositoryMock.Setup(r => r.GetWithItemsByIdAsync(1)).ReturnsAsync(list);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        Func<Task> act = async () => await _service.AddItemAsync(1, new ShoppingListItemCreateDto(), 1);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("已完成的采购清单无法添加项");
    }

    [Fact]
    public async Task AddItemAsync_Success()
    {
        var list = CreateShoppingList();
        _shoppingListRepositoryMock.Setup(r => r.GetWithItemsByIdAsync(1)).ReturnsAsync(list);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        var dto = new ShoppingListItemCreateDto { Name = "鸡蛋", Category = "蛋类", Quantity = 10, Unit = "个" };

        await _service.AddItemAsync(1, dto, 1);

        _shoppingListItemRepositoryMock.Verify(r => r.AddAsync(It.Is<ShoppingListItem>(i => i.Name == "鸡蛋")), Times.Once);
    }

    [Fact]
    public async Task RemoveItemAsync_Success()
    {
        var item = new ShoppingListItem { Id = 1, ShoppingListId = 1 };
        var list = CreateShoppingList();
        _shoppingListItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _shoppingListRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(list);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        await _service.RemoveItemAsync(1, 1);

        _shoppingListItemRepositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task ToggleItemPurchasedAsync_Success()
    {
        var item = new ShoppingListItem { Id = 1, ShoppingListId = 1, IsPurchased = false };
        var list = CreateShoppingList();
        _shoppingListItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _shoppingListRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(list);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        await _service.ToggleItemPurchasedAsync(1, true, 1);

        item.IsPurchased.Should().BeTrue();
        _shoppingListItemRepositoryMock.Verify(r => r.UpdateAsync(It.Is<ShoppingListItem>(i => i.IsPurchased)), Times.Once);
    }

    [Fact]
    public async Task ToggleItemPurchasedAsync_CompletedList_Throws()
    {
        var item = new ShoppingListItem { Id = 1, ShoppingListId = 1 };
        var list = CreateShoppingList(isCompleted: true);
        _shoppingListItemRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(item);
        _shoppingListRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(list);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        Func<Task> act = async () => await _service.ToggleItemPurchasedAsync(1, true, 1);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("已完成的采购清单无法修改");
    }

    [Fact]
    public async Task ConvertToFoodItems_FreshExpiryDays_CalculatesFreshStatus()
    {
        var list = CreateShoppingList();
        list.Items = new List<ShoppingListItem>
        {
            new ShoppingListItem { Id = 1, Name = "大米", Category = "主食", Quantity = 5, Unit = "kg", ExpiryDays = 30, StorageLocation = StorageLocation.Pantry }
        };
        _shoppingListRepositoryMock.Setup(r => r.GetWithItemsByIdAsync(1)).ReturnsAsync(list);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        var dto = new ShoppingListConvertDto { ShoppingListId = 1, PurchaseAll = true };

        var result = await _service.ConvertToFoodItemsAsync(dto, 1);

        result.Should().HaveCount(1);
        result[0].Status.Should().Be(FoodStatus.Fresh);
        list.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ConvertToFoodItems_NearExpiryDays_SetsNearExpiryStatus()
    {
        var list = CreateShoppingList();
        list.Items = new List<ShoppingListItem>
        {
            new ShoppingListItem { Id = 1, Name = "牛奶", Category = "乳制品", Quantity = 2, Unit = "盒", ExpiryDays = 3, StorageLocation = StorageLocation.Fridge }
        };
        _shoppingListRepositoryMock.Setup(r => r.GetWithItemsByIdAsync(1)).ReturnsAsync(list);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        var dto = new ShoppingListConvertDto { ShoppingListId = 1, PurchaseAll = true };

        var result = await _service.ConvertToFoodItemsAsync(dto, 1);

        result[0].Status.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public async Task ConvertToFoodItems_NoPurchasedItems_Throws()
    {
        var list = CreateShoppingList();
        list.Items = new List<ShoppingListItem>
        {
            new ShoppingListItem { Id = 1, Name = "x", Category = "y", Quantity = 1, Unit = "z", IsPurchased = false }
        };
        _shoppingListRepositoryMock.Setup(r => r.GetWithItemsByIdAsync(1)).ReturnsAsync(list);
        _householdMemberRepositoryMock.Setup(r => r.IsHouseholdMemberAsync(1, 1)).ReturnsAsync(true);

        var dto = new ShoppingListConvertDto { ShoppingListId = 1, PurchaseAll = false };

        Func<Task> act = async () => await _service.ConvertToFoodItemsAsync(dto, 1);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("没有可转换的清单项");
    }
}
