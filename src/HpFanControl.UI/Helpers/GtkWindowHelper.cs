using System;
using System.Runtime.InteropServices;
using HpFanControl.UI.Interop;

namespace HpFanControl.UI.Helpers;

public static class GtkWindowHelper
{
    private static IntPtr _cachedWindowPointer = IntPtr.Zero;
    public static IntPtr GetMainWindowPointer(string windowTitle)
    {
        if (_cachedWindowPointer != IntPtr.Zero)
            return _cachedWindowPointer;

        IntPtr list = NativeMethods.gtk_window_list_toplevels();
        if (list == IntPtr.Zero) return IntPtr.Zero;

        IntPtr current = list;
        IntPtr targetWindow = IntPtr.Zero;

        while (current != IntPtr.Zero)
        {
            IntPtr widget = Marshal.ReadIntPtr(current);
            IntPtr titlePtr = NativeMethods.gtk_window_get_title(widget);

            if (titlePtr != IntPtr.Zero)
            {
                string title = Marshal.PtrToStringUTF8(titlePtr);
                if (title == windowTitle)
                {
                    targetWindow = widget;
                    break;
                }
            }

            current = Marshal.ReadIntPtr(current + IntPtr.Size);
        }

        NativeMethods.g_list_free(list);

        if (targetWindow != IntPtr.Zero)
            _cachedWindowPointer = targetWindow;

        return targetWindow;
    }
}