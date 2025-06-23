using System.Text.Json;
using BudgetSplitter.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSplitter.App.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        int code;
        string title;

        if (exception is CustomException apiEx)
        {
            code = apiEx.StatusCode;
            title = exception.Message;
        }
        else
        {
            code = StatusCodes.Status500InternalServerError;
            title = "Internal server error";
        }

        var problem = new ProblemDetails
        {
            Type   = $"https://httpstatuses.com/{code}",
            Title  = title,
            Status = code,
            Instance = context.Request.Path
        };

        var json = JsonSerializer.Serialize(problem);
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = code;
        return context.Response.WriteAsync(json);
    }
}