using Microsoft.AspNetCore.Identity;

namespace LMS.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<int>
{
    public ApplicationRole() : base()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }
}

