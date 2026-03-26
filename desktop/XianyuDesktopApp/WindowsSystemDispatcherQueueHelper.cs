using System;
using System.Runtime.InteropServices;

namespace XianyuDesktopApp;

public sealed class WindowsSystemDispatcherQueueHelper
{
    [StructLayout(LayoutKind.Sequential)]
    private struct DispatcherQueueOptions
    {
        public int dwSize;
        public int threadType;
        public int apartmentType;
    }

    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController(
        DispatcherQueueOptions options,
        out IntPtr dispatcherQueueController);

    private object? _dispatcherQueueController;

    public void EnsureWindowsSystemDispatcherQueueController()
    {
        if (_dispatcherQueueController is not null)
        {
            return;
        }

        var options = new DispatcherQueueOptions
        {
            dwSize = Marshal.SizeOf<DispatcherQueueOptions>(),
            threadType = 2,
            apartmentType = 2
        };
        CreateDispatcherQueueController(options, out var controllerPointer);
        _dispatcherQueueController = Marshal.GetObjectForIUnknown(controllerPointer);
    }
}
