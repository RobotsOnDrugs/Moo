using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Moo.Utilities;

public static partial class MooUtilities
{
	private static readonly Dictionary<uint, HashSet<HWND>> WindowHandles = new();
	[LibraryImport("user32.dll")]
	public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

	public static void Main()
	{
		_ = EnumWindows(GetWindowInfo, 0);
		foreach (uint pid in WindowHandles.Keys)
		{
			Process process = Process.GetProcessById((int)pid);
			string maintitle = process.MainWindowTitle.Length != 0 ? process.MainWindowTitle : "<None>";
			string description = "<unknown> ";
			try
			{
				string _description = $"{FileVersionInfo.GetVersionInfo(process.MainModule?.FileName ?? string.Empty).FileDescription}";
				description = _description != string.Empty ? $"({_description}) " : "";
			}
			catch (System.ComponentModel.Win32Exception) { }
			Console.WriteLine($"{process.ProcessName}: {description}[{maintitle}] ->");
		}
		Thread.Sleep(10000);
	}

	private static BOOL GetWindowInfo(HWND hwnd, LPARAM _)
	{
		// HWND owner_hwnd = GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER);
		// uint owner_thread_id = GetWindowThreadProcessId(owner_hwnd, out uint owner_id);
		// Process owner_process = Process.GetProcessById((int)owner_id);
		int thread_id = (int)GetWindowThreadProcessId(hwnd, out uint id);
		if (WindowHandles.TryGetValue(id, out HashSet<HWND>? children))
			return children.Add(hwnd);
		WindowHandles[id] = new HashSet<HWND>() { hwnd };
		return true;
	}
}
