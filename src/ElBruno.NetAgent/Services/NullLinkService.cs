using ElBruno.NetAgent.Core.Services;

namespace ElBruno.NetAgent.Services
{
    /// <summary>
    /// No-op link opener for tests/non-UI hosts.
    /// </summary>
    public class NullLinkService : ILinkService
    {
        public void OpenLink(string url)
        {
            // no-op
        }
    }
}