namespace WMSP.Api.Models.Dtos;

// ===== 查询参数 =====

public class PlanQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Status { get; set; }
    public int? WarehouseId { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Keyword { get; set; }
}

// ===== 创建/编辑 =====

public class CreatePlanDto
{
    public string PlanName { get; set; } = null!;
    public int WarehouseId { get; set; }
    public string PlanType { get; set; } = null!;
    public string CheckMode { get; set; } = null!;
    public string PlanDate { get; set; } = null!;
    public List<string>? Zones { get; set; }
    public int? SampleRate { get; set; }
    public string? Remark { get; set; }
}

// ===== 响应 =====

public class PlanListItemDto
{
    public long PlanId { get; set; }
    public string PlanNo { get; set; } = null!;
    public string PlanName { get; set; } = null!;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public string PlanType { get; set; } = null!;
    public string CheckMode { get; set; } = null!;
    public string PlanDate { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int Progress { get; set; }
    public string CreatedByName { get; set; } = null!;
    public string CreatedAt { get; set; } = null!;
}

public class PlanDetailDto : PlanListItemDto
{
    public string? TargetZones { get; set; }
    public int? SampleRate { get; set; }
    public string? Remark { get; set; }
    public string? CompletedAt { get; set; }
}

public class PlanProgressDto
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int Claimed { get; set; }
    public int Counting { get; set; }
    public int Submitted { get; set; }
    public int Reviewed { get; set; }
    public int ProgressPct { get; set; }
    public int DiffCount { get; set; }
}

public class PublishResultDto
{
    public int TaskCount { get; set; }
}
