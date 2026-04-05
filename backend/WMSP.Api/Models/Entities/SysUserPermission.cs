using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSP.Api.Models.Entities;

[Table("sys_user_permission")]
public class SysUserPermission
{
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("permission_code")]
    [MaxLength(50)]
    public string PermissionCode { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual SysUser? User { get; set; }
}
