using doanki3API.Services;
using Microsoft.AspNetCore.Mvc;

namespace doanki3API.Controllers.Technical;

[Route("api/Technical")]
public sealed class TechnicalDashboardController : TechnicalApiControllerBase
{
    private readonly TechnicalDashboardService _dashboardService;

    public TechnicalDashboardController(TechnicalDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardService.GetAsync(cancellationToken);
        return Ok(dashboard);
    }
}
