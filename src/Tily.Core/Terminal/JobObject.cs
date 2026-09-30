using System.ComponentModel;
using System.Runtime.InteropServices;
using Tily.Core.Native;

namespace Tily.Core.Terminal;

public sealed class JobObject : IDisposable
{
    private const int MaxListedProcesses = 1024;
    private readonly object _sync = new();
    private IntPtr _handle;

    public JobObject()
    {
        _handle = JobObjectApi.CreateJobObjectW(IntPtr.Zero, null);
        if (_handle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Création du Job Object impossible.");
        }

        var limits = new JobObjectExtendedLimitInformation();
        limits.BasicLimitInformation.LimitFlags = JobObjectApi.LimitKillOnJobClose;
        var size = Marshal.SizeOf<JobObjectExtendedLimitInformation>();
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(limits, buffer, false);
            if (!JobObjectApi.SetInformationJobObject(_handle, JobObjectApi.JobObjectExtendedLimitInformationClass, buffer, size))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Configuration du Job Object impossible.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public void Assign(IntPtr processHandle)
    {
        if (!JobObjectApi.AssignProcessToJobObject(_handle, processHandle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Rattachement du processus au Job Object impossible.");
        }
    }

    public IReadOnlyList<int> ProcessIds()
    {
        lock (_sync)
        {
            return _handle == IntPtr.Zero ? [] : QueryProcessIds();
        }
    }

    private List<int> QueryProcessIds()
    {
        var size = sizeof(uint) * 2 + IntPtr.Size * MaxListedProcesses;
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (!JobObjectApi.QueryInformationJobObject(_handle, JobObjectApi.JobObjectBasicProcessIdListClass, buffer, size, out _))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Lecture des processus du Job Object impossible.");
            }

            var count = Marshal.ReadInt32(buffer, sizeof(uint));
            var ids = new List<int>(count);
            for (var index = 0; index < count; index++)
            {
                ids.Add((int)Marshal.ReadIntPtr(buffer, sizeof(uint) * 2 + IntPtr.Size * index));
            }

            return ids;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public void Terminate()
    {
        lock (_sync)
        {
            if (_handle != IntPtr.Zero)
            {
                JobObjectApi.TerminateJobObject(_handle, 1);
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_handle == IntPtr.Zero)
            {
                return;
            }

            ProcessApi.CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }
    }
}
