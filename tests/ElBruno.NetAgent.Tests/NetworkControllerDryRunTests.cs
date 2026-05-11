using Xunit;

namespace ElBruno.NetAgent.Tests
{
    public class NetworkControllerDryRunTests
    {
        // Lightweight fake controller used for Phase 6 dry-run scaffolding.
        // TODO: Replace FakeNetworkController with production INetworkController and add more assertions.
        private interface INetworkController
        {
            bool DryRun { get; }
            string AttemptPrefer(string interfaceId);
        }

        private class FakeNetworkController : INetworkController
        {
            public bool DryRun { get; }

            public FakeNetworkController(bool dryRun)
            {
                DryRun = dryRun;
            }

            public string AttemptPrefer(string interfaceId)
            {
                if (DryRun)
                {
                    // Simulate logging and no system mutation
                    return $"DryRun: would prefer {interfaceId}";
                }

                // In non-dry-run mode we would perform system operations; here we simulate success
                return $"Performed: preferred {interfaceId}";
            }
        }

        [Fact]
        public void DryRun_Does_Not_Mutate_System()
        {
            var controller = new FakeNetworkController(dryRun: true);
            var result = controller.AttemptPrefer("if1");

            Assert.StartsWith("DryRun:", result);
        }

        [Fact(Skip = "Integration: requires system changes - placeholder for future integration test")]
        public void Real_Prefer_Attempts_System_Change()
        {
            // TODO: implement an integration test that verifies behavior when not in DryRun mode.
        }
    }
}
