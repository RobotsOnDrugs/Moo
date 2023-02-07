using System;
#if RELEASE
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
#endif

using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Win32;

using static Windows.Win32.PInvoke;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using System.Runtime.InteropServices;

namespace AvaloniaSandbox;
public partial class MainWindow : Window
{
	[LibraryImport("user32.dll")]
	internal static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
	private int AltKeyPressed = 0;
	public MainWindow()
	{
		KeyUp += MainWindow_KeyUp;
		bool size_success = GetClientRect(GetDesktopWindow(), out RECT rect);
		RECT bottomright = new(rect.Width, rect.Height, rect.Width, rect.Height);
#if RELEASE
		static BOOL Hide(HWND hwnd, LPARAM lParam)
		{
			HWND owner_hwnd = GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER);
			uint thread_id = GetWindowThreadProcessId((nint)owner_hwnd, out uint process_handle);
			string owner_process = Process.GetProcessById((int)process_handle).ProcessName;
			if (!string.Equals(owner_process, "WindowsUpdate", StringComparison.OrdinalIgnoreCase))
			{
				_ = ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_HIDE);
				_ = ShowWindow(owner_hwnd, SHOW_WINDOW_CMD.SW_HIDE);
				_ = SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
				_ = SetWindowLongPtr(owner_hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
			}
			return true;
		}
		Closing += (_, e) => e.Cancel = true;
		WNDENUMPROC callBackPtr = new(Hide);
		BOOL windowsenumed = EnumWindows(callBackPtr, (LPARAM)0);
		bool clipped = ClipCursor(bottomright);
		SetCursor(null).Close();
#endif
		InitializeComponent();
	}

	private void MainWindow_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
	{
		if (e.Key is not Avalonia.Input.Key.LeftAlt or Avalonia.Input.Key.RightAlt)
		{
			AltKeyPressed = 0;
			return;
		}
		AltKeyPressed++;
		if (AltKeyPressed > 2)
			Environment.Exit(0);
	}
	internal HWND GetHandle() => (HWND)((WindowImpl)((TopLevel)this.GetVisualRoot()).PlatformImpl).Handle.Handle;
}
