using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using PRN232.Plagiarism.Application.Interfaces;

namespace PRN232.Plagiarism.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class GrpcAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string Roles { get; set; } = string.Empty;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(authHeader))
        {
            context.Result = new UnauthorizedObjectResult(new { Message = "Yêu cầu cung cấp Authorization Header chứa Bearer Token." });
            return;
        }

        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
        var claimsDto = await authService.VerifyTokenAsync(authHeader);

        if (!claimsDto.IsValid)
        {
            context.Result = new UnauthorizedObjectResult(new { Message = "JWT Token không hợp lệ hoặc đã hết hạn." });
            return;
        }

        // Set ClaimsPrincipal to HttpContext so downstreams can read User information
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, claimsDto.UserId),
            new Claim(ClaimTypes.Name, claimsDto.Username),
            new Claim(ClaimTypes.Role, claimsDto.Role)
        ], "gRPCAuth");

        context.HttpContext.User = new ClaimsPrincipal(identity);

        // Check Roles if required
        if (!string.IsNullOrEmpty(Roles))
        {
            var requiredRoles = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var hasRole = false;
            foreach (var role in requiredRoles)
            {
                if (string.Equals(claimsDto.Role, role, StringComparison.OrdinalIgnoreCase))
                {
                    hasRole = true;
                    break;
                }
            }

            if (!hasRole)
            {
                context.Result = new ObjectResult(new { Message = $"Quyền '{claimsDto.Role}' không được phép truy cập tài nguyên này." })
                {
                    StatusCode = 403
                };
            }
        }
    }
}
