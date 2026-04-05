using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WMSP.Api.Filters;
using WMSP.Api.Models.Dtos;
using WMSP.Api.Services;

namespace WMSP.Api.Controllers;

[ApiController]
[Route("api/v1/check-plans")]
[Authorize]
public class CheckPlanController : ControllerBase
{
    private readonly ICheckPlanService _planService;

    public CheckPlanController(ICheckPlanService planService)
    {
        _planService = planService;
    }

    /// <summary>盘点计划列表(分页+组合筛选)</summary>
    [HttpGet]
    [RequirePermission("check:plan")]
    public async Task<ActionResult<PageResult<PlanListItemDto>>> GetPlans([FromQuery] PlanQueryDto query)
    {
        return Ok(await _planService.GetPlansAsync(query));
    }

    /// <summary>盘点计划详情</summary>
    [HttpGet("{planId:long}")]
    [RequirePermission("check:plan")]
    public async Task<ActionResult<PlanDetailDto>> GetPlan(long planId)
    {
        return Ok(await _planService.GetPlanAsync(planId));
    }

    /// <summary>创建盘点计划</summary>
    [HttpPost]
    [RequirePermission("check:plan:create")]
    public async Task<ActionResult<PlanDetailDto>> CreatePlan([FromBody] CreatePlanDto dto)
    {
        var result = await _planService.CreatePlanAsync(dto);
        return CreatedAtAction(nameof(GetPlan), new { planId = result.PlanId }, result);
    }

    /// <summary>编辑盘点计划</summary>
    [HttpPut("{planId:long}")]
    [RequirePermission("check:plan:create")]
    public async Task<IActionResult> UpdatePlan(long planId, [FromBody] CreatePlanDto dto)
    {
        await _planService.UpdatePlanAsync(planId, dto);
        return NoContent();
    }

    /// <summary>发布计划→自动拆分子任务</summary>
    [HttpPost("{planId:long}/publish")]
    [RequirePermission("check:publish")]
    public async Task<ActionResult<PublishResultDto>> PublishPlan(long planId)
    {
        return Ok(await _planService.PublishPlanAsync(planId));
    }

    /// <summary>实时进度查询</summary>
    [HttpGet("{planId:long}/progress")]
    [RequirePermission("check:plan")]
    public async Task<ActionResult<PlanProgressDto>> GetProgress(long planId)
    {
        return Ok(await _planService.GetProgressAsync(planId));
    }

    /// <summary>完成盘点计划</summary>
    [HttpPost("{planId:long}/complete")]
    [RequirePermission("check:plan:create")]
    public async Task<IActionResult> CompletePlan(long planId)
    {
        await _planService.CompletePlanAsync(planId);
        return NoContent();
    }

    /// <summary>取消盘点计划</summary>
    [HttpPost("{planId:long}/cancel")]
    [RequirePermission("check:plan:create")]
    public async Task<IActionResult> CancelPlan(long planId)
    {
        await _planService.CancelPlanAsync(planId);
        return NoContent();
    }
}
