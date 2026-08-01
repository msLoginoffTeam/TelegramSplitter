using BudgetSplitter.App.Authentication;
using BudgetSplitter.Tests.Shared;

namespace BudgetSplitter.App.UnitTests.Authentication;

public sealed class TelegramInitDataValidatorTests
{
    private readonly TelegramInitDataValidator _validator = new();

    [Fact]
    public void TryValidate_WithValidSignedData_ReturnsTelegramUserId()
    {
        const long telegramUserId = 123_456_789;
        var initData = TelegramInitDataBuilder.Create(telegramUserId);

        var isValid = _validator.TryValidate(
            initData,
            TelegramInitDataBuilder.CreateOptions(),
            out var actualTelegramUserId,
            out var failureReason);

        Assert.True(isValid, failureReason);
        Assert.Equal(telegramUserId, actualTelegramUserId);
    }

    [Fact]
    public void TryValidate_WhenPayloadIsChanged_RejectsData()
    {
        var initData = TelegramInitDataBuilder.Create(queryId: "original");
        var tamperedInitData = initData.Replace("query_id=original", "query_id=changed", StringComparison.Ordinal);

        var isValid = _validator.TryValidate(
            tamperedInitData,
            TelegramInitDataBuilder.CreateOptions(),
            out _,
            out _);

        Assert.False(isValid);
    }

    [Fact]
    public void TryValidate_WhenDataIsExpired_RejectsData()
    {
        var initData = TelegramInitDataBuilder.Create(authenticatedAt: DateTimeOffset.UtcNow.AddHours(-2));

        var isValid = _validator.TryValidate(
            initData,
            TelegramInitDataBuilder.CreateOptions(maxAuthAgeSeconds: 60),
            out _,
            out var failureReason);

        Assert.False(isValid);
        Assert.Equal("Telegram init data has expired.", failureReason);
    }

    [Fact]
    public void TryValidate_WhenBotTokenIsMissing_RejectsData()
    {
        var options = TelegramInitDataBuilder.CreateOptions();
        options.BotToken = null;

        var isValid = _validator.TryValidate(
            TelegramInitDataBuilder.Create(),
            options,
            out _,
            out var failureReason);

        Assert.False(isValid);
        Assert.Equal("Telegram authentication is not configured.", failureReason);
    }
}
