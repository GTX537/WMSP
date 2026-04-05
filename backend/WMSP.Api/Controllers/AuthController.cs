using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WMSP.Api.Data;
using WMSP.Api.Models.Dtos;

namespace WMSP.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>用户登录</summary>
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { message = "用户名和密码不能为空" });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);
        if (user == null)
            return Unauthorized(new { message = "用户名或密码错误" });

        // 校验密码 (SHA256 hash)
        var hash = HashPassword(request.Password);
        if (user.PasswordHash != null && user.PasswordHash != hash)
            return Unauthorized(new { message = "用户名或密码错误" });

        // 查询权限
        var permissions = await _db.UserPermissions
            .Where(p => p.UserId == user.UserId)
            .Select(p => p.PermissionCode)
            .ToListAsync();

        // 查询可见仓库
        var warehouses = await _db.UserWarehouses
            .Where(uw => uw.UserId == user.UserId)
            .Join(_db.Warehouses.Where(w => w.IsActive), uw => uw.WarehouseId, w => w.WarehouseId,
                (uw, w) => new WarehouseDto { WarehouseId = w.WarehouseId, WarehouseName = w.WarehouseName })
            .ToListAsync();

        // 生成JWT
        var token = GenerateJwt(user.UserId, user.Username, user.RealName);

        return Ok(new LoginResponse
        {
            Token = token,
            UserId = user.UserId,
            RealName = user.RealName,
            Permissions = permissions,
            Warehouses = warehouses,
        });
    }

    /// <summary>获取当前用户信息 (用于刷新页面后恢复状态)</summary>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<ActionResult<LoginResponse>> GetMe()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user == null || !user.IsActive)
            return Unauthorized();

        var permissions = await _db.UserPermissions
            .Where(p => p.UserId == userId)
            .Select(p => p.PermissionCode)
            .ToListAsync();

        var warehouses = await _db.UserWarehouses
            .Where(uw => uw.UserId == userId)
            .Join(_db.Warehouses.Where(w => w.IsActive), uw => uw.WarehouseId, w => w.WarehouseId,
                (uw, w) => new WarehouseDto { WarehouseId = w.WarehouseId, WarehouseName = w.WarehouseName })
            .ToListAsync();

        return Ok(new LoginResponse
        {
            Token = "",
            UserId = user.UserId,
            RealName = user.RealName,
            Permissions = permissions,
            Warehouses = warehouses,
        });
    }

    private string GenerateJwt(int userId, string username, string realName)
    {
        var secret = _config["Jwt:Secret"] ?? "WMSP_Default_Secret_Key_2026_Must_Be_32Chars!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim("real_name", realName),
        };

        var expireMinutes = int.TryParse(_config["Jwt:ExpireMinutes"], out var m) ? m : 480;

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "WMSP",
            audience: _config["Jwt:Audience"] ?? "WMSP",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
