namespace WMSP.Api.Models.Dtos;

public class LoginRequest
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResponse
{
    public string Token { get; set; } = null!;
    public int UserId { get; set; }
    public string RealName { get; set; } = null!;
    public List<string> Permissions { get; set; } = new();
    public List<WarehouseDto> Warehouses { get; set; } = new();
}
