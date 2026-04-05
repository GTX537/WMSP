using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WMSP.Api.Models.Entities;

[Table("sys_user")]
public class SysUser
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("username")]
    [MaxLength(50)]
    public string Username { get; set; } = null!;

    [Column("real_name")]
    [MaxLength(50)]
    public string RealName { get; set; } = null!;

    [Column("password_hash")]
    [MaxLength(200)]
    public string? PasswordHash { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
