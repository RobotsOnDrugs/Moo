namespace Moo;
public static class HWNDExtensions
{
	public static Process? GetWindowProcess(this HWND hwnd)
	{
		uint thread_id = InternalPInvoke.GetWindowThreadProcessId((nint)hwnd, out uint id);
		return Process.GetProcessById((int)id);
	}
}
