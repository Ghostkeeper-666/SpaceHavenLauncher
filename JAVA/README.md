# Launcher Java Agent

The LauncherAgent loads the all modded JARs from the "jars.txt" file before the aspectjweaver agent does its thing.

*Ghostkeeper*

## JRE 8 SDK - Details

**Java Development Kit**: Azul Zulu OpenJDK 8u472
**Version**: Zulu 8.90.0.19-CA
**Build**: OpenJDK 8u472-b08
**Platform**: Windows x64

## JRE 8 SDK - Download Link

https://cdn.azul.com/zulu/bin/zulu8.90.0.19-ca-jdk8.0.472-win_x64.zip

## Compile agent class

`..\jdk8.0.472\bin\javac.exe -source 8 -target 8 -d . LauncherAgent.java`

## Generate agent JAR file

`..\jdk8.0.472\bin\jar.exe cfm LauncherAgent.jar MANIFEST.MF spacehavenlauncher`

## Verify agent

`..\jdk8.0.472\bin\jar.exe tf LauncherAgent.jar`

Expected Output:
```
META-INF/
META-INF/MANIFEST.MF
spacehavenlauncher/
spacehavenlauncher/LauncherAgent.class
```
