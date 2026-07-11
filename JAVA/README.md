# Launcher Java Agent

The java agent **LauncherAgent** adds modded JAR files listed in the `jars.txt` file to class loader before aspectjweaver agent does its thing

*Ghostkeeper*

## JRE 8 SDK Details

**Java Development Kit**: Azul Zulu OpenJDK 8u472
**Version**: Zulu 8.90.0.19-CA
**Build**: OpenJDK 8u472-b08
**Platform**: Windows x64

## JRE 8 SDK Download

[Download link](https://cdn.azul.com/zulu/bin/zulu8.90.0.19-ca-jdk8.0.472-win_x64.zip)

## Compile agent class

`C:\SH\jre\bin\javac.exe -source 8 -target 8 -d . LauncherAgent.java`

## Generate agent JAR file

`C:\SH\jre\bin\jar.exe cfm LauncherAgent.jar MANIFEST.MF spacehavenlauncher`

## Verify agent

`C:\SH\jre\bin\jar.exe tf LauncherAgent.jar`

Expected Output:
```
META-INF/
META-INF/MANIFEST.MF
spacehavenlauncher/
spacehavenlauncher/LauncherAgent.class
```
