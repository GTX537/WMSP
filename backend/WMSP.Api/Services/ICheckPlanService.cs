using WMSP.Api.Models.Dtos;

namespace WMSP.Api.Services;

public interface ICheckPlanService
{
    Task<PageResult<PlanListItemDto>> GetPlansAsync(PlanQueryDto query);
    Task<PlanDetailDto> GetPlanAsync(long planId);
    Task<PlanDetailDto> CreatePlanAsync(CreatePlanDto dto);
    Task UpdatePlanAsync(long planId, CreatePlanDto dto);
    Task<PublishResultDto> PublishPlanAsync(long planId);
    Task<PlanProgressDto> GetProgressAsync(long planId);
    Task CompletePlanAsync(long planId);
    Task CancelPlanAsync(long planId);
}
