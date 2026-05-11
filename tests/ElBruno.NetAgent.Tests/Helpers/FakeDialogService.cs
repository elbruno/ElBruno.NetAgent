using ElBruno.NetAgent.Core.Services;

namespace ElBruno.NetAgent.Tests.Helpers
{
    internal class FakeDialogService : IDialogService
    {
        public bool OpenLogsCalled { get; private set; }
        public bool OpenConfigCalled { get; private set; }
        public bool OpenConfigFolderCalled { get; private set; }
        public bool ShowAboutCalled { get; private set; }
        public void OpenLogs() => OpenLogsCalled = true;
        public void OpenConfig() => OpenConfigCalled = true;
        public void OpenConfigFolder() => OpenConfigFolderCalled = true;
        public void ShowAbout() => ShowAboutCalled = true;
    }
}
