using System.DirectoryServices.Protocols;
using System.Net;

namespace ModelContextGateway.Infrastructure.Identity
{
    public interface ILdapConnectionFactory
    {
        ILdapConnection CreateConnection(string server, int port, NetworkCredential? credential, AuthType authType);
    }
}
