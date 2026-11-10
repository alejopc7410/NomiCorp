using Microsoft.AspNetCore.Authorization;

namespace NominaEmpresa.API.Authorization;

public class PasswordChangeRequiredAttribute : AuthorizeAttribute
{
    public PasswordChangeRequiredAttribute()
    {
        Policy = "PasswordChangeCompleted";
    }
}
