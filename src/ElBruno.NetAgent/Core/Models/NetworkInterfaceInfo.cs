using System.Collections.Generic;
using ElBruno.NetAgent.Core.Enums;

namespace ElBruno.NetAgent.Core.Models
{
    /// <summary>
    /// Describes a network interface discovered by the inventory service.
    /// </summary>
    public class NetworkInterfaceInfo
    {
        /// <summary>System-provided interface id.</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Human readable name.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Adapter description provided by the OS.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Classified adapter kind.</summary>
        public NetworkAdapterKind Kind { get; set; }

        /// <summary>Operational state.</summary>
        public NetworkOperationalState OperationalStatus { get; set; }

        /// <summary>Interface speed in bits per second.</summary>
        public long Speed { get; set; }

        /// <summary>Gateway addresses discovered for this interface.</summary>
        public IReadOnlyList<string> GatewayAddresses { get; set; } = new List<string>();

        /// <summary>Whether the interface is marked as up.</summary>
        public bool IsUp { get; set; }
    }
}
