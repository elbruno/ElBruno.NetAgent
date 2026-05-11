using System.Collections.Generic;
using ElBruno.NetAgent.Core.Configuration;
using ElBruno.NetAgent.Core.Models;
using ElBruno.NetAgent.ViewModels;
using ElBruno.NetAgent.Tests.Helpers;
using Xunit;

namespace ElBruno.NetAgent.Tests
{
    public class StatusViewModelTests
    {
        [Fact]
        public void ViewModel_Exposes_Properties()
        {
            var inventory = new FakeNetworkInventoryService();
            var monitor = new FakeNetworkQualityMonitor();
            var dialog = new FakeDialogService();
            var options = new NetAgentOptions { AutoModeEnabled = true };

            var vm = new StatusViewModel(inventory, monitor, dialog, options);

            Assert.NotNull(vm.CurrentInterface);
            Assert.NotNull(vm.DetectedInterfaces);
            Assert.IsType<double>(vm.Latency);
            Assert.IsType<double>(vm.QualityScore);
            Assert.IsType<bool>(vm.AutoMode);
            Assert.NotNull(vm.LastDecision);
            Assert.NotNull(vm.LastSwitchTimestamp);
        }

        [Fact]
        public void RefreshCommand_Invokes_Services()
        {
            var inventory = new FakeNetworkInventoryService();
            inventory.InterfacesToReturn = new List<NetworkInterfaceInfo>
            {
                new NetworkInterfaceInfo { Id = "1", Name = "eth0", IsUp = true }
            };
            var monitor = new FakeNetworkQualityMonitor();
            var dialog = new FakeDialogService();
            var options = new NetAgentOptions();

            var vm = new StatusViewModel(inventory, monitor, dialog, options);

            vm.RefreshCommand.Execute(null);

            Assert.True(inventory.WasCalled);
            Assert.True(monitor.WasCalled);
            Assert.Equal("eth0", vm.CurrentInterface);
            Assert.Equal(10, vm.Latency);
            Assert.Equal(90, vm.QualityScore);
        }

        [Fact]
        public void OpenCommands_Call_DialogService()
        {
            var inventory = new FakeNetworkInventoryService();
            var monitor = new FakeNetworkQualityMonitor();
            var dialog = new FakeDialogService();
            var options = new NetAgentOptions();

            var vm = new StatusViewModel(inventory, monitor, dialog, options);

            vm.OpenLogsCommand.Execute(null);
            vm.OpenConfigCommand.Execute(null);

            Assert.True(dialog.OpenLogsCalled);
            Assert.True(dialog.OpenConfigCalled);
        }

        [Fact]
        public void AutoMode_False_When_Options_Disable()
        {
            var inventory = new FakeNetworkInventoryService();
            var monitor = new FakeNetworkQualityMonitor();
            var dialog = new FakeDialogService();
            var options = new NetAgentOptions { AutoModeEnabled = false };

            var vm = new StatusViewModel(inventory, monitor, dialog, options);

            Assert.False(vm.AutoMode);
        }
    }
}
