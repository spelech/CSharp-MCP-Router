using System.DirectoryServices.Protocols;
using System.Net;

namespace ModelContextGateway.Infrastructure.Identity
{
    public class LdapConnectionFactory : ILdapConnectionFactory
    {
        public ILdapConnection CreateConnection(string server, int port, NetworkCredential? credential, AuthType authType)
        {
            return new LdapConnectionWrapper(server, port, credential, authType);
        }
    }
}
