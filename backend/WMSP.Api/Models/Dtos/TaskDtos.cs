namespace WMSP.Api.Models.Dtos;

public class TaskQueryDto
{
    public long? PlanId { get; set; }
    public string? Status { get; set; }
    public bool? MyWarehouse { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class TaskListItemDto
{
    public long TaskId { get; set; }
    public string TaskNo { get; set; } = null!;
    public long PlanId { get; set; }
    public string PlanName { get; set; } = null!;
    public long LocationId { get; set; }
    public string LocationCode { get; set; } = null!;
    public string Zone { get; set; } = null!;
    public string? AssignedName { get; set; }
    public string Status { get; set; } = null!;
    public int ItemCount { get; set; }
    public int ScannedCount { get; set; }
    public int DiffCount { get; set; }
    public string? ClaimedAt { get; set; }
    public string? SubmittedAt { get; set; }
}

public class DetailDto
{
    public long DetailId { get; set; }
    public long TaskId { get; set; }
    public long MaterialId { get; set; }
    public string MaterialCode { get; set; } = null!;
    public string MaterialName { get; set; } = null!;
    public string Unit { get; set; } = null!;
    public decimal? BookQty { get; set; }
    public decimal? ActualQty { get; set; }
    public decimal DiffQty { get; set; }
    public string? DiffReason { get; set; }
    public string? ScanTime { get; set; }
    public bool IsRechecked { get; set; }
    public string OperatorName { get; set; } = null!;
    public string? Barcode { get; set; }
}

public class ScanMaterialDto
{
    public string Barcode { get; set; } = null!;
    public decimal ActualQty { get; set; }
}

public class ScanMaterialResultDto
{
    public bool HasDiff { get; set; }
    public decimal DiffQty { get; set; }
}

public class ReviewDto
{
    public bool Approved { get; set; }
    public List<ReviewDetailDto>? Details { get; set; }
}

public class ReviewDetailDto
{
    public long DetailId { get; set; }
    public string DiffReason { get; set; } = null!;
}
