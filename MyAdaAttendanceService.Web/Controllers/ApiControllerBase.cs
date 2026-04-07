using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MyAdaAttendanceService.Web.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                 ?? User.FindFirst("sub");

        if (claim is null || !int.TryParse(claim.Value, out var userId))
            throw new UnauthorizedAccessException("User identity not found.");

        return userId;
    }
}
