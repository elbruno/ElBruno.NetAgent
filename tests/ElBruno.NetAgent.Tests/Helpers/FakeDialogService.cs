using ElBruno.NetAgent.Core.Services;

namespace ElBruno.NetAgent.Tests.Helpers
{
    internal class FakeDialogService : IDialogService
    {
        public bool OpenLogsCalled { get; private set; }
        public bool OpenConfigCalled { get; private set; }

        public void OpenLogs() => OpenLogsCalled = true;
        public void OpenConfig() => OpenConfigCalled = true;
    }
}
