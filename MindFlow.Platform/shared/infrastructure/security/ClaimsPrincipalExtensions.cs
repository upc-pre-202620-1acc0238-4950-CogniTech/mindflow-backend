using System.Security.Claims;

namespace Mindflow_backend.Shared.Infrastructure.Security;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Id del usuario autenticado (claim <c>user_id</c> del JWT propio).</summary>
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirst("user_id")!.Value);
}
