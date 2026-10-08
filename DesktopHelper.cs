using System;
using System.Runtime.InteropServices;

namespace HomeworkMemo;

internal static class DesktopHelper
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    private static IntPtr _workerW = IntPtr.Zero;

    public static bool AttachToDesktop(IntPtr hwnd)
    {
        var workerW = GetDesktopWorkerW();
        if (workerW == IntPtr.Zero) return false;
        SetParent(hwnd, workerW);
        return true;
    }

    public static void DetachFromDesktop(IntPtr hwnd)
    {
        SetParent(hwnd, IntPtr.Zero);
    }

    private static IntPtr GetDesktopWorkerW()
    {
        if (_workerW != IntPtr.Zero && IsWindow(_workerW))
            return _workerW;

        var progman = FindWindow("Progman", null);
        if (progman != IntPtr.Zero)
        {
            SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0x0000, 1000, out _);
        }

        var result = IntPtr.Zero;
        EnumWindows((top, lParam) =>
        {
            var defView = FindWindowEx(top, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero)
            {
                var workerW = FindWindowEx(IntPtr.Zero, top, "WorkerW", null);
                if (workerW != IntPtr.Zero)
                {
                    result = workerW;
                    return false;
                }
            }
            return true;
        }, IntPtr.Zero);

        _workerW = result;
        return result;
    }
}
