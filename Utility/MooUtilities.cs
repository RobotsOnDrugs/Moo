using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Moo.Utilities;

public static partial class MooUtilities
{
	private static readonly Dictionary<uint, HashSet<HWND>> WindowHandles = new();
	[LibraryImport("user32.dll")]
	public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

	public static void Main(string[] args)
	{
		WNDENUMPROC window_info_callback = new(GetWindowInfo);
		BOOL windowsenumed = EnumWindows(window_info_callback, (LPARAM)0);
		foreach (uint pid in WindowHandles.Keys)
		{
			Process process = Process.GetProcessById((int)pid);
			string maintitle = process.MainWindowTitle.Length != 0 ? process.MainWindowTitle : "<None>";
			string description = "<unknown> ";
			try
			{
				string? _description = $"{FileVersionInfo.GetVersionInfo(process.MainModule?.FileName ?? string.Empty).FileDescription}";
				description = _description is not null ? $"({_description}) " : "";
			}
			catch (System.ComponentModel.Win32Exception) { }
			Console.WriteLine($"{process.ProcessName}: {description}[{maintitle}] ->");
		}
		Thread.Sleep(10000);
	}

	private static BOOL GetWindowInfo(HWND hwnd, LPARAM _)
	{
		HWND owner_hwnd = GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER);
		uint owner_thread_id = GetWindowThreadProcessId((nint)owner_hwnd, out uint owner_id);
		Process owner_process = Process.GetProcessById((int)owner_id);
		int thread_id = (int)GetWindowThreadProcessId((nint)hwnd, out uint id);
		//Process process = Process.GetProcessById((int)id);
		//if (!owner_process.MainWindowTitle.Contains("teamviewer", StringComparison.OrdinalIgnoreCase))
		//	return false;
		if (WindowHandles.TryGetValue(id, out HashSet<HWND>? children))
			return children.Add(hwnd);
		WindowHandles[id] = new HashSet<HWND>() { hwnd };
		return true;
	}
}
