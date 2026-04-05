using Microsoft.EntityFrameworkCore;
using WMSP.Api.Data;
using WMSP.Api.Models.Dtos;

namespace WMSP.Api.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync();
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardDto> GetDashboardAsync()
    {
        var dto = new DashboardDto();

        // 统计卡片
        var activePlanStatuses = new[] { "PUBLISHED", "IN_PROGRESS" };
        dto.Stats = new DashboardStatsDto
        {
            TotalPlans = await _db.ChkPlans.CountAsync(),
            ActivePlans = await _db.ChkPlans.CountAsync(p => activePlanStatuses.Contains(p.Status)),
            PendingTasks = await _db.ChkTasks.CountAsync(t => t.Status == "PENDING" || t.Status == "CLAIMED" || t.Status == "COUNTING"),
            CompletedTasks = await _db.ChkTasks.CountAsync(t => t.Status == "REVIEWED"),
            TotalDiffItems = await _db.ChkDetails.CountAsync(d => d.ActualQty != null && d.DiffQty != 0),
            TotalMaterials = await _db.Materials.CountAsync(),
            TotalLocations = await _db.Locations.CountAsync(),
            TotalWarehouses = await _db.Warehouses.CountAsync(),
        };

        // 计划按状态分组
        dto.PlansByStatus = await _db.ChkPlans
            .GroupBy(p => p.Status)
            .Select(g => new PlanStatusCountDto { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // 任务按状态分组
        dto.TasksByStatus = await _db.ChkTasks
            .GroupBy(t => t.Status)
            .Select(g => new TaskStatusCountDto { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // 最近10个计划
        dto.RecentPlans = await _db.ChkPlans
            .Include(p => p.Warehouse)
            .Include(p => p.Creator)
            .Include(p => p.Tasks)
            .OrderByDescending(p => p.CreatedAt)
            .Take(10)
            .Select(p => new RecentPlanDto
            {
                PlanId = p.PlanId,
                PlanNo = p.PlanNo,
                PlanName = p.PlanName,
                WarehouseName = p.Warehouse != null ? p.Warehouse.WarehouseName : "",
                Status = p.Status,
                PlanType = p.PlanType,
                PlanDate = p.PlanDate.ToString("yyyy-MM-dd"),
                CreatorName = p.Creator != null ? p.Creator.RealName : "",
                TaskCount = p.Tasks.Count,
                ReviewedCount = p.Tasks.Count(t => t.Status == "REVIEWED"),
            })
            .ToListAsync();

        // 近14天每日提交/复核数
        var since = DateTime.Now.Date.AddDays(-13);
        var submittedByDay = await _db.ChkTasks
            .Where(t => t.SubmittedAt != null && t.SubmittedAt >= since)
            .GroupBy(t => t.SubmittedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        var reviewedByDay = await _db.ChkTasks
            .Where(t => t.ReviewedAt != null && t.ReviewedAt >= since)
            .GroupBy(t => t.ReviewedAt!.Value.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync();

        for (var d = since; d <= DateTime.Now.Date; d = d.AddDays(1))
        {
            dto.DailyChecks.Add(new DailyCheckCountDto
            {
                Date = d.ToString("MM-dd"),
                SubmittedCount = submittedByDay.FirstOrDefault(x => x.Date == d)?.Count ?? 0,
                ReviewedCount = reviewedByDay.FirstOrDefault(x => x.Date == d)?.Count ?? 0,
            });
        }

        return dto;
    }
}
