using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace BudgetSplitter.App.Authentication;

public sealed class TelegramAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IHostEnvironment _environment;
    private readonly TelegramInitDataValidator _validator;
    private readonly IOptions<TelegramAuthOptions> _authOptions;

    public TelegramAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IHostEnvironment environment,
        TelegramInitDataValidator validator,
        IOptions<TelegramAuthOptions> authOptions)
        : base(options, logger, encoder)
    {
        _environment = environment;
        _validator = validator;
        _authOptions = authOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (_environment.IsDevelopment() &&
            Request.Headers.TryGetValue(TelegramAuthDefaults.DevelopmentUserIdHeaderName, out var developmentUserIdHeader))
        {
            if (long.TryParse(developmentUserIdHeader, out var developmentUserId) && developmentUserId > 0)
            {
                return Task.FromResult(Success(developmentUserId));
            }

            return Task.FromResult(AuthenticateResult.Fail("Invalid development Telegram user ID."));
        }

        if (!Request.Headers.TryGetValue(TelegramAuthDefaults.InitDataHeaderName, out var initDataHeader) ||
            string.IsNullOrWhiteSpace(initDataHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        return Task.FromResult(
            _validator.TryValidate(initDataHeader!, _authOptions.Value, out var telegramUserId, out var failureReason)
                ? Success(telegramUserId)
                : AuthenticateResult.Fail(failureReason));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = TelegramAuthDefaults.Scheme;
        return Task.CompletedTask;
    }

    private AuthenticateResult Success(long telegramUserId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, telegramUserId.ToString()),
            new Claim(TelegramAuthDefaults.TelegramIdClaimType, telegramUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, TelegramAuthDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TelegramAuthDefaults.Scheme);
        return AuthenticateResult.Success(ticket);
    }
}
