using Microsoft.EntityFrameworkCore;
using WMSP.Api.Data;
using WMSP.Api.Models.Dtos;
using WMSP.Api.Models.Entities;

namespace WMSP.Api.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public ReportService(AppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public async Task<List<DiffSummaryItemDto>> GetDiffSummaryAsync(long planId)
    {
        return await _db.ChkDetails
            .Include(d => d.Material)
            .Include(d => d.Location)
            .Where(d => d.Task!.PlanId == planId && d.ActualQty != null && d.DiffQty != 0)
            .Select(d => new DiffSummaryItemDto
            {
                MaterialCode = d.Material!.MaterialCode,
                MaterialName = d.Material.MaterialName,
                LocationCode = d.Location!.LocationCode,
                Unit = d.Material.Unit,
                BookQty = d.BookQty,
                ActualQty = d.ActualQty!.Value,
                DiffQty = d.DiffQty,
                UnitCost = d.Material.UnitCost,
                DiffAmount = d.Material.UnitCost.HasValue ? d.DiffQty * d.Material.UnitCost.Value : null,
                DiffReason = d.DiffReason,
            })
            .ToListAsync();
    }

    public async Task<AdjustResultDto> AdjustInventoryAsync(long planId)
    {
        // 校验: 所有子任务必须已复核
        var unreviewed = await _db.ChkTasks
            .CountAsync(t => t.PlanId == planId && t.Status != "REVIEWED");
        if (unreviewed > 0)
            throw new InvalidOperationException("只有所有子任务已复核的计划才可调账");

        var plan = await _db.ChkPlans.FindAsync(planId)
            ?? throw new KeyNotFoundException("计划不存在");

        // 获取所有差异明细
        var diffDetails = await _db.ChkDetails
            .Include(d => d.Material)
            .Where(d => d.Task!.PlanId == planId && d.ActualQty != null && d.DiffQty != 0)
            .ToListAsync();

        await using var transaction = await _db.Database.BeginTransactionAsync();

        int adjustedCount = 0;

        foreach (var detail in diffDetails)
        {
            // 行锁更新库存
            var stock = await _db.Stocks
                .FirstOrDefaultAsync(s =>
                    s.MaterialId == detail.MaterialId &&
                    s.LocationId == detail.LocationId &&
                    s.BatchNo == detail.BatchNo);

            if (stock == null) continue;

            var beforeQty = stock.BookQty;
            stock.BookQty += detail.DiffQty;
            stock.LastCheckDate = DateTime.Now;

            // 生成调账流水
            _db.Transactions.Add(new InvTransaction
            {
                MaterialId = detail.MaterialId,
                LocationId = detail.LocationId,
                TransType = "ADJ",
                Qty = detail.DiffQty,
                BeforeQty = beforeQty,
                AfterQty = stock.BookQty,
                RefDocNo = plan.PlanNo,
                OperatorId = _user.UserId,
            });

            adjustedCount++;
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new AdjustResultDto { AdjustedCount = adjustedCount };
    }
}
