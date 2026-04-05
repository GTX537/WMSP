using WMSP.Api.Models.Dtos;

namespace WMSP.Api.Services;

public interface IReportService
{
    Task<List<DiffSummaryItemDto>> GetDiffSummaryAsync(long planId);
    Task<AdjustResultDto> AdjustInventoryAsync(long planId);
}
