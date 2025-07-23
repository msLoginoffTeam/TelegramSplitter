using System.Reflection;
using BudgetSplitter.App.Middlewares;
using BudgetSplitter.App.Services.BalanceService;
using BudgetSplitter.App.Services.ExpenseService;
using BudgetSplitter.App.Services.GroupService;
using BudgetSplitter.App.Services.PaymentService;
using BudgetSplitter.App.Services.UserService;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? throw new SystemException("Connection string not found.");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
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

    // c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    // {
    //     In          = ParameterLocation.Header,
    //     Name        = "X-ApiKey",
    //     Type        = SecuritySchemeType.ApiKey,
    //     Description = "API ключ для доступа к методам."
    // });
    // c.AddSecurityRequirement(new OpenApiSecurityRequirement
    // {
    //     [ new OpenApiSecurityScheme { Reference = 
    //         new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" } }
    //     ] = Array.Empty<string>()
    // });

});

builder.Services.AddScoped<IGroupService, GroupService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBalanceService, BalanceService>();

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(conn, npgsql =>
    {
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    })
);

Console.WriteLine("==== Connection string ====");
Console.WriteLine(builder.Configuration.GetConnectionString("DefaultConnection"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();