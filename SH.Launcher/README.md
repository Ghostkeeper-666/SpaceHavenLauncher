# Space Haven Launcher (SHL)

This is a mod launcher application developed for Space Haven. It starts the original game as well as a modified one. It is intended to be a full replacement for the old Mod Loader project

This project is independent and is not affiliated with, endorsed by, or officially supported by BugByte unless explicitly stated otherwise.


## Installation

Installation instructions, prerequisites, and required configuration steps are provided in **INSTALL.md**.


## Main Features

- Mod builder and loader for modified game instances
- Advanced XML/JAVA mod build system
- Detailed error detection and reporting
- Immersive and responsive user interface
- Export of modified game data according to BugByte's EULA terms


## User Experience / User Interface

- This application makes use of GPU acceleration and multi-threading to provide a smooth and responsive user experience
- Comprehensive logging: Left-click a line to copy it; right-click it to navigate to the related file, directory, or link
- SHL background images can be made darker or be completely disabled
- Caching and hashing of intermediate files for improved build performance
- Valuable feedback for mod developers through logs and intermediate files
- Customizable persistent application settings
- Responsive user-friendly interface resembling the game theme
- Reliable mod variable value management
- Display of fan art as a slideshow


## Path Configuration

- Automatic detection of relevant paths for mod loading
- Manual setup of custom paths (also used as a fallback if automatic detection fails)
- Persistent path settings
- Generated files are primarily stored in the application's WORK directory


## Initialization

- Template files are prepared for optimized mod builds during app initialization
- Re-initialization can be triggered by clicking the buttons on the left side of the Navigation Console


## Mod Loading and Design

- SHL is not responsible for security threats, vulnerabilities, or other risks introduced by loaded mods
- Supported mod types: XML, JAVA, or XML+JAVA
- Easy enabling and disabling of mods without modifying the mod directory
- Detailed logging of mod loading
- Remembers mod loading sequence
- Detects malformed info.xml
- Detects missing mandatory fields in info.xml
- Allows modders to define font color and a background image for the mod page
- Detects MOD ID Conflicts
- Allows assigning a new CUSTOM ID to replace the MOD ID
- Allows mods without a MOD ID by assigning an AUTO ID
- To use AUTO ID, you must declare internal IDs using the following format: mid="{id}0001" (ALWAYS 4 digits after `{id}`)
- The {id} variable is reserved and must not be used!


## Mod Variables

- Mod variables are persisted in `WORK_DIR/values/*.xml` files
- The player can define custom mod variable values
- The player can set variable values from following sources: ORIGINAL, SUGGESTED, or OLD MOD VERSION

- Variables can be searched by their description
- Detection of duplicate declared variables in info.xml
- ORIGINAL variable values should make the mod resemble the original game as much as possible
- SUGGESTED variable values should make the game resemble the modder's intended behavior
- ORIGINAL variable values are read from the first existing attribute: 'original', 'default', or 'value'
- SUGGESTED variable values are read from the first existing attribute: 'suggested', or 'value'
- The CURRENT variable value defaults to SUGGESTED if never set before


## XML Mods

- Merging of content provided by mod `library/` XML files
- Patch operations defined by mod `patches/` XML files
- Sprite textures are provided by mod `textures/*` files
- Sprite sheet textures (CIM) are provided by mod `cim/*` files
- Audio is provided by mod `audio/*` files


## Mod Build

When launching a modified game, SHL performs a modded build:
- after re-initializing the application
- when the previous build has failed
- when any change is detected (mod loading sequence, mod variable, mod file content, etc)
- if the corresponding System Core settings force it


## XML Build Features

- Validation of XML merge/patch syntax errors
- Validation of missing mod variables
- Validation of malformed XPATH expressions
- Separate log for each mod
- Intermediate results for later analysis
- Autogeneration of sequential IDs for textures and audio
- Generation of version.txt / haven file version info
- Update the credits section with the names of mod authors and contributors
- Generation of mods.json file for JAVA mods


## JAVA Build Features

- The modded game is started using the game's shipped JRE
- Current AOP libraries: aspectjweaver-1.9.19.jar and aspectj-1.9.19.jar
- For advanced users: "vmArgs" can be manually configured in System Core tab
- The modded game is started by directly calling the JRE shipped with the game
- The modded JAR files are attached through the LauncherAgent JAVA agent
- Real-time logging of JVM output: set log verbosity to VERBOSE in System Core tab
- Make sure you understand the JAVA technologies available in the version included with the game before creating advanced JAVA mods


## Mod Compatibility

- SHL provides tools for loading and building mods but cannot guarantee compatibility between third-party mods.
- SHL provides tools to assist with resolving conflicting MOD IDs, but some conflicts require manual resolution by mod authors.
- Mod conflicts must be resolved by mod authors or users.


## Bug Reports and Support

- When reporting issues, please provide the `DEBUG.ZIP` file generated by SHL from the System Core tab, as well as detailed reproduction steps whenever possible.
- The official Space Haven Discord is the primary supported channel for bug reports and technical support. Reports submitted through other channels may not receive support.


## Terms of Use and License Restrictions

- You are granted permission to use SHL for personal and community purposes, provided that you comply with these terms and all applicable third-party licenses.
- SHL is a non-profit project developed and maintained by Ghostkeeper666, relying solely on voluntary donations from its supporters.
- These terms apply to SHL-created source code, binaries, assets, documentation, and other project files, excluding third-party components governed by their own licenses.
- You may not redistribute or publish SHL binaries, source code, or project files, or otherwise make SHL or any part of it publicly available without the explicit written permission of Ghostkeeper666.
- To the extent permitted by applicable law, you may not modify, reverse engineer, decompile, or create derivative works of SHL without the explicit written permission of Ghostkeeper666.
- SHL is provided "as is" without warranties of any kind. Ghostkeeper666 shall not be liable for any damage, data loss, corrupted files, crashes, security issues, vulnerabilities, or other problems resulting from the use of SHL or third-party mods.
- SHL does not create, verify, or take responsibility for third-party mods. Each mod remains the responsibility of its respective author, and users are responsible for ensuring compliance with applicable mod licenses.
- Any additional permission granted beyond these terms for redistribution, modification, or publication may be revoked at any time.
- Ghostkeeper666 reserves the right to modify these terms at any time.


## Data Collection

- SHL may generate local diagnostic information, logs, and debug files strictly required for mod loading, troubleshooting, and debugging purposes.


## Export Responsibility

- SHL does not grant any ownership, licensing, or redistribution rights to exported game files, Space Haven files, or third-party mod files.
- Users are solely responsible for ensuring that any exported or shared files generated by SHL comply with the BugByte EULA and all applicable mod licenses.
- SHL does not authorize or permit the redistribution, publication, or sharing of files belonging to Space Haven or third-party mods unless explicitly allowed by their respective owners or licenses.


## Roadmap

- With the explicit written permission of Ghostkeeper666 and the full support of BugByte, this project may transition into a proprietary project under an agreement between BugByte and Ghostkeeper666.
- Alternatively, the project may be released as an open-source initiative, while voluntary donations continue to support the ongoing work of Ghostkeeper666.

