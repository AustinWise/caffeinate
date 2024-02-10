using System.Diagnostics;
using Windows.Win32;
using Windows.Win32.System.Power;

namespace caffeinate;

// All the APIs we are using are available on Windows 7 and higher.
#pragma warning disable CA1416 // Validate platform compatibility

class Program(EXECUTION_STATE state, int? timeout, int? pidToWatch, List<string> utilityArgs)
{
    static void Usage(string? errorMessage = null)
    {
        if (errorMessage is not null)
            Console.Error.WriteLine(errorMessage);
        string? processName = Path.GetFileName(Environment.ProcessPath);
        if (processName is null)
            processName = "caffeinate";
        Console.Error.WriteLine($"usage: {processName}[-di] [-t timeout] [-w Process ID] [command arguments...]");
        Environment.Exit(1);
    }

    static int Main(string[] args)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                Usage("Only Windows is supported.");
            }
            Program p = parseArgs(args);
            p.Run();
            return 0;
        }
        catch (ExitException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("PROGRAM CRASHED");
            Console.Error.WriteLine(ex);
            return 2;
        }
        finally
        {
            PInvoke.SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
        }
    }

    private static Program parseArgs(string[] args)
    {
        bool caffeinateDisplay = false;
        bool caffeinateSystem = false;
        int? timeout = null;
        int? pidToWatch = null;

        int argNdx;
        for (argNdx = 0; argNdx < args.Length; argNdx++)
        {
            string a = args[argNdx];
            if (a.Length == 0 || a[0] != '-')
            {
                break;
            }
            for (int charNdx = 1; charNdx < a.Length; charNdx++)
            {
                switch (a[charNdx])
                {
                    case 'd':
                        caffeinateDisplay = true;
                        break;
                    case 'i':
                        caffeinateSystem = true;
                        break;
                    case 't':
                        {
                            argNdx++;
                            if (argNdx < args.Length && int.TryParse(args[argNdx], out int i))
                            {
                                if (i < 0)
                                {
                                    Usage("Timeout must be a postive number.");
                                }
                                timeout = i;
                            }
                            else
                            {
                                Usage("Expected timeout to follow '-t' flag");
                            }
                        }
                        break;
                    case 'w':
                        {
                            argNdx++;
                            if (argNdx < args.Length && int.TryParse(args[argNdx], out int i))
                            {
                                pidToWatch = i;
                            }
                            else
                            {
                                Usage("Expected PID to follow '-w' flag");
                            }
                        }
                        break;
                    default:
                        Usage($"Unknown command line flag: {a[charNdx]}");
                        break;
                }
            }
        }

        var utilityArgs = new List<string>();
        for (; argNdx < args.Length; argNdx++)
        {
            utilityArgs.Add(args[argNdx]);
        }

        if (!caffeinateDisplay && !caffeinateSystem)
        {
            caffeinateSystem = true;
        }

        var esFlags = EXECUTION_STATE.ES_CONTINUOUS;
        if (caffeinateDisplay)
            esFlags |= EXECUTION_STATE.ES_DISPLAY_REQUIRED;
        if (caffeinateSystem)
            esFlags |= EXECUTION_STATE.ES_SYSTEM_REQUIRED;


        var p = new Program(esFlags, timeout, pidToWatch, utilityArgs);
        return p;
    }

    Process? GetProcessToWatch()
    {
        Process? processToWatch = null;
        if (utilityArgs.Count != 0)
        {
            var psi = new ProcessStartInfo(utilityArgs[0]);
            for (int i = 1; i < utilityArgs.Count; i++)
            {
                psi.ArgumentList.Add(utilityArgs[i]);
            }
            try
            {
                return Process.Start(psi);

            }
            catch (Exception ex)
            {
                throw new ExitException("Failed to start process: " + ex.Message);
            }
        }
        else if (pidToWatch.HasValue)
        {
            try
            {
                processToWatch = Process.GetProcessById(pidToWatch.Value);
            }
            catch (Exception ex)
            {
                throw new ExitException($"Could not find process with id {pidToWatch}: {ex.Message}");
            }
        }
        return processToWatch;
    }

    void Run()
    {
        PInvoke.SetThreadExecutionState(state);

        using Process? processToWatch = GetProcessToWatch();

        if (processToWatch is not null)
        {
            processToWatch.WaitForExit();
        }
        else
        {
            if (timeout.HasValue)
            {
                Thread.Sleep(TimeSpan.FromSeconds(timeout.Value));
            }
            else
            {
                Thread.Sleep(Timeout.Infinite);
            }
        }
    }
}
