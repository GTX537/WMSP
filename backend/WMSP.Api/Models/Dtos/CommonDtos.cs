namespace WMSP.Api.Models.Dtos;

public class PageResult<T>
{
    public List<T> List { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class DiffSummaryItemDto
{
    public string MaterialCode { get; set; } = null!;
    public string MaterialName { get; set; } = null!;
    public string LocationCode { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public decimal BookQty { get; set; }
    public decimal ActualQty { get; set; }
    public decimal DiffQty { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? DiffAmount { get; set; }
    public string? DiffReason { get; set; }
}

public class AdjustResultDto
{
    public int AdjustedCount { get; set; }
}

public class WarehouseDto
{
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
}

// ===== 看板 =====

public class DashboardDto
{
    public DashboardStatsDto Stats { get; set; } = new();
    public List<PlanStatusCountDto> PlansByStatus { get; set; } = new();
    public List<TaskStatusCountDto> TasksByStatus { get; set; } = new();
    public List<RecentPlanDto> RecentPlans { get; set; } = new();
    public List<DailyCheckCountDto> DailyChecks { get; set; } = new();
}

public class DashboardStatsDto
{
    public int TotalPlans { get; set; }
    public int ActivePlans { get; set; }
    public int PendingTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int TotalDiffItems { get; set; }
    public int TotalMaterials { get; set; }
    public int TotalLocations { get; set; }
    public int TotalWarehouses { get; set; }
}

public class PlanStatusCountDto
{
    public string Status { get; set; } = null!;
    public int Count { get; set; }
}

public class TaskStatusCountDto
{
    public string Status { get; set; } = null!;
    public int Count { get; set; }
}

public class RecentPlanDto
{
    public long PlanId { get; set; }
    public string PlanNo { get; set; } = null!;
    public string PlanName { get; set; } = null!;
    public string WarehouseName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string PlanType { get; set; } = null!;
    public string PlanDate { get; set; } = null!;
    public string CreatorName { get; set; } = null!;
    public int TaskCount { get; set; }
    public int ReviewedCount { get; set; }
}

public class DailyCheckCountDto
{
    public string Date { get; set; } = null!;
    public int SubmittedCount { get; set; }
    public int ReviewedCount { get; set; }
}
