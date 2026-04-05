using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using WMSP.Api.Data;
using WMSP.Api.Models.Dtos;
using WMSP.Api.Models.Entities;

namespace WMSP.Api.Services;

public class CheckTaskService : ICheckTaskService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IConnectionMultiplexer _redis;

    public CheckTaskService(AppDbContext db, ICurrentUser user, IConnectionMultiplexer redis)
    {
        _db = db;
        _user = user;
        _redis = redis;
    }

    public async Task<PageResult<TaskListItemDto>> GetTasksAsync(TaskQueryDto query)
    {
        var q = _db.ChkTasks
            .Include(t => t.Plan)
            .Include(t => t.Location)
            .Include(t => t.Assignee)
            .AsQueryable();

        // 仓库隔离
        if (query.MyWarehouse == true)
            q = q.Where(t => _user.WarehouseIds.Contains(t.Location!.WarehouseId));

        if (query.PlanId.HasValue)
            q = q.Where(t => t.PlanId == query.PlanId.Value);
        if (!string.IsNullOrEmpty(query.Status))
            q = q.Where(t => t.Status == query.Status);

        var total = await q.CountAsync();

        var list = await q.OrderBy(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TaskListItemDto
            {
                TaskId = t.TaskId,
                TaskNo = t.TaskNo,
                PlanId = t.PlanId,
                PlanName = t.Plan!.PlanName,
                LocationId = t.LocationId,
                LocationCode = t.Location!.LocationCode,
                Zone = t.Location.Zone ?? "",
                AssignedName = t.Assignee != null ? t.Assignee.RealName : null,
                Status = t.Status,
                ItemCount = t.Details.Count,
                ScannedCount = t.Details.Count(d => d.ActualQty != null),
                DiffCount = t.Details.Count(d => d.ActualQty != null && d.DiffQty != 0),
                ClaimedAt = t.ClaimedAt != null ? t.ClaimedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                SubmittedAt = t.SubmittedAt != null ? t.SubmittedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
            })
            .ToListAsync();

        return new PageResult<TaskListItemDto> { List = list, Total = total, Page = query.Page, PageSize = query.PageSize };
    }

    public async Task ClaimTaskAsync(long taskId)
    {
        // 乐观锁: 仅 PENDING 状态可领取
        var affected = await _db.ChkTasks
            .Where(t => t.TaskId == taskId && t.Status == "PENDING")
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, "CLAIMED")
                .SetProperty(t => t.AssignedTo, _user.UserId)
                .SetProperty(t => t.ClaimedAt, DateTime.Now)
                .SetProperty(t => t.UpdatedAt, DateTime.Now));

        if (affected == 0)
            throw new InvalidOperationException("该任务已被其他人领取");
    }

    public async Task<List<DetailDto>> ScanLocationAsync(long taskId, string barcode)
    {
        // 查找货位
        var location = await _db.Locations
            .FirstOrDefaultAsync(l => l.LocationBarcode == barcode || l.LocationCode == barcode)
            ?? throw new KeyNotFoundException($"未识别的货位条码: {barcode}");

        // 校验是否属于当前任务
        var task = await _db.ChkTasks.FindAsync(taskId)
            ?? throw new KeyNotFoundException("任务不存在");

        if (task.LocationId != location.LocationId)
            throw new InvalidOperationException("该货位不属于当前盘点任务");

        // CLAIMED → COUNTING
        if (task.Status == "CLAIMED")
        {
            task.Status = "COUNTING";
            task.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
        }

        // 获取盘点模式
        var plan = await _db.ChkPlans.FindAsync(task.PlanId);
        var isBlind = plan?.CheckMode == "BLIND";

        var details = await _db.ChkDetails
            .Include(d => d.Material)
            .Include(d => d.Operator)
            .Where(d => d.TaskId == taskId)
            .Select(d => new DetailDto
            {
                DetailId = d.DetailId,
                TaskId = d.TaskId,
                MaterialId = d.MaterialId,
                MaterialCode = d.Material!.MaterialCode,
                MaterialName = d.Material.MaterialName,
                Unit = d.Material.Unit,
                BookQty = isBlind ? null : d.BookQty,
                ActualQty = d.ActualQty,
                DiffQty = d.DiffQty,
                DiffReason = d.DiffReason,
                ScanTime = d.ScanTime != null ? d.ScanTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                IsRechecked = d.IsRechecked,
                OperatorName = d.Operator!.RealName,
                Barcode = d.Material.Barcode,
            })
            .ToListAsync();

        return details;
    }

    public async Task<ScanMaterialResultDto> ScanMaterialAsync(long taskId, ScanMaterialDto dto)
    {
        // 解析条码 → 物料
        var material = await _db.Materials
            .FirstOrDefaultAsync(m => m.Barcode == dto.Barcode || m.MaterialCode == dto.Barcode)
            ?? throw new KeyNotFoundException("该物料不在本次盘点清单中");

        // 匹配明细
        var detail = await _db.ChkDetails
            .FirstOrDefaultAsync(d => d.TaskId == taskId && d.MaterialId == material.MaterialId)
            ?? throw new KeyNotFoundException("该物料不在本次盘点清单中");

        // 更新实盘数量
        detail.ActualQty = dto.ActualQty;
        detail.ScanTime = DateTime.Now;
        detail.OperatorId = _user.UserId;
        detail.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        // 重新读取计算列
        await _db.Entry(detail).ReloadAsync();

        var hasDiff = detail.DiffQty != 0;

        // 清除Redis进度缓存
        var task = await _db.ChkTasks.FindAsync(taskId);
        if (task != null)
        {
            var redisDb = _redis.GetDatabase();
            await redisDb.KeyDeleteAsync($"check:progress:{task.PlanId}");
        }

        return new ScanMaterialResultDto
        {
            HasDiff = hasDiff,
            DiffQty = detail.DiffQty,
        };
    }

    public async Task SubmitTaskAsync(long taskId)
    {
        var task = await _db.ChkTasks.FindAsync(taskId)
            ?? throw new KeyNotFoundException("任务不存在");

        if (task.Status != "COUNTING")
            throw new InvalidOperationException("仅盘点中的任务可提交");

        task.Status = "SUBMITTED";
        task.SubmittedAt = DateTime.Now;
        task.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();

        // 更新计划状态为 IN_PROGRESS (如果还不是)
        var plan = await _db.ChkPlans.FindAsync(task.PlanId);
        if (plan != null && plan.Status == "PUBLISHED")
        {
            plan.Status = "IN_PROGRESS";
            plan.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<List<DetailDto>> GetDetailsAsync(long taskId)
    {
        return await _db.ChkDetails
            .Include(d => d.Material)
            .Include(d => d.Operator)
            .Where(d => d.TaskId == taskId)
            .Select(d => new DetailDto
            {
                DetailId = d.DetailId,
                TaskId = d.TaskId,
                MaterialId = d.MaterialId,
                MaterialCode = d.Material!.MaterialCode,
                MaterialName = d.Material.MaterialName,
                Unit = d.Material.Unit,
                BookQty = d.BookQty,
                ActualQty = d.ActualQty,
                DiffQty = d.DiffQty,
                DiffReason = d.DiffReason,
                ScanTime = d.ScanTime != null ? d.ScanTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                IsRechecked = d.IsRechecked,
                OperatorName = d.Operator!.RealName,
                Barcode = d.Material.Barcode,
            })
            .ToListAsync();
    }

    public async Task ReviewTaskAsync(long taskId, ReviewDto dto)
    {
        var task = await _db.ChkTasks.FindAsync(taskId)
            ?? throw new KeyNotFoundException("任务不存在");

        if (task.Status != "SUBMITTED")
            throw new InvalidOperationException("仅已提交的任务可复核");

        await using var transaction = await _db.Database.BeginTransactionAsync();

        if (dto.Approved)
        {
            // 校验: 所有差异项必须有原因
            var missingReason = await _db.ChkDetails
                .AnyAsync(d => d.TaskId == taskId && d.DiffQty != 0 && d.DiffReason == null);

            // 先保存前端传来的差异原因
            if (dto.Details != null)
            {
                foreach (var item in dto.Details)
                {
                    await _db.ChkDetails
                        .Where(d => d.DetailId == item.DetailId)
                        .ExecuteUpdateAsync(s => s.SetProperty(d => d.DiffReason, item.DiffReason));
                }
            }

            // 再次检查
            missingReason = await _db.ChkDetails
                .AnyAsync(d => d.TaskId == taskId && d.DiffQty != 0 && d.DiffReason == null);
            if (missingReason)
                throw new InvalidOperationException("差异项必须填写差异原因");

            task.Status = "REVIEWED";
            task.ReviewedBy = _user.UserId;
            task.ReviewedAt = DateTime.Now;
        }
        else
        {
            // 退回: 清空实盘数据
            await _db.ChkDetails
                .Where(d => d.TaskId == taskId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.ActualQty, (decimal?)null)
                    .SetProperty(d => d.ScanTime, (DateTime?)null)
                    .SetProperty(d => d.IsRechecked, false)
                    .SetProperty(d => d.DiffReason, (string?)null));

            task.Status = "COUNTING";
            task.ReviewedBy = null;
            task.ReviewedAt = null;
        }

        task.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        // 清除进度缓存
        var redisDb = _redis.GetDatabase();
        await redisDb.KeyDeleteAsync($"check:progress:{task.PlanId}");
    }
}
