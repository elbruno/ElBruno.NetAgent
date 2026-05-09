# ElBruno.NetAgent – Configuration

## 1. Runtime configuration location

The app should use this file:

```text
%LOCALAPPDATA%\ElBruno.NetAgent\config.json
```

## 2. Repository sample

The repository must include:

```text
docs/config.sample.json
```

## 3. Default configuration

```json
{
  "AutoModeEnabled": false,
  "DryRunMode": true,
  "CheckIntervalSeconds": 5,
  "PingTimeoutMs": 1000,
  "LatencyThresholdMs": 180,
  "PacketLossThresholdPercent": 20,
  "FailoverDurationSeconds": 20,
  "FailbackCooldownSeconds": 120,
  "MinimumChecksBeforeSwitch": 3,
  "MinimumScoreDeltaToSwitch": 20,
  "PreferredAdapterKinds": [
    "UsbTethering",
    "Ethernet",
    "WiFi"
  ],
  "IgnoredAdapterNameContains": [
    "Loopback",
    "VirtualBox",
    "Hyper-V",
    "VMware"
  ],
  "PingEndpoints": [
    "1.1.1.1",
    "8.8.8.8",
    "9.9.9.9"
  ],
  "LogLevel": "Information",
  "ShowNotifications": true,
  "StartMinimizedToTray": true
}
```

## 4. Settings

### AutoModeEnabled

When `true`, the app may automatically prefer another interface based on decision rules.

Default: `false`

Reason: first launch should be safe.

### DryRunMode

When `true`, the app logs intended switch actions but does not change Windows settings.

Default: `true`

Reason: safe AI-generated first implementation.

### CheckIntervalSeconds

How often network quality is measured.

Default: `5`

### PingTimeoutMs

Timeout per ping request.

Default: `1000`

### LatencyThresholdMs

Latency above this value is considered degraded/poor depending on other metrics.

Default: `180`

### PacketLossThresholdPercent

Packet loss above this value is considered poor.

Default: `20`

### FailoverDurationSeconds

The current network must remain unhealthy for this duration before automatic switch is considered.

Default: `20`

### FailbackCooldownSeconds

Minimum time after a switch before another automatic switch is allowed.

Default: `120`

### MinimumChecksBeforeSwitch

Minimum number of quality samples required before switching.

Default: `3`

### MinimumScoreDeltaToSwitch

Candidate interface must be better than current interface by at least this score delta.

Default: `20`

### PreferredAdapterKinds

Preferred adapter order when multiple candidates have similar quality.

Default:

```json
[
  "UsbTethering",
  "Ethernet",
  "WiFi"
]
```

### IgnoredAdapterNameContains

Adapter descriptions containing these values should not be preferred automatically.

### PingEndpoints

Endpoints used for latency checks.

The implementation should try all endpoints and use aggregate results.

## 7. Live Mode Settings

### LiveModeAllowed

When `true`, the controller is permitted to use live mode for network operations.

**Default: `false`**

**Important:** This is the hard safety gate. Even if `DryRunMode` is `false` and `NetworkSwitchMode.Live` is requested, live operations will NOT execute unless this flag is `true`.

Configuration validation will **reject** any config where `LiveModeAllowed` is `true`.

## 8. Validation rules

- `CheckIntervalSeconds` must be >= 2.
- `PingTimeoutMs` must be >= 250.
- `LatencyThresholdMs` must be >= 50.
- `PacketLossThresholdPercent` must be between 0 and 100.
- `FailbackCooldownSeconds` must be >= 30.
- At least one ping endpoint must be configured.
- `DryRunMode` must be `true` (enforced by Phase 10 safety rules).
- `AutoModeEnabled` must be `false` (enforced by Phase 10 safety rules).
- `LiveModeAllowed` must be `false` (enforced by Phase 10 safety rules).

## 9. Safe defaults

First launch must use:

```json
{
  "AutoModeEnabled": false,
  "DryRunMode": true
}
```

The user must explicitly enable real switching.
