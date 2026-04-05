using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WMSP.Api.Filters;
using WMSP.Api.Hubs;
using WMSP.Api.Models.Dtos;
using WMSP.Api.Services;

namespace WMSP.Api.Controllers;

[ApiController]
[Route("api/v1/check-tasks")]
[Authorize]
public class CheckTaskController : ControllerBase
{
    private readonly ICheckTaskService _taskService;
    private readonly IHubContext<CheckProgressHub> _hubContext;

    public CheckTaskController(ICheckTaskService taskService, IHubContext<CheckProgressHub> hubContext)
    {
        _taskService = taskService;
        _hubContext = hubContext;
    }

    /// <summary>子任务列表</summary>
    [HttpGet]
    [RequirePermission("check:task", "check:scan")]
    public async Task<ActionResult<PageResult<TaskListItemDto>>> GetTasks([FromQuery] TaskQueryDto query)
    {
        return Ok(await _taskService.GetTasksAsync(query));
    }

    /// <summary>领取任务</summary>
    [HttpPost("{taskId:long}/claim")]
    [RequirePermission("check:scan")]
    public async Task<IActionResult> ClaimTask(long taskId)
    {
        try
        {
            await _taskService.ClaimTaskAsync(taskId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>PDA扫货位码→物料清单</summary>
    [HttpGet("scan-location")]
    [RequirePermission("check:scan")]
    public async Task<ActionResult<List<DetailDto>>> ScanLocation([FromQuery] long taskId, [FromQuery] string barcode)
    {
        return Ok(await _taskService.ScanLocationAsync(taskId, barcode));
    }

    /// <summary>PDA扫物料码+录入数量</summary>
    [HttpPost("{taskId:long}/scan-material")]
    [RequirePermission("check:scan")]
    public async Task<ActionResult<ScanMaterialResultDto>> ScanMaterial(long taskId, [FromBody] ScanMaterialDto dto)
    {
        var result = await _taskService.ScanMaterialAsync(taskId, dto);

        // SignalR推送扫码事件
        await _hubContext.Clients.Group($"plan-{taskId}").SendAsync("ScanEvent", new
        {
            taskId,
            barcode = dto.Barcode,
            actualQty = dto.ActualQty,
            hasDiff = result.HasDiff,
        });

        return Ok(result);
    }

    /// <summary>提交盘点结果</summary>
    [HttpPost("{taskId:long}/submit")]
    [RequirePermission("check:submit")]
    public async Task<IActionResult> SubmitTask(long taskId)
    {
        await _taskService.SubmitTaskAsync(taskId);

        // SignalR推送提交事件
        await _hubContext.Clients.Group($"plan-{taskId}").SendAsync("TaskSubmitted", new { taskId });

        return NoContent();
    }

    /// <summary>复核页盘点明细</summary>
    [HttpGet("{taskId:long}/details")]
    [RequirePermission("check:review")]
    public async Task<ActionResult<List<DetailDto>>> GetDetails(long taskId)
    {
        return Ok(await _taskService.GetDetailsAsync(taskId));
    }

    /// <summary>复核(通过/退回)</summary>
    [HttpPost("{taskId:long}/review")]
    [RequirePermission("check:review")]
    public async Task<IActionResult> ReviewTask(long taskId, [FromBody] ReviewDto dto)
    {
        await _taskService.ReviewTaskAsync(taskId, dto);
        return NoContent();
    }
}
