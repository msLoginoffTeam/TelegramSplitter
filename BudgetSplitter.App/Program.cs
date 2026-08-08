using System.Reflection;
using AuditLogLens;
using AuditLogLens.Configuration;
using BudgetSplitter.App.Authentication;
using BudgetSplitter.App.Audit;
using BudgetSplitter.App.Authorization;
using BudgetSplitter.App.Middlewares;
using BudgetSplitter.App.Services.AuditLogService;
using BudgetSplitter.App.Services.BalanceService;
using BudgetSplitter.App.Services.ExpenseService;
using BudgetSplitter.App.Services.GroupService;
using BudgetSplitter.App.Services.PaymentService;
using BudgetSplitter.App.Services.UserService;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

if (builder.Environment.IsEnvironment("Tests"))
{
    builder.Configuration.AddJsonFile("appsettings.Tests.json", optional: false, reloadOnChange: false);
}

builder.Configuration.AddEnvironmentVariables();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddControllers(options => options.Filters.AddService<GroupPermissionAuthorizationFilter>());
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<TelegramAuthOptions>(
    builder.Configuration.GetSection(TelegramAuthOptions.SectionName));
builder.Services.AddSingleton<TelegramInitDataValidator>();
builder.Services.AddHttpClient<TelegramBotIdentityService>();
builder.Services
    .AddAuthentication(TelegramAuthDefaults.Scheme)
    .AddScheme<AuthenticationSchemeOptions, TelegramAuthenticationHandler>(
        TelegramAuthDefaults.Scheme,
        _ => { });
builder.Services.AddAuthorization();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Budget Splitter API",
        Version     = "v1.0 beta", 
        Description = "A service for recording joint expenses in Telegram",
        Contact = new OpenApiContact
        {
            Name  = "Max",
            Url   = new Uri("https://github.com/msLoginoffTeam/TelegramSplitter")
        }
    });

    c.AddSecurityDefinition("TelegramInitData", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = TelegramAuthDefaults.InitDataHeaderName,
        Type = SecuritySchemeType.ApiKey,
        Description = "Telegram Mini App initData."
    });
    c.OperationFilter<TelegramAuthOperationFilter>();

});

builder.Services
    .AddAuditInfrastructure(options => options.WriteMode = AuditWriteMode.Transactional)
    .AddEfAuditWriter<AuditLogEntry, AuditLogEntryMapper>()
    .AddAuditRestrictions<BudgetSplitterAuditRestrictions>()
    .AddAuditEnricher<AuditMetadataEnricher>()
    .AddAuditEnricher<GroupMembersAuditEnricher>()
    .AddAuditEnricher<ExpenseShareGroupAuditEnricher>()
    .AddAuditEnricher<AuditUserNameEnricher>();

builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBalanceService, BalanceService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IGroupAuthorizationService, GroupAuthorizationService>();
builder.Services.AddScoped<GroupPermissionAuthorizationFilter>();

builder.Services.AddDbContext<AppDbContext>((provider, opts) =>
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new SystemException("Connection string not found.");
    }

    opts.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    });
    opts.AddAuditInterceptor(provider);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}
