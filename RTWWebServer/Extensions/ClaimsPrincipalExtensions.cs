using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RTWWebServer.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetSubjectId(this ClaimsPrincipal user, out long subjectId)
    {
        // JWT 표준 sub 클레임 사용
        var claim = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!string.IsNullOrEmpty(claim) && long.TryParse(claim, CultureInfo.InvariantCulture, out subjectId))
        {
            return true;
        }

        subjectId = 0;
        return false;
    }
}