using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using NLog;
using NLog.Config;
using NLog.Targets;

using WindowsDesktop;

namespace Moo;
internal static partial class FunnyStuff
{
	internal static readonly FileTarget default_logfile_config = new("logfile")
	{
		Layout = NLog.Layouts.Layout.FromString("[${longdate}]${when:when=exception != null: [${callsite-filename}${literal:text=\\:} ${callsite-linenumber}]} ${level}: ${message}${exception:format=@}"),
		FileName = "funnystuff.log",
		ArchiveOldFileOnStartupAboveSize = 1024 * 1024
	};
	public static Dictionary<HWND, Process> HiddenWindows { get; private set; } = new();
	public static VirtualDesktop? NewDesktop { get; private set; } = null;
	private static readonly string ThisProcessName = Process.GetProcessById(Environment.ProcessId).ProcessName;
	//"uv_x64", "UltraViewer_Desktop", 
	private static readonly HashSet<string> whitelist = new() { ThisProcessName, "TeamViewer", "TeamViewer_desktop", "Anydesk", "AnyDesk", "Idle", "WindowsTerminal", "conhost", "powershell", "devenv" };
	private static readonly HashSet<string> killlist = new() { "WinStore.App", "explorer", "SystemSettings" };
	private static ConcurrentDictionary<HWND, WINDOW_EX_STYLE> PreviousWindowStates = new();
	private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
	private static readonly LoggingConfiguration config = new();
	public static void InitLogging()
	{
		LogManager.Configuration = config;
		config.AddRule(LogLevel.Info, LogLevel.Fatal, default_logfile_config);
	}
	public static void FuckUpWindows()
	{
		foreach (string dead in killlist)
			foreach (Process proc in Process.GetProcessesByName(dead))
				proc.Kill();
		WNDENUMPROC hide_window_callback = new(FuckUpWindow);
		BOOL windowsenumed = EnumWindows(hide_window_callback, (LPARAM)0);
		bool clipped = ClipCursor(App.ScreenBottomCorner());
		_ = SetCursor(null);
		PreviousWindowStates = new();
	}
	private static BOOL FuckUpWindow(HWND hwnd, LPARAM lParam)
	{
		uint owner_thread_id;
		Process owner_process;
		uint thread_id;
		Process process;
		uint owner_id;
		try
		{
			owner_thread_id = InternalPInvoke.GetWindowThreadProcessId((nint)hwnd, out owner_id);
			owner_process = Process.GetProcessById((int)owner_id);
			thread_id = InternalPInvoke.GetWindowThreadProcessId((nint)hwnd, out uint id);
			process = Process.GetProcessById((int)id);
		}
		catch (ArgumentException)
		{
			return false;
		}
		WINDOW_EX_STYLE owner_old_style = (WINDOW_EX_STYLE)GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
		WINDOW_EX_STYLE old_style = (WINDOW_EX_STYLE)GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
		if (Marshal.GetLastPInvokeError() is not 0)
		{
			logger.Info($"[Hide] [Get Style] [{Marshal.GetLastPInvokeError()}] {Marshal.GetLastPInvokeErrorMessage()}");
			LogManager.Flush();
			return false;
		}
		try
		{
			if (!PreviousWindowStates.ContainsKey(hwnd) && !whitelist.Contains(owner_process.ProcessName) && (Environment.ProcessId != owner_id))
			{
				_ = PreviousWindowStates.TryAdd(hwnd, old_style);
				logger.Info($"[Hide] Hiding {owner_process.ProcessName} old style: {old_style}");
				_ = ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_HIDE);
				bool window_pos_was_set = SetWindowStyle(hwnd, WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
				logger.Info($"Window position was {(window_pos_was_set ? "" : "not ")}set");
				if (Marshal.GetLastPInvokeError() is not 0)
				{
					logger.Info($"[Hide] [Set Style] [{Marshal.GetLastPInvokeError()}] {Marshal.GetLastPInvokeErrorMessage()}");
					LogManager.Flush();
					return false;
				}
			}
		}
		catch (Exception ex) { logger.Fatal(ex); throw; }
		finally { LogManager.Flush(); }
		return true;
	}
	public static void RestoreToolWindows()
	{
		lock (PreviousWindowStates)
			try
			{
				foreach (KeyValuePair<HWND, WINDOW_EX_STYLE> window_style in PreviousWindowStates)
				{
					HWND hwnd = window_style.Key;
					WINDOW_EX_STYLE old_style = window_style.Value;
					WINDOW_EX_STYLE current_style = (WINDOW_EX_STYLE)GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
					if (current_style == old_style)
						continue;
					Process process = GetProcessFromWindow(hwnd);
					logger.Info($"[Restore] -> {process.ProcessName}");
					logger.Info($"[Restore] old style: {old_style}");
					bool should_show_child = old_style is not WINDOW_EX_STYLE.WS_EX_LTRREADING && (old_style != (old_style | WINDOW_EX_STYLE.WS_EX_NOACTIVATE));
					bool was_set = SetWindowStyle(hwnd, window_style.Value);
					if (should_show_child)
					{
						logger.Info($"[Restore] Showing {process.ProcessName} window");
						_ = ShowWindow(hwnd, SHOW_WINDOW_CMD.SW_SHOW);
					}
					logger.Info($"[Restore] was restored to style: {old_style}");
					if (Marshal.GetLastPInvokeError() is not 0)
						logger.Info($"[Restore] [{Marshal.GetLastPInvokeError()}] {Marshal.GetLastPInvokeErrorMessage()}");
					LogManager.Flush();
				}
			}
		catch (Exception ex)
		{
			logger.Error(ex + "\r\n");
			LogManager.Flush();
			LogManager.Shutdown();
			throw;
		}
	}
	private static BOOL SetWindowStyle(HWND hwnd, WINDOW_EX_STYLE style)
	{
		nint old_style = SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)style);
		if (Marshal.GetLastPInvokeError() is not 0)
			_ = SetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, old_style);
		if (Marshal.GetLastPInvokeError() is not 0)
		{
			logger.Info($"[Window Fuckery] Couldn't set window style: [{Marshal.GetLastPInvokeError()}] {Marshal.GetLastPInvokeErrorMessage()}");
			return false;
		}
		WINDOWINFO pwi;
		unsafe
		{
			pwi = new() { cbSize = (uint)sizeof(WINDOWINFO) };
			_ = GetWindowInfo(hwnd, ref pwi);
		}
		RECT window_pos = pwi.rcWindow;
		_ = SetWindowPos(
		hwnd, (HWND)1,
		window_pos.X, window_pos.Y, window_pos.Width, window_pos.Height,
		SET_WINDOW_POS_FLAGS.SWP_NOMOVE | SET_WINDOW_POS_FLAGS.SWP_NOSIZE | SET_WINDOW_POS_FLAGS.SWP_NOZORDER | SET_WINDOW_POS_FLAGS.SWP_FRAMECHANGED);
		if (Marshal.GetLastPInvokeError() is not 0)
		{
			logger.Info($"[Window Fuckery] Couldn't set window position: [{Marshal.GetLastPInvokeError()}] {Marshal.GetLastPInvokeErrorMessage()}");
			return false;
		}
		return true;
	}
	public static void HideWindows()
	{
		VirtualDesktop new_desktop = VirtualDesktop.Create();
		foreach (HWND hwnd in GetFilteredWindows())
			VirtualDesktop.MoveToDesktop((nint)hwnd, new_desktop);
		NewDesktop = new_desktop;
	}
	public static void UnhideWindows()
	{
		foreach (KeyValuePair<HWND, Process> kvp in HiddenWindows)
			VirtualDesktop.MoveToDesktop((nint)kvp.Key, VirtualDesktop.Current);
		NewDesktop?.Remove();
		NewDesktop = null;
	}
	private static HashSet<HWND> GetFilteredWindows()
	{
		HashSet<HWND> FilteredWindows = new();
		BOOL Filter(HWND hwnd, LPARAM _)
		{
			VirtualDesktop? window_desktop;
			bool added = false;
			Process process = GetProcessFromWindow(hwnd);
			if (process.Id == Environment.ProcessId)
				return true;
			nint handle_ptr = (nint)hwnd;
			if (GetClientRect(hwnd, out RECT rect) && rect.Size.Width > 1 && rect.Size.Height > 1)
			{
				window_desktop = VirtualDesktop.FromHwnd(handle_ptr);
				if (window_desktop is not null)
					added = FilteredWindows.Add(hwnd);
			}
			return true;
		}
		WNDENUMPROC enum_window_callback = new(Filter);
		BOOL windowsenumed = EnumWindows(enum_window_callback, (LPARAM)0);
		return FilteredWindows;
	}
	private static Process GetProcessFromWindow(HWND hwnd)
	{
		uint thread_id = InternalPInvoke.GetWindowThreadProcessId((nint)hwnd, out uint id);
		return Process.GetProcessById((int)id);
	}
}
