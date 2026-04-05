using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSP.Api.Models.Entities;

[Table("wh_warehouse")]
public class WhWarehouse
{
    [Key]
    [Column("warehouse_id")]
    public int WarehouseId { get; set; }

    [Column("warehouse_name")]
    [MaxLength(100)]
    public string WarehouseName { get; set; } = null!;

    [Column("warehouse_code")]
    [MaxLength(30)]
    public string WarehouseCode { get; set; } = null!;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
