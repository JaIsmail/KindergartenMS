using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.Api.Controllers;

[ApiController]
public class DashboardController : ControllerBase
{
    [HttpGet("/admin")]
    public ContentResult Dashboard()
    {
        return new ContentResult
        {
            ContentType = "text/html",
            StatusCode  = 200,
            Content     = System.IO.File.ReadAllText(
                Path.Combine(AppContext.BaseDirectory, "dashboard.html"))
        };
    }
}
