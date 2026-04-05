using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WMSP.Api.Services;

namespace WMSP.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequirePermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _permissions;

    public RequirePermissionAttribute(params string[] permissions)
    {
        _permissions = permissions;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

        if (currentUser.UserId == 0)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // 任一权限匹配即通过
        var hasAny = _permissions.Any(p => currentUser.HasPermission(p));
        if (!hasAny)
        {
            context.Result = new ForbidResult();
        }

        await Task.CompletedTask;
    }
}
