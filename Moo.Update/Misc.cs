using System.Runtime.InteropServices;
using Windows.Win32.UI.Controls;

namespace Moo.Update;
internal static class Misc
{
	internal static BOOL GlassWindow(HWND hwnd, LPARAM _)
	{
		uint thread_id = Vanara.PInvoke.User32.GetWindowThreadProcessId((nint)hwnd, out uint id);
		Process process = Process.GetProcessById((int)id);
        MARGINS margins = new() { cxLeftWidth = -1, cxRightWidth = -1, cyBottomHeight = -1, cyTopHeight = -1 };
		WINDOW_EX_STYLE style = (WINDOW_EX_STYLE)GetWindowLongPtr(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
		if (string.Equals(process.ProcessName, "filezilla", StringComparison.OrdinalIgnoreCase))
		{
			BOOL success = GetClientRect(hwnd, out RECT rect);
			if (success && style is not WINDOW_EX_STYLE.WS_EX_TOOLWINDOW && rect.Size.Width is not 0 && rect.Size.Height is not 0)
			{
				Console.WriteLine($"{(nint)hwnd}: {style}");
				Console.WriteLine($"{(nint)hwnd}: {rect.Size.Width}x{rect.Size.Height}");
				if ((uint)DwmExtendFrameIntoClientArea(hwnd, margins) is not 0)
				{
					Console.WriteLine(Marshal.GetLastPInvokeErrorMessage());
					return false;
				}
				return true;
			}
		}
		return true;
	}
}
