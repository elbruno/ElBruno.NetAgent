using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.Core.Services;

namespace ElBruno.NetAgent.Tests.Helpers
{
    internal class FakeNetworkInventoryService : INetworkInventoryService
    {
        public bool WasCalled { get; private set; }
        public IReadOnlyList<NetworkInterfaceInfo> InterfacesToReturn { get; set; } = new List<NetworkInterfaceInfo>();

        public Task<IReadOnlyList<NetworkInterfaceInfo>> GetInterfacesAsync(CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(InterfacesToReturn);
        }
    }
}
