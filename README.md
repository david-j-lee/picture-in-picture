# Picture in Picture

This is an updated version of [PiP-Tool](https://github.com/LionelJouin/PiP-Tool).

> PiP tool is a software to use the Picture in Picture mode on Windows. This feature allows you to watch content (video for example) in thumbnail format on the screen while continuing to use any other software on Windows.

## Notable changes

- New UI design
- Updated to dotnet core 3.1
- Migrated from GalaSoft's MVVM Light to Microsoft's MVVM Toolkit
- Added dependency injection
- Migrated logging to ILogger
- Removed machine learning (might add back later)

### TODO

- Clean up view models and add services with better IoC and messaging
- Fix opacity in PiP window for the close button
- Toggle transparency on hover effects, separate from locking
- Include initial setup mode that requires the user to make two clicks to indicate where the windows bounds should be
- Consider https://github.com/dotnet/Microsoft.Maui.Graphics instead of `System.Drawing.Common`
- Unit Tests
