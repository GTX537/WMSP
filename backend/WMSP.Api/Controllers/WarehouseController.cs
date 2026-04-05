using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMSP.Api.Data;
using WMSP.Api.Models.Dtos;
using WMSP.Api.Services;

namespace WMSP.Api.Controllers;

[ApiController]
[Route("api/v1/warehouses")]
[Authorize]
public class WarehouseController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _user;

    public WarehouseController(AppDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    /// <summary>用户可见仓库列表</summary>
    [HttpGet("mine")]
    public async Task<ActionResult<List<WarehouseDto>>> GetMyWarehouses()
    {
        var list = await _db.Warehouses
            .Where(w => _user.WarehouseIds.Contains(w.WarehouseId) && w.IsActive)
            .Select(w => new WarehouseDto { WarehouseId = w.WarehouseId, WarehouseName = w.WarehouseName })
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>仓库下区域列表</summary>
    [HttpGet("{warehouseId:int}/zones")]
    public async Task<ActionResult<List<string>>> GetZones(int warehouseId)
    {
        var zones = await _db.Locations
            .Where(l => l.WarehouseId == warehouseId && l.IsActive && l.Zone != null)
            .Select(l => l.Zone!)
            .Distinct()
            .OrderBy(z => z)
            .ToListAsync();
        return Ok(zones);
    }
}
