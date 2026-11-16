# Launcher Java Agent

The java agent **LauncherAgent** adds modded JAR files listed in the `jars.txt` file to class loader before aspectjweaver agent does its thing

*Ghostkeeper*

## JDK 8 Downloads (used by the game until June 2026)

**Java Version**: Azul Zulu OpenJDK 8u472  
**Zulu Version**: 8.90.0.19-CA  
**Build**: OpenJDK 8u472-b08  
**Architecture**: x64

- [JDK 8 - Windows download](https://cdn.azul.com/zulu/bin/zulu8.90.0.19-ca-jdk8.0.472-win_x64.zip)
- [JDK 8 - macOS download](https://cdn.azul.com/zulu/bin/zulu8.90.0.19-ca-jdk8.0.472-macosx_x64.zip)
- [JDK 8 - Linux download](https://cdn.azul.com/zulu/bin/zulu8.90.0.19-ca-jdk8.0.472-linux_x64.tar.gz)

## JDK 11 Downloads (used by the game until June 2026, but only on MacOS so far...)

**Java Version**: Azul Zulu OpenJDK 11.0.27  
**Zulu Version**: 11.80.21-CA  
**Build**: OpenJDK 11.0.27+6-LTS  
**Architecture**: x64

- [JDK 11 - Windows download](https://cdn.azul.com/zulu/bin/zulu11.80.21-ca-jdk11.0.27-win_x64.zip)
- [JDK 11 - macOS download](https://cdn.azul.com/zulu/bin/zulu11.80.21-ca-jdk11.0.27-macosx_x64.zip)
- [JDK 11 - Linux download](https://cdn.azul.com/zulu/bin/zulu11.80.21-ca-jdk11.0.27-linux_x64.tar.gz)

## Compile agent class - use JDK 11 for that

Use the JDK 11, with compatibility for Java 8:

`C:\SH\jre\bin\javac.exe --release 8 -d . LauncherAgent.java`

If for some reason you still want to use the JDK 8:

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
