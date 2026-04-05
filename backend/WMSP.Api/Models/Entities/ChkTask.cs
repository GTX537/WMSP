using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSP.Api.Models.Entities;

[Table("chk_task")]
public class ChkTask
{
    [Key]
    [Column("task_id")]
    public long TaskId { get; set; }

    [Column("task_no")]
    [MaxLength(30)]
    public string TaskNo { get; set; } = null!;

    [Column("plan_id")]
    public long PlanId { get; set; }

    [Column("location_id")]
    public long LocationId { get; set; }

    [Column("assigned_to")]
    public int? AssignedTo { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDING";

    [Column("claimed_at")]
    public DateTime? ClaimedAt { get; set; }

    [Column("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [Column("reviewed_by")]
    public int? ReviewedBy { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("reject_reason")]
    [MaxLength(500)]
    public string? RejectReason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey("PlanId")]
    public virtual ChkPlan? Plan { get; set; }

    [ForeignKey("LocationId")]
    public virtual WhLocation? Location { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual SysUser? Assignee { get; set; }

    public virtual ICollection<ChkDetail> Details { get; set; } = new List<ChkDetail>();
}
