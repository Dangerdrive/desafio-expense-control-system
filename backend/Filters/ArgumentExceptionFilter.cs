using Backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Backend.Filters;

/// <summary>
/// Converte ArgumentException de regras de negócio em resposta 400.
/// </summary>
public class ArgumentExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ArgumentException exception)
            return;

        context.Result = new BadRequestObjectResult(new ErrorResponse
        {
            Message = exception.Message
        });
        context.ExceptionHandled = true;
    }
}
