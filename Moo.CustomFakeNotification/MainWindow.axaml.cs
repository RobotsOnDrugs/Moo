using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

using static Vanara.PInvoke.User32;
using Vanara.PInvoke;
using static Windows.Win32.PInvoke;
using Windows.Win32.UI.WindowsAndMessaging;
using System.Threading;

namespace Moo.CustomFakeNotification;
public partial class MainWindow : Window
{
	public MainWindow()
	{
		this.AttachDevTools();
		InitializeComponent();
		Button hello = this.Find<Button>("HelloButton");
		hello.Click += Hello_Click;
		Activated += MainWindow_Activated;
		PositionChanged += MainWindow_Activated;
		//DoubleTapped += MainWindow_Activated;
		//LostFocus += MainWindow_Activated;
		//PointerCaptureLost += MainWindow_Activated;
		_ = Dispatcher.UIThread.InvokeAsync(FlashButton);
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1163:Unused parameter.", Justification = "EnumMonitorProc signature")]
	private void MainWindow_Activated(object? sender, EventArgs e)
	{
		Thread.Sleep(100);
		HWND mwhandle = GetHandleVanara();
		HWND top_window = GetTopWindow(GetDesktopWindow());
		if (top_window != mwhandle)
		{
			//_ = SetWindowPos(top_window, (HWND)1, 0, 0, 0, 0, SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_HIDEWINDOW | SetWindowPosFlags.SWP_NOSIZE);
			Topmost = true;
		}
		int window_height = Convert.ToInt32(DesiredSize.Height);
		int window_width = Convert.ToInt32(DesiredSize.Width);
		const int margin = 10;
		Windows.Win32.Graphics.Gdi.MONITORINFO minfo = new() { cbSize = (uint)Marshal.SizeOf<Windows.Win32.Graphics.Gdi.MONITORINFO>() };
		HDC pmonitor_dc = GetWindowDC(GetHandleVanara());
		PRECT work_area = new();
		MONITORINFO pmi = new() { cbSize = (uint)Marshal.SizeOf<MONITORINFO>(), rcMonitor = new(1, 1, 1, 1) };
		bool monitor_proc(nint monitor, nint dc, PRECT size, nint _)
		{
			bool got_monitor = GetMonitorInfo((HMONITOR)monitor, ref pmi);
			if (pmi.dwFlags == MonitorInfoFlags.MONITORINFOF_PRIMARY)
				work_area = pmi.rcWork;
			return true;
		}
		_ = EnumDisplayMonitors(HDC.NULL, null, monitor_proc, 0);
		string error_message = Marshal.GetLastPInvokeErrorMessage();
		Windows.Win32.Foundation.HWND mwhandle2 = (Windows.Win32.Foundation.HWND)Process.GetProcessById(Environment.ProcessId).MainWindowHandle;
		nint new_ex_style = SetWindowLongPtr(mwhandle2, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)(WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_TOPMOST | WINDOW_EX_STYLE.WS_EX_COMPOSITED | WINDOW_EX_STYLE.WS_EX_LAYERED));
		_ = SetWindowPos(mwhandle, (HWND)(-1), work_area.right - (window_width + margin), work_area.bottom - (window_height + margin), window_width, window_height, SetWindowPosFlags.SWP_SHOWWINDOW);
		WINDOWPLACEMENT pwndpl = new();
		_ = GetWindowPlacement(GetHandleVanara(), ref pwndpl);
		RECT window_rect = pwndpl.rcNormalPosition;
		HRGN window_region = Gdi32.CreateRoundRectRgn(0, 0, 500, 300, 10, 10);
		_ = SetWindowRgn(GetHandleVanara(), window_region, true);
		error_message = Marshal.GetLastPInvokeErrorMessage();
		Topmost = true;
		//this.PositionChanged += MainWindow_Activated;
		error_message = Marshal.GetLastPInvokeErrorMessage();
		//HasBeenMoved = true;
	}

	private void Hello_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		nint _ = SetWindowLongPtr(GetHandleCsWin32(), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)WINDOW_EX_STYLE.WS_EX_APPWINDOW);
	}

	private async Task FlashButton()
	{
		Button hello = this.Find<Button>("HelloButton");
		for (int i = 0; i < 25; i++)
		{
			hello.Foreground = Brushes.White;
			await Task.Delay(100);
			hello.Foreground = Brushes.Black;
			await Task.Delay(100);
		}
	}
	internal HWND GetHandleVanara() => (HWND)PlatformImpl!.Handle.Handle;
	internal Windows.Win32.Foundation.HWND GetHandleCsWin32() => (Windows.Win32.Foundation.HWND)PlatformImpl!.Handle.Handle;
}
