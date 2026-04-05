using Microsoft.EntityFrameworkCore;
using WMSP.Api.Models.Entities;

namespace WMSP.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ChkPlan> ChkPlans => Set<ChkPlan>();
    public DbSet<ChkTask> ChkTasks => Set<ChkTask>();
    public DbSet<ChkDetail> ChkDetails => Set<ChkDetail>();
    public DbSet<WhWarehouse> Warehouses => Set<WhWarehouse>();
    public DbSet<WhLocation> Locations => Set<WhLocation>();
    public DbSet<MatMaterial> Materials => Set<MatMaterial>();
    public DbSet<SysUser> Users => Set<SysUser>();
    public DbSet<SysUserWarehouse> UserWarehouses => Set<SysUserWarehouse>();
    public DbSet<SysUserPermission> UserPermissions => Set<SysUserPermission>();
    public DbSet<InvStock> Stocks => Set<InvStock>();
    public DbSet<InvTransaction> Transactions => Set<InvTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ChkDetail: diff_qty 计算列
        modelBuilder.Entity<ChkDetail>()
            .Property(d => d.DiffQty)
            .HasComputedColumnSql("ISNULL(actual_qty, 0) - book_qty", stored: true);

        // 复合主键
        modelBuilder.Entity<SysUserWarehouse>()
            .HasKey(e => new { e.UserId, e.WarehouseId });

        modelBuilder.Entity<SysUserPermission>()
            .HasKey(e => new { e.UserId, e.PermissionCode });

        // 索引
        modelBuilder.Entity<ChkPlan>()
            .HasIndex(e => e.PlanNo).IsUnique();

        modelBuilder.Entity<ChkTask>()
            .HasIndex(e => e.TaskNo).IsUnique();

        modelBuilder.Entity<ChkTask>()
            .HasIndex(e => e.PlanId);

        modelBuilder.Entity<ChkDetail>()
            .HasIndex(e => e.TaskId);

        modelBuilder.Entity<ChkDetail>()
            .HasIndex(e => e.MaterialId);

        modelBuilder.Entity<InvStock>()
            .HasIndex(e => new { e.MaterialId, e.LocationId, e.BatchNo })
            .IsUnique();
    }
}
