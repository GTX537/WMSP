using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using WMSP.Api.Data;
using WMSP.Api.Models.Dtos;
using WMSP.Api.Models.Entities;

namespace WMSP.Api.Services;

public class CheckPlanService : ICheckPlanService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;
    private readonly IConnectionMultiplexer _redis;

    public CheckPlanService(AppDbContext db, ICurrentUser user, IConnectionMultiplexer redis)
    {
        _db = db;
        _user = user;
        _redis = redis;
    }

    public async Task<PageResult<PlanListItemDto>> GetPlansAsync(PlanQueryDto query)
    {
        var q = _db.ChkPlans
            .Include(p => p.Warehouse)
            .Include(p => p.Creator)
            .Where(p => _user.WarehouseIds.Contains(p.WarehouseId))
            .AsQueryable();

        if (!string.IsNullOrEmpty(query.Status))
            q = q.Where(p => p.Status == query.Status);
        if (query.WarehouseId.HasValue)
            q = q.Where(p => p.WarehouseId == query.WarehouseId.Value);
        if (!string.IsNullOrEmpty(query.StartDate) && DateOnly.TryParse(query.StartDate, out var sd))
            q = q.Where(p => p.PlanDate >= sd);
        if (!string.IsNullOrEmpty(query.EndDate) && DateOnly.TryParse(query.EndDate, out var ed))
            q = q.Where(p => p.PlanDate <= ed);
        if (!string.IsNullOrEmpty(query.Keyword))
            q = q.Where(p => p.PlanName.Contains(query.Keyword));

        var total = await q.CountAsync();

        var list = await q.OrderByDescending(p => p.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new PlanListItemDto
            {
                PlanId = p.PlanId,
                PlanNo = p.PlanNo,
                PlanName = p.PlanName,
                WarehouseId = p.WarehouseId,
                WarehouseName = p.Warehouse!.WarehouseName,
                PlanType = p.PlanType,
                CheckMode = p.CheckMode,
                PlanDate = p.PlanDate.ToString("yyyy-MM-dd"),
                Status = p.Status,
                Progress = 0, // 后续计算
                CreatedByName = p.Creator!.RealName,
                CreatedAt = p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            })
            .ToListAsync();

        // 批量计算进度
        var planIds = list.Select(p => p.PlanId).ToList();
        var progressMap = await _db.ChkTasks
            .Where(t => planIds.Contains(t.PlanId))
            .GroupBy(t => t.PlanId)
            .Select(g => new
            {
                PlanId = g.Key,
                Total = g.Count(),
                Reviewed = g.Count(t => t.Status == "REVIEWED"),
            })
            .ToDictionaryAsync(x => x.PlanId);

        foreach (var item in list)
        {
            if (progressMap.TryGetValue(item.PlanId, out var pg) && pg.Total > 0)
                item.Progress = (int)(pg.Reviewed * 100.0 / pg.Total);
        }

        return new PageResult<PlanListItemDto>
        {
            List = list,
            Total = total,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    public async Task<PlanDetailDto> GetPlanAsync(long planId)
    {
        var p = await _db.ChkPlans
            .Include(x => x.Warehouse)
            .Include(x => x.Creator)
            .FirstOrDefaultAsync(x => x.PlanId == planId)
            ?? throw new KeyNotFoundException("计划不存在");

        return MapToDetail(p);
    }

    public async Task<PlanDetailDto> CreatePlanAsync(CreatePlanDto dto)
    {
        // 仓库权限校验
        if (!_user.WarehouseIds.Contains(dto.WarehouseId))
            throw new UnauthorizedAccessException("该仓库不在您的可见范围内");

        // 生成计划单号 (Redis原子操作)
        var db = _redis.GetDatabase();
        var dateStr = DateTime.Now.ToString("yyMMdd");
        var serial = await db.StringIncrementAsync($"biz:plan_no:{dateStr}");
        var planNo = $"PL{dateStr}{serial:D3}";

        var plan = new ChkPlan
        {
            PlanNo = planNo,
            PlanName = dto.PlanName,
            WarehouseId = dto.WarehouseId,
            PlanType = dto.PlanType,
            CheckMode = dto.CheckMode,
            TargetZones = dto.Zones != null ? string.Join(",", dto.Zones) : null,
            SampleRate = dto.SampleRate,
            PlanDate = DateOnly.Parse(dto.PlanDate),
            Status = "DRAFT",
            CreatedBy = _user.UserId,
            Remark = dto.Remark,
        };

        _db.ChkPlans.Add(plan);
        await _db.SaveChangesAsync();

        // 重新加载导航属性
        await _db.Entry(plan).Reference(p => p.Warehouse).LoadAsync();
        await _db.Entry(plan).Reference(p => p.Creator).LoadAsync();

        return MapToDetail(plan);
    }

    public async Task UpdatePlanAsync(long planId, CreatePlanDto dto)
    {
        var plan = await _db.ChkPlans.FindAsync(planId)
            ?? throw new KeyNotFoundException("计划不存在");

        if (plan.Status != "DRAFT")
            throw new InvalidOperationException("仅草稿状态可编辑");

        plan.PlanName = dto.PlanName;
        plan.WarehouseId = dto.WarehouseId;
        plan.PlanType = dto.PlanType;
        plan.CheckMode = dto.CheckMode;
        plan.TargetZones = dto.Zones != null ? string.Join(",", dto.Zones) : null;
        plan.SampleRate = dto.SampleRate;
        plan.PlanDate = DateOnly.Parse(dto.PlanDate);
        plan.Remark = dto.Remark;
        plan.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync();
    }

    public async Task<PublishResultDto> PublishPlanAsync(long planId)
    {
        var plan = await _db.ChkPlans.FindAsync(planId)
            ?? throw new KeyNotFoundException("计划不存在");

        if (plan.Status != "DRAFT")
            throw new InvalidOperationException("仅草稿状态可发布");

        // 确定货位范围
        var locQuery = _db.Locations
            .Where(l => l.WarehouseId == plan.WarehouseId && l.IsActive);

        if (plan.PlanType == "ZONE" && !string.IsNullOrEmpty(plan.TargetZones))
        {
            var zones = plan.TargetZones.Split(',');
            locQuery = locQuery.Where(l => zones.Contains(l.Zone));
        }

        var locations = await locQuery.ToListAsync();

        if (locations.Count == 0)
            throw new InvalidOperationException("该计划下无可盘点的货位，请检查仓库和区域配置");

        // SPOT: 抽盘
        if (plan.PlanType == "SPOT" && plan.SampleRate.HasValue)
        {
            var count = (int)Math.Ceiling(locations.Count * plan.SampleRate.Value / 100.0);
            locations = locations.OrderBy(_ => Guid.NewGuid()).Take(count).ToList();
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var dateStr = DateTime.Now.ToString("yyMMdd");
        var redisDb = _redis.GetDatabase();
        int taskCount = 0;

        foreach (var loc in locations)
        {
            var serial = await redisDb.StringIncrementAsync($"biz:task_no:{dateStr}");
            var taskNo = $"TK{dateStr}{serial:D4}";

            var task = new ChkTask
            {
                TaskNo = taskNo,
                PlanId = planId,
                LocationId = loc.LocationId,
                Status = "PENDING",
            };
            _db.ChkTasks.Add(task);
            await _db.SaveChangesAsync(); // 获取 TaskId

            // 快照该货位的库存明细
            var stocks = await _db.Stocks
                .Where(s => s.LocationId == loc.LocationId && s.BookQty > 0)
                .ToListAsync();

            foreach (var stock in stocks)
            {
                _db.ChkDetails.Add(new ChkDetail
                {
                    TaskId = task.TaskId,
                    MaterialId = stock.MaterialId,
                    LocationId = stock.LocationId,
                    BatchNo = stock.BatchNo,
                    BookQty = stock.BookQty,
                    OperatorId = _user.UserId,
                });
            }
            taskCount++;
        }

        plan.Status = "PUBLISHED";
        plan.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        // 清除Redis进度缓存
        await redisDb.KeyDeleteAsync($"check:progress:{planId}");

        return new PublishResultDto { TaskCount = taskCount };
    }

    public async Task<PlanProgressDto> GetProgressAsync(long planId)
    {
        var redisDb = _redis.GetDatabase();
        var cacheKey = $"check:progress:{planId}";

        // 尝试从Redis读取缓存
        var cached = await redisDb.StringGetAsync(cacheKey);
        if (cached.HasValue)
            return System.Text.Json.JsonSerializer.Deserialize<PlanProgressDto>((string)cached!)!;

        var tasks = await _db.ChkTasks
            .Where(t => t.PlanId == planId)
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var total = tasks.Sum(t => t.Count);
        var reviewed = tasks.FirstOrDefault(t => t.Status == "REVIEWED")?.Count ?? 0;

        var diffCount = await _db.ChkDetails
            .Where(d => d.Task!.PlanId == planId && d.ActualQty != null && d.DiffQty != 0)
            .CountAsync();

        var result = new PlanProgressDto
        {
            Total = total,
            Pending = tasks.FirstOrDefault(t => t.Status == "PENDING")?.Count ?? 0,
            Claimed = tasks.FirstOrDefault(t => t.Status == "CLAIMED")?.Count ?? 0,
            Counting = tasks.FirstOrDefault(t => t.Status == "COUNTING")?.Count ?? 0,
            Submitted = tasks.FirstOrDefault(t => t.Status == "SUBMITTED")?.Count ?? 0,
            Reviewed = reviewed,
            ProgressPct = total > 0 ? (int)(reviewed * 100.0 / total) : 0,
            DiffCount = diffCount,
        };

        // 缓存30秒
        await redisDb.StringSetAsync(cacheKey,
            System.Text.Json.JsonSerializer.Serialize(result),
            TimeSpan.FromSeconds(30));

        return result;
    }

    public async Task CompletePlanAsync(long planId)
    {
        var plan = await _db.ChkPlans.FindAsync(planId)
            ?? throw new KeyNotFoundException("计划不存在");

        var unreviewed = await _db.ChkTasks
            .CountAsync(t => t.PlanId == planId && t.Status != "REVIEWED");

        if (unreviewed > 0)
            throw new InvalidOperationException($"还有{unreviewed}个任务未复核，无法完成计划");

        plan.Status = "COMPLETED";
        plan.CompletedAt = DateTime.Now;
        plan.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task CancelPlanAsync(long planId)
    {
        var plan = await _db.ChkPlans.FindAsync(planId)
            ?? throw new KeyNotFoundException("计划不存在");

        if (plan.Status != "DRAFT" && plan.Status != "PUBLISHED")
            throw new InvalidOperationException("只有草稿或已发布状态的计划可以取消");

        await using var transaction = await _db.Database.BeginTransactionAsync();

        // 释放已领取的任务
        var tasks = await _db.ChkTasks.Where(t => t.PlanId == planId).ToListAsync();
        foreach (var task in tasks)
        {
            task.Status = "PENDING";
            task.AssignedTo = null;
            task.ClaimedAt = null;
        }

        plan.Status = "CANCELLED";
        plan.CompletedAt = DateTime.Now;
        plan.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static PlanDetailDto MapToDetail(ChkPlan p) => new()
    {
        PlanId = p.PlanId,
        PlanNo = p.PlanNo,
        PlanName = p.PlanName,
        WarehouseId = p.WarehouseId,
        WarehouseName = p.Warehouse?.WarehouseName ?? "",
        PlanType = p.PlanType,
        CheckMode = p.CheckMode,
        PlanDate = p.PlanDate.ToString("yyyy-MM-dd"),
        Status = p.Status,
        Progress = 0,
        CreatedByName = p.Creator?.RealName ?? "",
        CreatedAt = p.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
        TargetZones = p.TargetZones,
        SampleRate = p.SampleRate,
        Remark = p.Remark,
        CompletedAt = p.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
    };
}
