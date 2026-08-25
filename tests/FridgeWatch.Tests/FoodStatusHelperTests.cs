using FluentAssertions;
using FridgeWatch.Domain.Common;
using FridgeWatch.Domain.Enums;
using Xunit;

namespace FridgeWatch.Tests;

public class FoodStatusHelperTests
{
    private static DateTime Today => DateTime.UtcNow.Date;

    [Fact]
    public void CalculateStatus_ExpiredOneDayAgo_ReturnsExpired()
    {
        // Arrange
        var expiryDate = Today.AddDays(-1);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.Expired);
    }

    [Fact]
    public void CalculateStatus_ExpiredManyDaysAgo_ReturnsExpired()
    {
        // Arrange
        var expiryDate = Today.AddDays(-30);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.Expired);
    }

    [Fact]
    public void CalculateStatus_ExpiresToday_ReturnsNearExpiry()
    {
        // Arrange
        var expiryDate = Today;

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_OneDayLeft_ReturnsNearExpiry()
    {
        // Arrange
        var expiryDate = Today.AddDays(1);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_TwoDaysLeft_ReturnsNearExpiry()
    {
        // Arrange
        var expiryDate = Today.AddDays(2);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_ExactlyThreeDaysLeft_ReturnsNearExpiry()
    {
        // Arrange
        var expiryDate = Today.AddDays(3);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_FourDaysLeft_ReturnsFresh()
    {
        // Arrange
        var expiryDate = Today.AddDays(4);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.Fresh);
    }

    [Fact]
    public void CalculateStatus_SevenDaysLeft_ReturnsFresh()
    {
        // Arrange
        var expiryDate = Today.AddDays(7);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate);

        // Assert
        result.Should().Be(FoodStatus.Fresh);
    }

    [Fact]
    public void CalculateStatus_ZeroQuantity_ReturnsConsumed_EvenWhenExpiryInFuture()
    {
        // Arrange
        var expiryDate = Today.AddDays(10);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate, 0);

        // Assert
        result.Should().Be(FoodStatus.Consumed);
    }

    [Fact]
    public void CalculateStatus_NegativeQuantity_ReturnsConsumed()
    {
        // Arrange
        var expiryDate = Today.AddDays(10);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate, -1m);

        // Assert
        result.Should().Be(FoodStatus.Consumed);
    }

    [Fact]
    public void CalculateStatus_ZeroQuantityAndExpired_ReturnsConsumed()
    {
        // Arrange
        var expiryDate = Today.AddDays(-5);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate, 0);

        // Assert
        result.Should().Be(FoodStatus.Consumed);
    }

    [Fact]
    public void CalculateStatus_PositiveQuantity_ExpiredOneDayAgo_ReturnsExpired()
    {
        // Arrange
        var expiryDate = Today.AddDays(-1);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate, 1);

        // Assert
        result.Should().Be(FoodStatus.Expired);
    }

    [Fact]
    public void CalculateStatus_PositiveQuantity_ExactlyThreeDaysLeft_ReturnsNearExpiry()
    {
        // Arrange
        var expiryDate = Today.AddDays(3);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate, 2);

        // Assert
        result.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_PositiveQuantity_FourDaysLeft_ReturnsFresh()
    {
        // Arrange
        var expiryDate = Today.AddDays(4);

        // Act
        var result = FoodStatusHelper.CalculateStatus(expiryDate, 2);

        // Assert
        result.Should().Be(FoodStatus.Fresh);
    }

    [Fact]
    public void GetDaysExpired_ExpiredOneDay_ReturnsOne()
    {
        // Arrange
        var expiryDate = Today.AddDays(-1);

        // Act
        var result = FoodStatusHelper.GetDaysExpired(expiryDate);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void GetDaysExpired_ExpiredSevenDays_ReturnsSeven()
    {
        // Arrange
        var expiryDate = Today.AddDays(-7);

        // Act
        var result = FoodStatusHelper.GetDaysExpired(expiryDate);

        // Assert
        result.Should().Be(7);
    }

    [Fact]
    public void GetDaysExpired_ExpiresToday_ReturnsZero()
    {
        // Arrange
        var expiryDate = Today;

        // Act
        var result = FoodStatusHelper.GetDaysExpired(expiryDate);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void GetDaysExpired_NotExpired_ReturnsNegativeDaysToExpiry()
    {
        // Arrange
        var expiryDate = Today.AddDays(5);

        // Act
        var result = FoodStatusHelper.GetDaysExpired(expiryDate);

        // Assert
        result.Should().Be(-5);
    }

    [Fact]
    public void ShouldBeArchived_AlreadyArchived_ReturnsFalse()
    {
        // Arrange
        var expiryDate = Today.AddDays(-30);

        // Act
        var result = FoodStatusHelper.ShouldBeArchived(expiryDate, FoodStatus.Archived, 7);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldBeArchived_AlreadyConsumed_ReturnsFalse()
    {
        // Arrange
        var expiryDate = Today.AddDays(-30);

        // Act
        var result = FoodStatusHelper.ShouldBeArchived(expiryDate, FoodStatus.Consumed, 7);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldBeArchived_ExpiredExactlyThresholdDays_ReturnsTrue()
    {
        // Arrange
        var expiryDate = Today.AddDays(-7);

        // Act
        var result = FoodStatusHelper.ShouldBeArchived(expiryDate, FoodStatus.Expired, 7);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldBeArchived_ExpiredOneDayBeforeThreshold_ReturnsFalse()
    {
        // Arrange
        var expiryDate = Today.AddDays(-6);

        // Act
        var result = FoodStatusHelper.ShouldBeArchived(expiryDate, FoodStatus.Expired, 7);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldBeArchived_ExpiredBeyondThreshold_ReturnsTrue()
    {
        // Arrange
        var expiryDate = Today.AddDays(-30);

        // Act
        var result = FoodStatusHelper.ShouldBeArchived(expiryDate, FoodStatus.Expired, 7);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldBeArchived_NotExpired_ReturnsFalse()
    {
        // Arrange
        var expiryDate = Today.AddDays(3);

        // Act
        var result = FoodStatusHelper.ShouldBeArchived(expiryDate, FoodStatus.NearExpiry, 7);

        // Assert
        result.Should().BeFalse();
    }
}
