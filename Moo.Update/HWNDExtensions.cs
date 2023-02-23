namespace Moo.Update;
public static class HWNDExtensions
{
	public static Process GetWindowProcess(this HWND hwnd)
	{
		uint thread_id = Vanara.PInvoke.User32.GetWindowThreadProcessId((nint)hwnd, out uint id);
		return Process.GetProcessById((int)id);
	}
}
