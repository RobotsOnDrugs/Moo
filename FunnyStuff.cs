using System;
using System.Diagnostics;

using System.Runtime.InteropServices;

namespace Moo;
internal static partial class FunnyStuff
{
	[LibraryImport("user32.dll")]
	public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
	internal static void MessWithWindows(HWND skip_hwnd)
	{
		WNDENUMPROC hide_window_callback = new(HideWindowAndOwner);
		BOOL windowsenumed = EnumWindows(hide_window_callback, (LPARAM)(nint)skip_hwnd);
		bool clipped = ClipCursor(GetBottomCorner());
		SetCursor(null).Close();
	}
	internal static BOOL HideWindowAndOwner(HWND hwnd, LPARAM lParam)
	{
		HWND owner_hwnd = GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER);
		uint thread_id = GetWindowThreadProcessId((nint)owner_hwnd, out uint process_handle);
		Process owner_process = Process.GetProcessById((int)process_handle);
		if (((nint)hwnd != lParam.Value) && ((nint)owner_hwnd != lParam.Value))
		{
			_ = ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_HIDE);
			_ = ShowWindow(owner_hwnd, SHOW_WINDOW_CMD.SW_HIDE);
			_ = SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
			_ = SetWindowLongPtr(owner_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
		}
		return true;
	}
	public static RECT GetBottomCorner()
	{
		_ = GetClientRect(GetDesktopWindow(), out RECT rect);
		return new(rect.Width, rect.Height, rect.Width, rect.Height);
	}
}
