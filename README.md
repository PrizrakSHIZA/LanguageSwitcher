# Language Switcher

Language Switcher is a small Windows tray utility for people who use several input languages but do not want to cycle through every installed language every time.

I wrote it because the default Windows language switching flow becomes annoying when you have more than two languages installed. For example, I may need English, Russian, Ukrainian, and Japanese installed, but I do not always want every one of them to appear in the normal cycle. This app lets me keep those languages installed in Windows while controlling which ones are included in the regular switching order.

## Features

- Runs quietly in the system tray.
- Tray menu with `Settings`, `Restart as administrator`, and `Exit`.
- Choose which Windows input languages are included in cyclic switching.
- Keep disabled languages available through dedicated hotkeys.
- Reorder enabled languages to control the switching sequence.
- Configure a global cycle hotkey.
- Configure per-language hotkeys.
- Optional startup with Windows.
- Restart as administrator when you need to switch input language inside elevated windows.

## Settings

Settings are stored per user in:

```text
%AppData%\LanguageSwitcher\settings.xml
```

The startup option is stored in the current user's Windows Run registry key:

```text
HKCU\Software\Microsoft\Windows\CurrentVersion\Run
```

## Build

Open `LanguageSwitcher.sln` in Visual Studio, select `Release`, and build the solution.

The release executable is created in:

```text
LanguageSwitcher\bin\Release\
```

If building from the command line with the .NET SDK, this project may need explicit resource task settings:

```powershell
dotnet msbuild LanguageSwitcher.sln /p:Configuration=Release /p:Platform="Any CPU" /p:GenerateResourceMSBuildArchitecture=CurrentArchitecture /p:GenerateResourceMSBuildRuntime=CurrentRuntime
```

## Installer

The repository includes an Inno Setup script:

```text
LanguageSwitcher.iss
```

Build the project in `Release` first, then compile the Inno Setup script. The generated installer is:

```text
installer\LanguageSwitcherSetup.exe
```

The installer output is a release artifact and should normally be uploaded to a GitHub Release rather than committed to the repository.

## Release Contents

Recommended release files:

- `LanguageSwitcherSetup.exe` for normal users.
- Optionally, a portable zip containing the contents of `LanguageSwitcher\bin\Release\` for users who do not want an installer.

For most releases, the installer alone is enough.
