using System.DirectoryServices.Protocols;
using System.Net;

namespace ModelContextGateway.Infrastructure.Identity
{
    public class LdapConnectionWrapper : ILdapConnection
    {
        private readonly LdapConnection _connection;

        public LdapConnectionWrapper(string server, int port, NetworkCredential? credential, AuthType authType)
        {
            var identifier = new LdapDirectoryIdentifier(server, port);
            _connection = new LdapConnection(identifier, credential, authType);
        }

        public void Bind()
        {
            _connection.Bind();
        }

        public void SetSessionOptions(int protocolVersion, bool secureSocketLayer)
        {
            _connection.SessionOptions.ProtocolVersion = protocolVersion;
            _connection.SessionOptions.SecureSocketLayer = secureSocketLayer;
        }

        public DirectoryResponse SendRequest(DirectoryRequest request)
        {
            return _connection.SendRequest(request);
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
