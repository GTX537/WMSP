namespace WMSP.Api.Services;

public interface ICurrentUser
{
    int UserId { get; }
    string RealName { get; }
    IReadOnlyList<int> WarehouseIds { get; }
    bool HasPermission(string code);
}
