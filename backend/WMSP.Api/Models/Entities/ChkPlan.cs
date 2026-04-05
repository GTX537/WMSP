using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSP.Api.Models.Entities;

[Table("chk_plan")]
public class ChkPlan
{
    [Key]
    [Column("plan_id")]
    public long PlanId { get; set; }

    [Column("plan_no")]
    [MaxLength(30)]
    public string PlanNo { get; set; } = null!;

    [Column("plan_name")]
    [MaxLength(200)]
    public string PlanName { get; set; } = null!;

    [Column("warehouse_id")]
    public int WarehouseId { get; set; }

    [Column("plan_type")]
    [MaxLength(20)]
    public string PlanType { get; set; } = null!;

    [Column("check_mode")]
    [MaxLength(20)]
    public string CheckMode { get; set; } = null!;

    [Column("target_zones")]
    [MaxLength(500)]
    public string? TargetZones { get; set; }

    [Column("sample_rate")]
    public int? SampleRate { get; set; }

    [Column("plan_date")]
    public DateOnly PlanDate { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "DRAFT";

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("approved_by")]
    public int? ApprovedBy { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("remark")]
    [MaxLength(500)]
    public string? Remark { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey("WarehouseId")]
    public virtual WhWarehouse? Warehouse { get; set; }

    [ForeignKey("CreatedBy")]
    public virtual SysUser? Creator { get; set; }

    public virtual ICollection<ChkTask> Tasks { get; set; } = new List<ChkTask>();
}
