using Xunit;
using FluentAssertions;
using FridgeWatch.Domain.Common;
using FridgeWatch.Domain.Enums;

namespace FridgeWatch.Tests;

/// <summary>
/// 保质期状态判断链路测试：覆盖新鲜、临期、过期三种状态，
/// 以及“剩余三天”这一临界点和刚好过期一天、刚好剩三天等边界条件。
/// 计算逻辑基于 DateTime.UtcNow.Date，故测试日期均相对 UTC 今天构造。
/// </summary>
public class FoodStatusHelperTests
{
    private static DateTime Today => DateTime.UtcNow.Date;

    // ---------- 新鲜（Fresh）：剩余天数 >= 4 ----------

    [Fact]
    public void CalculateStatus_WhenFourDaysLeft_ReturnsFresh()
    {
        // 剩 4 天是临界点外的第一天，应判为新鲜
        var status = FoodStatusHelper.CalculateStatus(Today.AddDays(4));

        status.Should().Be(FoodStatus.Fresh);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(365)]
    public void CalculateStatus_WhenWellBeforeExpiry_ReturnsFresh(int daysLeft)
    {
        var status = FoodStatusHelper.CalculateStatus(Today.AddDays(daysLeft));

        status.Should().Be(FoodStatus.Fresh);
    }

    // ---------- 临期（NearExpiry）：0 <= 剩余天数 <= 3 ----------

    [Fact]
    public void CalculateStatus_WhenExactlyThreeDaysLeft_ReturnsNearExpiry()
    {
        // 关键临界点：刚好剩三天应判为临期
        var status = FoodStatusHelper.CalculateStatus(Today.AddDays(3));

        status.Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_WhenExpiresToday_ReturnsNearExpiry()
    {
        // 剩余 0 天（今天到期但尚未过期）仍属临期
        var status = FoodStatusHelper.CalculateStatus(Today);

        status.Should().Be(FoodStatus.NearExpiry);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CalculateStatus_WhenWithinThreeDays_ReturnsNearExpiry(int daysLeft)
    {
        var status = FoodStatusHelper.CalculateStatus(Today.AddDays(daysLeft));

        status.Should().Be(FoodStatus.NearExpiry);
    }

    // ---------- 过期（Expired）：剩余天数 < 0 ----------

    [Fact]
    public void CalculateStatus_WhenExpiredByOneDay_ReturnsExpired()
    {
        // 关键边界：刚好过期一天应判为过期
        var status = FoodStatusHelper.CalculateStatus(Today.AddDays(-1));

        status.Should().Be(FoodStatus.Expired);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    [InlineData(-30)]
    public void CalculateStatus_WhenPastExpiry_ReturnsExpired(int daysOffset)
    {
        var status = FoodStatusHelper.CalculateStatus(Today.AddDays(daysOffset));

        status.Should().Be(FoodStatus.Expired);
    }

    [Fact]
    public void CalculateStatus_ThreeDayBoundary_TransitionsFromNearExpiryToFresh()
    {
        // 临界点两侧对比：剩 3 天临期、剩 4 天新鲜
        FoodStatusHelper.CalculateStatus(Today.AddDays(3)).Should().Be(FoodStatus.NearExpiry);
        FoodStatusHelper.CalculateStatus(Today.AddDays(4)).Should().Be(FoodStatus.Fresh);
    }

    [Fact]
    public void CalculateStatus_ZeroDayBoundary_TransitionsFromExpiredToNearExpiry()
    {
        // 过期临界点两侧对比：过期一天为过期、今天到期为临期
        FoodStatusHelper.CalculateStatus(Today.AddDays(-1)).Should().Be(FoodStatus.Expired);
        FoodStatusHelper.CalculateStatus(Today).Should().Be(FoodStatus.NearExpiry);
    }

    [Fact]
    public void CalculateStatus_IgnoresTimeOfDay_UsesDateOnly()
    {
        // 只按日期计算，忽略当天具体时刻：今天晚些时候到期仍为临期
        var status = FoodStatusHelper.CalculateStatus(Today.AddHours(23).AddMinutes(59));

        status.Should().Be(FoodStatus.NearExpiry);
    }

    // ---------- 带数量的重载：数量耗尽优先判为已消耗（Consumed） ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CalculateStatus_WhenQuantityDepleted_ReturnsConsumed(decimal quantity)
    {
        // 数量 <= 0 时无论保质期如何都判为已消耗
        var status = FoodStatusHelper.CalculateStatus(Today.AddDays(7), quantity);

        status.Should().Be(FoodStatus.Consumed);
    }

    [Fact]
    public void CalculateStatus_WhenQuantityDepletedAndExpired_StillReturnsConsumed()
    {
        // 即使已过期，数量耗尽仍优先判为已消耗
        var status = FoodStatusHelper.CalculateStatus(Today.AddDays(-5), 0);

        status.Should().Be(FoodStatus.Consumed);
    }

    [Theory]
    [InlineData(4, FoodStatus.Fresh)]
    [InlineData(3, FoodStatus.NearExpiry)]
    [InlineData(0, FoodStatus.NearExpiry)]
    [InlineData(-1, FoodStatus.Expired)]
    public void CalculateStatus_WithPositiveQuantity_DelegatesToDateBasedStatus(int daysOffset, FoodStatus expected)
    {
        // 数量 > 0 时应退回到基于保质期的判断
        var status = FoodStatusHelper.CalculateStatus(Today.AddDays(daysOffset), 1);

        status.Should().Be(expected);
    }

    // ---------- GetDaysExpired：已过期天数 ----------

    [Fact]
    public void GetDaysExpired_WhenExpiredByOneDay_ReturnsOne()
    {
        FoodStatusHelper.GetDaysExpired(Today.AddDays(-1)).Should().Be(1);
    }

    [Fact]
    public void GetDaysExpired_WhenExpiresToday_ReturnsZero()
    {
        FoodStatusHelper.GetDaysExpired(Today).Should().Be(0);
    }

    [Fact]
    public void GetDaysExpired_WhenNotYetExpired_ReturnsNegative()
    {
        // 尚未过期时返回负数（还剩的天数取负）
        FoodStatusHelper.GetDaysExpired(Today.AddDays(3)).Should().Be(-3);
    }

    // ---------- ShouldBeArchived：过期到达阈值后应归档 ----------

    [Fact]
    public void ShouldBeArchived_WhenExpiredDaysEqualsThreshold_ReturnsTrue()
    {
        // 刚好达到自动归档阈值即应归档
        var result = FoodStatusHelper.ShouldBeArchived(Today.AddDays(-7), FoodStatus.Expired, 7);

        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldBeArchived_WhenExpiredDaysBelowThreshold_ReturnsFalse()
    {
        var result = FoodStatusHelper.ShouldBeArchived(Today.AddDays(-6), FoodStatus.Expired, 7);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(FoodStatus.Archived)]
    [InlineData(FoodStatus.Consumed)]
    public void ShouldBeArchived_WhenAlreadyTerminalStatus_ReturnsFalse(FoodStatus currentStatus)
    {
        // 已归档或已消耗的食材不再重复归档，即便早已过期
        var result = FoodStatusHelper.ShouldBeArchived(Today.AddDays(-30), currentStatus, 7);

        result.Should().BeFalse();
    }
}
