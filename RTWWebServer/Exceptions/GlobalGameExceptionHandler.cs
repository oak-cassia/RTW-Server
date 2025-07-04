using Microsoft.AspNetCore.Diagnostics;
using NetworkDefinition.ErrorCode;
using RTWWebServer.DTOs;

namespace RTWWebServer.Exceptions;

public class GlobalGameExceptionHandler(ILogger<GlobalGameExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = "application/json";

        GameResponse<object> response;

        if (exception is GameException gameException)
        {
            logger.LogInformation(
                "game exception: {ErrorCode}, message: {Message}",
                gameException.ErrorCode, gameException.Message);

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            response = GameResponse<object>.Fail(gameException.ErrorCode);
        }
        else
        {
            // 기타 예외는 서버 오류로 간주
            logger.LogError(exception, "unhandled exception: {Message}", exception.Message);
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            response = GameResponse<object>.Fail(WebServerErrorCode.InternalServerError);
        }

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);
        return true;
    }
}