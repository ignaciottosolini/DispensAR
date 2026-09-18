using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DispensAR.Api.Auth;

public sealed class AccountClaimsFactory(UserManager<Account> users, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<Account>(users, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(Account user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(Options.ClaimsIdentity.RoleClaimType, user.Rol.ToString()));
        if (user.EsAdministradorPlataforma)
            identity.AddClaim(new Claim("platform.administrator", "true"));
        return identity;
    }
}
