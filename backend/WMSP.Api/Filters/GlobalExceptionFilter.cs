using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WMSP.Api.Filters;

public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var (statusCode, message) = context.Exception switch
        {
            KeyNotFoundException ex => (404, ex.Message),
            UnauthorizedAccessException ex => (403, ex.Message),
            InvalidOperationException ex => (400, ex.Message),
            FluentValidation.ValidationException ex => (400, string.Join("; ", ex.Errors.Select(e => e.ErrorMessage))),
            _ => (500, "服务器内部错误"),
        };

        if (statusCode == 500)
            _logger.LogError(context.Exception, "Unhandled exception");

        context.Result = new ObjectResult(new { message }) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}
