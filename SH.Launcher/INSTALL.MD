
# Description

Space Haven Launcher (SHL) was designed as a replacement for the old Mod Loader


# Player Features

- Does not modify original game files
- Responsive and customizable user interface
- Fast mod loading using multi-threading
- Cross-platform support for Windows, Linux, and macOS
- Integrated help resources available under the "Learning Computer" tab
- Fully customizable and persistent application settings
- Background slideshow with fan-art images
- FAQ, help documentation, and contextual tooltips


# Modding Features

- Modding instructions and tutorials
- Additional modding capabilities
- Real-time, detailed, and reliable logging
- Navigation to files and locations referenced by log entries
- Real-time logging of JVM output
- Intermediate build files (textures, XML files, per-mod logs)
- Optional automatic game launch after building
- Customizable mod page background and text colors


# Requirements

- A valid installation of Space Haven
- A supported operating system
- Sufficient disk space and memory for generating mod builds


# Installation

- SHL is currently distributed as a portable ZIP package
- Download the ZIP package matching your operating system
- Extract it to any desired location
- Review the setup sections below to ensure all required permissions and configurations are correct


# Application Security and Permissions


## Installation Troubleshooting

- Make sure the initial directories are configured in the application's **System Core** tab
- Make sure SHL has the required access permissions (see **Required I/O Permissions** below)

**Linux**

Ensure the application has execute permissions:

```bash
chmod 755 ./SpaceHavenLauncher
```

**macOS**

Preferably place the application under `/Applications/...`

Remove the downloaded application quarantine flag:

```bash
xattr -dr com.apple.quarantine SpaceHavenLauncher.app
```

Since SHL is not notarized by Apple, you may optionally sign the application yourself:

```bash
codesign --force --deep --sign - *
```


## Original Game Files

- SHL never modifies original game files
- Uninstalling SHL should not affect the original game installation
- Modded games can no longer be launched after SHL is removed


## Networking

SHL currently does not require network access for normal operation, and network access can safely be blocked.

Future versions may require network access for:
- Automatic updates
- Steam synchronization of subscribed mods
- Updating gameplay statistics for Steam


## Temporary Generated Files

- SHL generates files only inside its WORK directory
- The WORK directory contains generated files required for mod building and application operation
- Delete the WORK directory for a complete uninstall

The WORK directory is separate from the application directory:
- Windows: `C:\Users\<User>\AppData\Local\SpaceHavenLauncher`
- Linux: `~/.local/share/SpaceHavenLauncher (location may vary by distribution)`
- macOS: `~/Library/Application Support/SpaceHavenLauncher`

## Required I/O Permissions

SHL requires:
- READ access to the game directory
- READ access to the classic mods directory
- READ access to Steam and Steam Workshop directories
- READ access to SHL application directory
- READ/WRITE access to the WORK directory


## Optional I/O Permissions

- SHL automatically detects Space Haven and Steam locations when possible
- SHL may evaluate paths with different capitalization because some mods are not case-sensitive
- This may require additional READ access outside the main directories


## Mods and Modded Game Loading

- SHL creates modified game files based on the original game JAR files without changing the originals
- SHL launches the modded game using the game's Java runtime
- Mod JAR files are attached through a Java agent
- SHL does not verify, control, or take responsibility for third-party mods
- Users are responsible for ensuring that mods are obtained from trusted sources and are safe to use
- It is recommended to scan downloaded mods with antivirus software before loading them


## Collection and Submission of Debugging Data

- SHL can generate a DEBUG.ZIP file containing diagnostic information
- This file may be manually submitted by the user to SHL maintainers for troubleshooting
- SHL does not automatically upload or transmit this file
- Users may inspect the contents of DEBUG.ZIP before submitting it


## Backup

Open the System Core tab to locate the WORK directory.

Important files you may wish to back up:
- `app.xml` - persistent user interface settings
- `paths.xml` - persistent paths and directory settings
- `mods.xml` - loaded mod order
- `values` directory - current mod variable values


## Uninstall

- Back up any important files first (see the Backup section)
- To uninstall or update SHL, delete the application directory
- For a complete uninstall, also delete the WORK directory

