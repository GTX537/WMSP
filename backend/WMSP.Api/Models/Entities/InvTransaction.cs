using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSP.Api.Models.Entities;

[Table("inv_transaction")]
public class InvTransaction
{
    [Key]
    [Column("transaction_id")]
    public long TransactionId { get; set; }

    [Column("material_id")]
    public long MaterialId { get; set; }

    [Column("location_id")]
    public long LocationId { get; set; }

    [Column("trans_type")]
    [MaxLength(20)]
    public string TransType { get; set; } = null!;

    [Column("qty")]
    public decimal Qty { get; set; }

    [Column("before_qty")]
    public decimal BeforeQty { get; set; }

    [Column("after_qty")]
    public decimal AfterQty { get; set; }

    [Column("ref_doc_no")]
    [MaxLength(30)]
    public string? RefDocNo { get; set; }

    [Column("operator_id")]
    public int OperatorId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
