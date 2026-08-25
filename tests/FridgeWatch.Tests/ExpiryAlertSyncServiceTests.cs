using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FridgeWatch.Application.Services;
using FridgeWatch.Domain.Entities;
using FridgeWatch.Domain.Enums;
using FridgeWatch.Domain.Interfaces;

namespace FridgeWatch.Tests;

public class ExpiryAlertSyncServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<ExpiryAlertSyncService>> _loggerMock;
    private readonly ExpiryAlertSyncService _syncService;

    private readonly List<FoodItem> _foodItems = new();
    private readonly List<HouseholdMember> _members = new();
    private readonly List<ExpiryAlert> _existingAlerts = new();
    private readonly List<ExpiryAlert> _addedAlerts = new();
    private readonly List<int> _deletedAlertIds = new();

    private static DateTime Today => DateTime.UtcNow.Date;

    public ExpiryAlertSyncServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ExpiryAlertSyncService>>();
        _syncService = new ExpiryAlertSyncService(_unitOfWorkMock.Object, _loggerMock.Object);

        _unitOfWorkMock.Setup(u => u.FoodItems.GetAllAsync())
            .ReturnsAsync(_foodItems);

        _unitOfWorkMock.Setup(u => u.HouseholdMembers.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()))
            .Returns((Expression<Func<HouseholdMember, bool>> predicate) =>
                Task.FromResult(_members.Where(predicate.Compile()).ToList()));

        _unitOfWorkMock.Setup(u => u.ExpiryAlerts.FindAsync(It.IsAny<Expression<Func<ExpiryAlert, bool>>>()))
            .Returns((Expression<Func<ExpiryAlert, bool>> predicate) =>
                Task.FromResult(_existingAlerts.Where(predicate.Compile()).ToList()));

        _unitOfWorkMock.Setup(u => u.ExpiryAlerts.AddAsync(It.IsAny<ExpiryAlert>()))
            .Callback<ExpiryAlert>(_addedAlerts.Add)
            .Returns((ExpiryAlert alert) => Task.FromResult(alert));

        _unitOfWorkMock.Setup(u => u.ExpiryAlerts.DeleteAsync(It.IsAny<int>()))
            .Callback<int>(id =>
            {
                _deletedAlertIds.Add(id);
                _existingAlerts.RemoveAll(a => a.Id == id);
            })
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.FoodItems.UpdateAsync(It.IsAny<FoodItem>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);
    }

    private void AddMembers(int householdId, params int[] userIds)
    {
        foreach (var userId in userIds)
        {
            _members.Add(new HouseholdMember
            {
                Id = _members.Count + 1,
                HouseholdId = householdId,
                UserId = userId
            });
        }
    }

    private static FoodItem CreateFoodItem(int id, int householdId, DateTime expiryDate, FoodStatus status, decimal quantity = 1)
    {
        return new FoodItem
        {
            Id = id,
            HouseholdId = householdId,
            Name = $"食材{id}",
            Category = "测试分类",
            StorageLocation = StorageLocation.Fridge,
            PurchaseDate = expiryDate.AddDays(-7),
            ExpiryDate = expiryDate,
            Quantity = quantity,
            Unit = "个",
            Status = status
        };
    }

    private ExpiryAlert AddExistingAlert(int id, int foodItemId, int userId, AlertType alertType)
    {
        var alert = new ExpiryAlert
        {
            Id = id,
            FoodItemId = foodItemId,
            UserId = userId,
            AlertType = alertType,
            AlertDate = DateTime.UtcNow,
            IsRead = false
        };
        _existingAlerts.Add(alert);
        return alert;
    }

    [Fact]
    public async Task ScanAndSyncAllAlertsAsync_ExactlyThreeDaysLeft_CreatesNearExpiryAlertsForAllMembers_AndUpdatesStatus()
    {
        // Arrange
        var foodItem = CreateFoodItem(1, 1, Today.AddDays(3), FoodStatus.Fresh);
        _foodItems.Add(foodItem);
        AddMembers(1, 10, 11);

        // Act
        var result = await _syncService.ScanAndSyncAllAlertsAsync();

        // Assert
        result.TotalFoodItemsProcessed.Should().Be(1);
        result.FoodItemsStatusUpdated.Should().Be(1);
        result.NearExpiryAlertsCreated.Should().Be(2);
        result.ExpiredAlertsCreated.Should().Be(0);
        result.AlertsCreated.Should().Be(2);
        result.AlertsRemoved.Should().Be(0);

        _addedAlerts.Should().HaveCount(2);
        _addedAlerts.Should().OnlyContain(a => a.AlertType == AlertType.NearExpiry);
        _addedAlerts.Select(a => a.UserId).Should().BeEquivalentTo(new[] { 10, 11 });
        _addedAlerts.Should().OnlyContain(a => a.FoodItemId == 1 && !a.IsRead);

        foodItem.Status.Should().Be(FoodStatus.NearExpiry);
        foodItem.UpdatedAt.Should().NotBeNull();
        _unitOfWorkMock.Verify(u => u.FoodItems.UpdateAsync(foodItem), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ScanAndSyncAllAlertsAsync_ExpiredOneDay_CreatesExpiredAlerts_AndUpdatesStatusToExpired()
    {
        // Arrange
        var foodItem = CreateFoodItem(1, 1, Today.AddDays(-1), FoodStatus.Fresh);
        _foodItems.Add(foodItem);
        AddMembers(1, 10, 11);

        // Act
        var result = await _syncService.ScanAndSyncAllAlertsAsync();

        // Assert
        result.ExpiredAlertsCreated.Should().Be(2);
        result.NearExpiryAlertsCreated.Should().Be(0);
        result.AlertsCreated.Should().Be(2);

        _addedAlerts.Should().HaveCount(2);
        _addedAlerts.Should().OnlyContain(a => a.AlertType == AlertType.Expired);
        _addedAlerts.Select(a => a.UserId).Should().BeEquivalentTo(new[] { 10, 11 });

        foodItem.Status.Should().Be(FoodStatus.Expired);
        _unitOfWorkMock.Verify(u => u.FoodItems.UpdateAsync(foodItem), Times.Once);
    }

    [Fact]
    public async Task ScanAndSyncAllAlertsAsync_FourDaysLeft_RemainsFresh_AndRemovesExistingAlerts()
    {
        // Arrange
        var foodItem = CreateFoodItem(1, 1, Today.AddDays(4), FoodStatus.Fresh);
        _foodItems.Add(foodItem);
        AddMembers(1, 10);
        var staleAlert = AddExistingAlert(100, 1, 10, AlertType.NearExpiry);

        // Act
        var result = await _syncService.ScanAndSyncAllAlertsAsync();

        // Assert
        foodItem.Status.Should().Be(FoodStatus.Fresh);
        result.FoodItemsStatusUpdated.Should().Be(0);
        _addedAlerts.Should().BeEmpty();
        _deletedAlertIds.Should().Contain(staleAlert.Id);
        result.AlertsRemoved.Should().Be(1);
        _unitOfWorkMock.Verify(u => u.FoodItems.UpdateAsync(It.IsAny<FoodItem>()), Times.Never);
    }

    [Fact]
    public async Task ScanAndSyncAllAlertsAsync_ExistingSameTypeAlert_IsKeptAndNotDuplicated()
    {
        // Arrange
        var foodItem = CreateFoodItem(1, 1, Today.AddDays(2), FoodStatus.NearExpiry);
        _foodItems.Add(foodItem);
        AddMembers(1, 10, 11);
        AddExistingAlert(100, 1, 10, AlertType.NearExpiry);

        // Act
        var result = await _syncService.ScanAndSyncAllAlertsAsync();

        // Assert
        result.NearExpiryAlertsCreated.Should().Be(1);
        _addedAlerts.Should().HaveCount(1);
        _addedAlerts[0].UserId.Should().Be(11);
        _addedAlerts[0].AlertType.Should().Be(AlertType.NearExpiry);
        _deletedAlertIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.FoodItems.UpdateAsync(It.IsAny<FoodItem>()), Times.Never);
    }

    [Fact]
    public async Task ScanAndSyncAllAlertsAsync_TransitionFromNearExpiryToExpired_RemovesNearExpiryAlerts_AndCreatesExpiredAlerts()
    {
        // Arrange
        var foodItem = CreateFoodItem(1, 1, Today.AddDays(-1), FoodStatus.NearExpiry);
        _foodItems.Add(foodItem);
        AddMembers(1, 10, 11);
        AddExistingAlert(100, 1, 10, AlertType.NearExpiry);
        AddExistingAlert(101, 1, 11, AlertType.NearExpiry);

        // Act
        var result = await _syncService.ScanAndSyncAllAlertsAsync();

        // Assert
        foodItem.Status.Should().Be(FoodStatus.Expired);
        _addedAlerts.Should().HaveCount(2);
        _addedAlerts.Should().OnlyContain(a => a.AlertType == AlertType.Expired);
        _addedAlerts.Select(a => a.UserId).Should().BeEquivalentTo(new[] { 10, 11 });
        _deletedAlertIds.Should().BeEquivalentTo(new[] { 100, 101 });
        result.AlertsRemoved.Should().Be(2);
        result.ExpiredAlertsCreated.Should().Be(2);
        result.NearExpiryAlertsCreated.Should().Be(0);
    }

    [Fact]
    public async Task ScanAndSyncAllAlertsAsync_ConsumedItem_RemovesAllSystemAlerts_AndSkipsStatusUpdate()
    {
        // Arrange
        var foodItem = CreateFoodItem(1, 1, Today.AddDays(2), FoodStatus.Consumed);
        _foodItems.Add(foodItem);
        AddMembers(1, 10);
        AddExistingAlert(100, 1, 10, AlertType.NearExpiry);

        // Act
        var result = await _syncService.ScanAndSyncAllAlertsAsync();

        // Assert
        foodItem.Status.Should().Be(FoodStatus.Consumed);
        _addedAlerts.Should().BeEmpty();
        _deletedAlertIds.Should().Contain(100);
        result.AlertsRemoved.Should().Be(1);
        _unitOfWorkMock.Verify(u => u.FoodItems.UpdateAsync(It.IsAny<FoodItem>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.HouseholdMembers.FindAsync(It.IsAny<Expression<Func<HouseholdMember, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task ScanAndSyncAllAlertsAsync_ArchivedItem_RemovesAllSystemAlerts_AndSkipsStatusUpdate()
    {
        // Arrange
        var foodItem = CreateFoodItem(1, 1, Today.AddDays(-10), FoodStatus.Archived);
        _foodItems.Add(foodItem);
        AddMembers(1, 10);
        AddExistingAlert(100, 1, 10, AlertType.Expired);

        // Act
        var result = await _syncService.ScanAndSyncAllAlertsAsync();

        // Assert
        foodItem.Status.Should().Be(FoodStatus.Archived);
        _addedAlerts.Should().BeEmpty();
        _deletedAlertIds.Should().Contain(100);
        result.AlertsRemoved.Should().Be(1);
        _unitOfWorkMock.Verify(u => u.FoodItems.UpdateAsync(It.IsAny<FoodItem>()), Times.Never);
    }

    [Fact]
    public async Task SyncAlertsForFoodItemAsync_ExpiresToday_CreatesNearExpiryAlert()
    {
        // Arrange
        var foodItem = CreateFoodItem(1, 1, Today, FoodStatus.Fresh);
        AddMembers(1, 10);

        // Act
        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        // Assert
        _addedAlerts.Should().HaveCount(1);
        _addedAlerts[0].AlertType.Should().Be(AlertType.NearExpiry);
        _addedAlerts[0].UserId.Should().Be(10);
        foodItem.Status.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public async Task SyncAlertsForFoodItemAsync_FreshItem_RemovesExistingAlerts_AndCreatesNone()
    {
        // Arrange
        var foodItem = CreateFoodItem(1, 1, Today.AddDays(10), FoodStatus.Fresh);
        AddMembers(1, 10, 11);
        AddExistingAlert(100, 1, 10, AlertType.NearExpiry);

        // Act
        await _syncService.SyncAlertsForFoodItemAsync(foodItem);

        // Assert
        _addedAlerts.Should().BeEmpty();
        _deletedAlertIds.Should().Contain(100);
        foodItem.Status.Should().Be(FoodStatus.Fresh);
        _unitOfWorkMock.Verify(u => u.FoodItems.UpdateAsync(It.IsAny<FoodItem>()), Times.Never);
    }

    [Fact]
    public async Task ScanAndSyncAllAlertsAsync_MixedItems_AggregatesCountsCorrectly()
    {
        // Arrange
        _foodItems.Add(CreateFoodItem(1, 1, Today.AddDays(10), FoodStatus.Fresh));
        _foodItems.Add(CreateFoodItem(2, 1, Today.AddDays(3), FoodStatus.Fresh));
        _foodItems.Add(CreateFoodItem(3, 1, Today.AddDays(-2), FoodStatus.Expired));
        AddMembers(1, 10, 11);

        // Act
        var result = await _syncService.ScanAndSyncAllAlertsAsync();

        // Assert
        result.TotalFoodItemsProcessed.Should().Be(3);
        result.FoodItemsStatusUpdated.Should().Be(1);
        result.NearExpiryAlertsCreated.Should().Be(2);
        result.ExpiredAlertsCreated.Should().Be(2);
        result.AlertsCreated.Should().Be(4);
        result.AlertsRemoved.Should().Be(0);

        _foodItems[0].Status.Should().Be(FoodStatus.Fresh);
        _foodItems[1].Status.Should().Be(FoodStatus.NearExpiry);
        _foodItems[2].Status.Should().Be(FoodStatus.Expired);
        _addedAlerts.Should().HaveCount(4);
    }
}
