using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMSP.Api.Filters;
using WMSP.Api.Models.Dtos;
using WMSP.Api.Services;

namespace WMSP.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>差异汇总报表</summary>
    [HttpGet("reports/check-diff")]
    [RequirePermission("report:diff")]
    public async Task<ActionResult<List<DiffSummaryItemDto>>> GetDiffSummary([FromQuery] long planId)
    {
        return Ok(await _reportService.GetDiffSummaryAsync(planId));
    }

    /// <summary>一键调账</summary>
    [HttpPost("inventory/adjust")]
    [RequirePermission("inventory:adjust")]
    public async Task<ActionResult<AdjustResultDto>> AdjustInventory([FromBody] AdjustRequest request)
    {
        return Ok(await _reportService.AdjustInventoryAsync(request.PlanId));
    }
}

public class AdjustRequest
{
    public long PlanId { get; set; }
}
