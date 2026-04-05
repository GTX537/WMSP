using WMSP.Api.Models.Dtos;

namespace WMSP.Api.Services;

public interface ICheckTaskService
{
    Task<PageResult<TaskListItemDto>> GetTasksAsync(TaskQueryDto query);
    Task ClaimTaskAsync(long taskId);
    Task<List<DetailDto>> ScanLocationAsync(long taskId, string barcode);
    Task<ScanMaterialResultDto> ScanMaterialAsync(long taskId, ScanMaterialDto dto);
    Task SubmitTaskAsync(long taskId);
    Task<List<DetailDto>> GetDetailsAsync(long taskId);
    Task ReviewTaskAsync(long taskId, ReviewDto dto);
}
