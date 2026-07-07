using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace FModLoaderInstaller.Services;

/// <summary>
/// UAC elevation check and re-launch service.
/// </summary>
public static class ElevationService
{
    /// <summary>Returns true if the current process has admin privileges.</summary>
    public static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    /// <summary>Attempts to re-launch the current process with admin rights via UAC.</summary>
    public static bool RelaunchAsAdmin()
    {
        try
        {
            var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
            if (exePath == null) return false;

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
