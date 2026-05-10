namespace ElBruno.NetAgent.Core.Services
{
    /// <summary>
    /// Abstraction for opening external links (e.g., GitHub) to make it testable.
    /// </summary>
    public interface ILinkService
    {
        void OpenLink(string url);
    }
}