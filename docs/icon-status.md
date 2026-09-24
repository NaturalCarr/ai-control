# Icon Status

AI Control uses the excavator artwork in both its window icon and notification-area icon. The source image is `src/excavator.png`; transparent pixels show the current status color.

The overall icon follows the most urgent service state:

- Red when a managed service crashes or reports a startup failure.
- Yellow while any managed service is starting or restarting.
- Green when no service is starting or in a crashed state.

The build script generates `ai-control.ico` from the PNG and embeds it in `ai-control.exe`. The PNG remains next to the source so the runtime notification-area icon can update its background color. Rebuild with `build.bat` after changing the artwork.
