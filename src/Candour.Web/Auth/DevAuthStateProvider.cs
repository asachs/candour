namespace Candour.Web.Auth;

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

public class DevAuthStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "dev-user"),
            new Claim("oid", "00000000-0000-0000-0000-000000000000"),
        }, "DevAuth");

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
}
