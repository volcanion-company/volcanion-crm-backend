using Asp.Versioning;
using CrmSaas.Api.Common;
using CrmSaas.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmSaas.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public abstract class BaseController : ControllerBase
{
    protected ActionResult<ApiResponse<T>> OkResponse<T>(T data, string? message = null)
    {
        return Ok(ApiResponse<T>.Ok(data, message));
    }

    protected ActionResult<ApiResponse> OkResponse(string? message = null)
    {
        return Ok(ApiResponse.Ok(message));
    }

    protected ActionResult<ApiResponse<T>> CreatedResponse<T>(T data, string? message = null)
    {
        return StatusCode(201, ApiResponse<T>.Ok(data, message));
    }

    protected ActionResult<ApiResponse<T>> BadRequestResponse<T>(string error)
    {
        return BadRequest(ApiResponse<T>.Fail(error));
    }

    protected ActionResult<ApiResponse> BadRequestResponse(string error)
    {
        return BadRequest(ApiResponse.Fail(error));
    }

    protected ActionResult<ApiResponse<T>> NotFoundResponse<T>(string error)
    {
        return NotFound(ApiResponse<T>.Fail(error));
    }

    protected ActionResult<ApiResponse> NotFoundResponse(string error)
    {
        return NotFound(ApiResponse.Fail(error));
    }

    protected string? GetIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
