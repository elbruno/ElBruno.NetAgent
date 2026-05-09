## Learnings

- Tests required allowing injection of config folder for ConfigurationService to avoid touching real user folders. Added optional constructor parameter `overrideFolder` and used it when building the config file path. This makes configuration testable and avoids using %LOCALAPPDATA% in unit tests.
