using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSP.Api.Models.Entities;

[Table("wh_location")]
public class WhLocation
{
    [Key]
    [Column("location_id")]
    public long LocationId { get; set; }

    [Column("warehouse_id")]
    public int WarehouseId { get; set; }

    [Column("location_code")]
    [MaxLength(30)]
    public string LocationCode { get; set; } = null!;

    [Column("location_barcode")]
    [MaxLength(50)]
    public string? LocationBarcode { get; set; }

    [Column("zone")]
    [MaxLength(50)]
    public string? Zone { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [ForeignKey("WarehouseId")]
    public virtual WhWarehouse? Warehouse { get; set; }
}
