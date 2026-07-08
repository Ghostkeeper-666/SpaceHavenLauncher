package spacehavenlauncher;

import java.io.*;
import java.lang.instrument.Instrumentation;
import java.lang.reflect.Method;
import java.net.URL;
import java.net.URLClassLoader;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.util.Date;

public class LauncherAgent
{
    private static PrintWriter log;

    public static void premain(String agentArgs, Instrumentation instrumentation) throws Exception
    {
        try
        {
            File baseDirectory = GetAgentDirectory();

            log = new PrintWriter(new FileWriter(new File(baseDirectory, "launcheragent.log")));

            Log("=================================================");
            Log("=== SPACE HAVEN LAUNCHER AGENT BY GHOSTKEEPER ===");
            Log("=================================================");
            Log("Agent started");
            Log("Agent directory: " + baseDirectory.getAbsolutePath());

            File jarsFile = new File(baseDirectory, "jars.txt");

            if (!jarsFile.isFile())
            {
                Log("jars.txt not found");
                return;
            }

            URLClassLoader classLoader = (URLClassLoader)ClassLoader.getSystemClassLoader();

            Log("System ClassLoader: " + classLoader);

            Method addURL = URLClassLoader.class.getDeclaredMethod("addURL", URL.class);
            addURL.setAccessible(true);

            BufferedReader reader = new BufferedReader(new InputStreamReader(new FileInputStream(jarsFile), StandardCharsets.UTF_8));

            try
            {
                String line;

                while ((line = reader.readLine()) != null)
                {
                    line = line.trim();

                    if (line.length() == 0 || line.startsWith("#"))
                        continue;

                    Path path = Paths.get(line);

                    if (!path.isAbsolute())
                        path = baseDirectory.toPath().resolve(path);

                    URL url = path.toFile().toURI().toURL();

                    Log("Adding: " + url);

                    addURL.invoke(classLoader, url);
                }
            }
            finally
            {
                reader.close();
            }

            Log("Agent finished");
        }
        catch (Exception e)
        {
            if (log != null)
            {
                Log("ERROR:");
                e.printStackTrace(log);
            }

            throw e;
        }
        finally
        {
            if (log != null)
                log.close();
        }
    }

    private static File GetAgentDirectory()
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
            throw new RuntimeException(e);
        }
    }

    private static void Log(String message)
    {
        if (log == null)
            return;

        log.println(message);
        log.flush();
    }
}
