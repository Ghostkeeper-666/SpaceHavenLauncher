package spacehavenlauncher;

import java.io.*;
import java.lang.instrument.Instrumentation;
import java.lang.reflect.Method;
import java.net.URL;
import java.net.URLClassLoader;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;

public class LauncherAgent
{
    private static File logPath;
    private static final StringBuilder logContent = new StringBuilder();

    public static void premain(String agentArgs, Instrumentation instrumentation) throws Exception
    {
        try
        {
            File baseDirectory = getAgentDirectory();
            logPath = new File(baseDirectory, "LauncherAgent.log");

            log("======================================================");
            log("=== SPACE HAVEN LAUNCHER JAVA AGENT by GHOSTKEEPER ===");
            log("======================================================");
            log("Agent started");
            log("Directory: " + baseDirectory.getAbsolutePath());

            log("Java version: " + System.getProperty("java.version"));
            log("Java runtime: " + System.getProperty("java.runtime.version"));
            log("Java vendor: " + System.getProperty("java.vendor"));
            log("JVM: " + System.getProperty("java.vm.name"));

            log("Retrieving class loader...");
            URLClassLoader classLoader = (URLClassLoader)ClassLoader.getSystemClassLoader();
            Method addURL = URLClassLoader.class.getDeclaredMethod("addURL", URL.class);
            addURL.setAccessible(true);
            log("Using class loader: " + classLoader);

            log("Reading jars.txt file...");
            File jarsFile = new File(baseDirectory, "jars.txt");
            if (!jarsFile.isFile())
                throw new RuntimeException("The jars.txt file could not be found at " + jarsFile.getAbsolutePath());

            try (BufferedReader reader = new BufferedReader(new InputStreamReader(new FileInputStream(jarsFile), StandardCharsets.UTF_8)))
            {
                String line;
                boolean success = true;

                log("Modifying class loader's search paths...");
                while ((line = reader.readLine()) != null)
                {
                    line = line.trim();

                    if (line.length() == 0 || line.startsWith("#"))
                        continue;

                    Path path = Paths.get(line);
                    if (!path.isAbsolute())
                        path = baseDirectory.toPath().resolve(path);
                    URL url = path.toFile().toURI().toURL();
                    log("Adding JAR: " + url);

                    if (!Files.isRegularFile(path))
                    {
                        success = false;
                        log("ERROR: JAR file not found: " + path);
                        continue;
                    }

                    addURL.invoke(classLoader, url);
                }
                
                if(!success)
                    throw new RuntimeException("Some JAR paths could not be added to the class loader");
            }

            log("The agent has completed successfully");
            return;
        }
        catch (Exception e)
        {
            // Try to read the stacktrace:
            try
            {
                StringWriter sw = new StringWriter();
                PrintWriter pw = new PrintWriter(sw);
                e.printStackTrace(pw);
                pw.flush();
                String stackTrace = ("ERROR: " + sw.toString()).replaceAll("[\\r\\n]+", " ");
                log(stackTrace);
            }
            catch (Exception ignored)
            {
                log("ERROR: " + e.toString());
            }

            String failed = "The agent has failed!";
            log(failed);

            throw new RuntimeException(failed, e);
        }
    }

    private static File getAgentDirectory()
    {
        try
        {
            File location = new File(LauncherAgent.class.getProtectionDomain().getCodeSource().getLocation().toURI());
            if (location.isFile())
                return location.getParentFile();
            return location;
        }
        catch (Exception e)
        {
            // Fallback: write to cwd ?!?
            throw new RuntimeException("Cannot determine agent directory", e);
        }
    }

    private static void log(String msg)
    {
        if(msg == null)
            msg = "";
        logContent.append("[LauncherAgent]  " + msg).append(System.lineSeparator());
        tryWriteLog();
    }

    private static void tryWriteLog()
    {
        if (logPath == null)
            return;

        String content = logContent.toString();

        for (int i = 0; i < 5; i++)
        {
            try (Writer writer = new OutputStreamWriter(new FileOutputStream(logPath, false), StandardCharsets.UTF_8))
            {
                writer.write(content);
                return;
            }
            catch (IOException e)
            {
                try
                {
                    Thread.sleep(50);
                }
                catch (InterruptedException ignored)
                {
                    Thread.currentThread().interrupt();
                    break;
                }
            }
        }
    }

}
