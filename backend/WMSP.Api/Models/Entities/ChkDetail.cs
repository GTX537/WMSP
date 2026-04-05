using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSP.Api.Models.Entities;

[Table("chk_detail")]
public class ChkDetail
{
    [Key]
    [Column("detail_id")]
    public long DetailId { get; set; }

    [Column("task_id")]
    public long TaskId { get; set; }

    [Column("material_id")]
    public long MaterialId { get; set; }

    [Column("location_id")]
    public long LocationId { get; set; }

    [Column("batch_no")]
    [MaxLength(50)]
    public string? BatchNo { get; set; }

    [Column("book_qty")]
    public decimal BookQty { get; set; }

    [Column("actual_qty")]
    public decimal? ActualQty { get; set; }

    [Column("diff_qty")]
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal DiffQty { get; set; }

    [Column("diff_reason")]
    [MaxLength(200)]
    public string? DiffReason { get; set; }

    [Column("scan_time")]
    public DateTime? ScanTime { get; set; }

    [Column("is_rechecked")]
    public bool IsRechecked { get; set; }

    [Column("recheck_qty")]
    public decimal? RecheckQty { get; set; }

    [Column("operator_id")]
    public int OperatorId { get; set; }

    [Column("remark")]
    [MaxLength(500)]
    public string? Remark { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation
    [ForeignKey("TaskId")]
    public virtual ChkTask? Task { get; set; }

    [ForeignKey("MaterialId")]
    public virtual MatMaterial? Material { get; set; }

    [ForeignKey("LocationId")]
    public virtual WhLocation? Location { get; set; }

    [ForeignKey("OperatorId")]
    public virtual SysUser? Operator { get; set; }
}
