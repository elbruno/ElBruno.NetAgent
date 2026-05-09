## Learnings

- Implemented Phase 2 configuration system: NetAgentOptions + IConfigurationService + ConfigurationService.
- Preserved DryRunMode default to true to be safe.
- Added a NullConfigurationService for unit-test compatibility with TrayIconService.
- Added Phase 3: network inventory models, INetworkInventoryService, and NetworkInventoryService with USB tethering heuristics and interface logging.

