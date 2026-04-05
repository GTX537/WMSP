using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSP.Api.Models.Entities;

[Table("inv_stock")]
public class InvStock
{
    [Key]
    [Column("stock_id")]
    public long StockId { get; set; }

    [Column("material_id")]
    public long MaterialId { get; set; }

    [Column("location_id")]
    public long LocationId { get; set; }

    [Column("warehouse_id")]
    public int WarehouseId { get; set; }

    [Column("batch_no")]
    [MaxLength(50)]
    public string? BatchNo { get; set; }

    [Column("book_qty")]
    public decimal BookQty { get; set; }

    [Column("last_check_date")]
    public DateTime? LastCheckDate { get; set; }

    [ForeignKey("MaterialId")]
    public virtual MatMaterial? Material { get; set; }

    [ForeignKey("LocationId")]
    public virtual WhLocation? Location { get; set; }
}
