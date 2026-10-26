using DataSenderPlusProject.Helpers;
using System;
using System.Diagnostics;
using System.Threading;

internal static class SingleInstanceGuardTests
{
    private static int Child(string name, string mode)
    {
        using (var process = Process.Start(new ProcessStartInfo
        {
            FileName = System.Reflection.Assembly.GetExecutingAssembly().Location,
            Arguments = mode + " " + name,
            UseShellExecute = false,
            CreateNoWindow = true
        }))
        {
            if (!process.WaitForExit(10000)) throw new Exception("Child timeout");
            return process.ExitCode;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            var childGuard = new SingleInstanceGuard(args[1]);
            bool acquired = childGuard.TryAcquire();
            if (args[0] == "abandon") Environment.Exit(acquired ? 0 : 2);
            childGuard.Dispose();
            return acquired ? 0 : 2;
        }
        string name = "DataSenderPlus-Test-" + Guid.NewGuid();
        using (var guard = new SingleInstanceGuard(name))
        {
            Check(guard.TryAcquire(), "First process should acquire");
            Check(guard.TryAcquire(), "Repeated same-process check should succeed");
            Check(Child(name, "check") == 2, "Second process must be rejected");
            Check(guard.TryAcquire(), "Rejected second process must not affect first");
        }
        Check(Child(name, "check") == 0, "Restart must acquire after release");
        using (var keepAlive = new Mutex(false, name))
        {
            Check(Child(name, "abandon") == 0, "Child must acquire before abandonment");
            using (var guard = new SingleInstanceGuard(name))
                Check(guard.TryAcquire(), "Recover abandoned mutex");
        }
        Console.WriteLine("PASS: first/repeated acquire, rejection without affecting owner, restart, and crash recovery.");
        return 0;
    }
}
