using System.Security.Claims;
using RTWWebServer.Enums;

namespace RTWWebServer.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static long GetAccountId(this ClaimsPrincipal principal)
    {
        var accountIdClaim = principal.FindFirst("AccountId");
        if (accountIdClaim == null || !long.TryParse(accountIdClaim.Value, out var accountId))
        {
            throw new InvalidOperationException("AccountId claim not found or invalid.");
        }

        return accountId;
    }

    public static UserRole GetUserRole(this ClaimsPrincipal principal)
    {
        var roleClaim = principal.FindFirst("role");
        if (roleClaim == null || !Enum.TryParse<UserRole>(roleClaim.Value, true, out var role))
        {
            return UserRole.Normal; // 기본값
        }

        return role;
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("email")?.Value;
    }

    public static Guid? GetGuid(this ClaimsPrincipal principal)
    {
        var guidClaim = principal.FindFirst("guid");
        if (guidClaim != null && Guid.TryParse(guidClaim.Value, out var guid))
        {
            return guid;
        }

        return null;
    }
}