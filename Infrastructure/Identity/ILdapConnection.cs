using System.DirectoryServices.Protocols;

namespace ModelContextGateway.Infrastructure.Identity
{
    public interface ILdapConnection : IDisposable
    {
        void Bind();
        void SetSessionOptions(int protocolVersion, bool secureSocketLayer);
        DirectoryResponse SendRequest(DirectoryRequest request);
    }
}
