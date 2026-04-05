using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSP.Api.Models.Entities;

[Table("sys_user_warehouse")]
public class SysUserWarehouse
{
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("warehouse_id")]
    public int WarehouseId { get; set; }

    [ForeignKey("UserId")]
    public virtual SysUser? User { get; set; }

    [ForeignKey("WarehouseId")]
    public virtual WhWarehouse? Warehouse { get; set; }
}
