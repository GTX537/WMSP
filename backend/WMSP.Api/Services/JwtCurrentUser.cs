using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using WMSP.Api.Data;

namespace WMSP.Api.Services;

public class JwtCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AppDbContext _db;
    private List<int>? _warehouseIds;
    private List<string>? _permissions;

    public JwtCurrentUser(IHttpContextAccessor httpContextAccessor, AppDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int UserId => int.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public string RealName => User?.FindFirstValue("real_name") ?? "";

    public IReadOnlyList<int> WarehouseIds
    {
        get
        {
            _warehouseIds ??= _db.UserWarehouses
                .Where(uw => uw.UserId == UserId)
                .Select(uw => uw.WarehouseId)
                .ToList();
            return _warehouseIds;
        }
    }

    public bool HasPermission(string code)
    {
        _permissions ??= _db.UserPermissions
            .Where(p => p.UserId == UserId)
            .Select(p => p.PermissionCode)
            .ToList();
        return _permissions.Contains("*") || _permissions.Contains(code);
    }
}
