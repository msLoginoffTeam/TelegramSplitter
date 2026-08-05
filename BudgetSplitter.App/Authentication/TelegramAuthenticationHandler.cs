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
            _validator.TryValidateIdentity(initDataHeader!, _authOptions.Value, out var identity, out var failureReason)
                ? Success(identity)
                : AuthenticateResult.Fail(failureReason));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = TelegramAuthDefaults.Scheme;
        return Task.CompletedTask;
    }

    private AuthenticateResult Success(long telegramUserId)
        => Success(new TelegramUserIdentity(telegramUserId, null, null), includeProfileClaims: false);

    private AuthenticateResult Success(TelegramUserIdentity profile, bool includeProfileClaims = true)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, profile.TelegramId.ToString()),
            new Claim(TelegramAuthDefaults.TelegramIdClaimType, profile.TelegramId.ToString())
        };
        if (includeProfileClaims)
        {
            claims.Add(new Claim(TelegramAuthDefaults.TelegramDisplayNameClaimType, profile.DisplayName ?? string.Empty));
            claims.Add(new Claim(TelegramAuthDefaults.TelegramUsernameClaimType, profile.Username ?? string.Empty));
        }

        var claimsIdentity = new ClaimsIdentity(claims, TelegramAuthDefaults.Scheme);
        var principal = new ClaimsPrincipal(claimsIdentity);
        var ticket = new AuthenticationTicket(principal, TelegramAuthDefaults.Scheme);
        return AuthenticateResult.Success(ticket);
    }
}
