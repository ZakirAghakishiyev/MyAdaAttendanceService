using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MyAdaAttendanceService.Web.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected async Task<IActionResult> HandleAsync<T>(
        Func<Task<T?>> action,
        Func<T, IActionResult>? onSuccess = null,
        string? notFoundMessage = null) where T : class
    {
        try
        {
            var result = await action();
            if (result is null)
                return NotFound(new { message = notFoundMessage ?? "Resource not found." });

            return onSuccess is null ? Ok(result) : onSuccess(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    protected async Task<IActionResult> HandleAsync(Func<Task> action)
    {
        try
        {
            await action();
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    protected int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                 ?? User.FindFirst("sub");

        if (claim is null || !int.TryParse(claim.Value, out var userId))
            throw new UnauthorizedAccessException("User identity not found.");

        return userId;
    }
}
