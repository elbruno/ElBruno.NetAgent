Icon generation

Place a 256x256 or 32x32 Windows .ico file at assets\icons\netagent.ico to customize the tray icon. The app will load the file if present; otherwise it will attempt to load an embedded resource named '*netagent.ico' and if that is not present it falls back to the system application icon.

To create a .ico from a PNG, use an online converter or tools like ImageMagick:

magick convert icon.png -define icon:auto-resize=64,48,32,16 netagent.ico
