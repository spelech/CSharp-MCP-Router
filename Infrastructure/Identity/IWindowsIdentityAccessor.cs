using System.Security.Principal;

namespace McpRouter.Infrastructure.Identity
{
    public interface IWindowsIdentityAccessor
    {
        bool TryGetWindowsIdentityDetails(IIdentity? identity, out string? userSid, out List<string> groupSids);
    }

    public class WindowsIdentityAccessor : IWindowsIdentityAccessor
    {
        public bool TryGetWindowsIdentityDetails(IIdentity? identity, out string? userSid, out List<string> groupSids)
        {
            userSid = null;
            groupSids = new List<string>();

            if (identity is WindowsIdentity winIdentity)
            {
#pragma warning disable CA1416
                userSid = winIdentity.User?.Value;
                if (winIdentity.Groups != null)
                {
                    groupSids = winIdentity.Groups.Select(g => g.Value).ToList();
                }
#pragma warning restore CA1416
                return true;
            }

            return false;
        }
    }
}
