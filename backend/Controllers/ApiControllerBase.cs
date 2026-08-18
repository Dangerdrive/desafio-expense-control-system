using Backend.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller base com respostas de erro padronizadas.
/// </summary>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Cria uma resposta 400 com mensagem.
    /// </summary>
    protected BadRequestObjectResult BadRequestWithMessage(string message) =>
        BadRequest(new ErrorResponse { Message = message });

    /// <summary>
    /// Cria uma resposta 404 com mensagem.
    /// </summary>
    protected NotFoundObjectResult NotFoundWithMessage(string message) =>
        NotFound(new ErrorResponse { Message = message });
}
