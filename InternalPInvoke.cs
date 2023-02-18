using System.Runtime.InteropServices;

namespace Moo;
internal static partial class InternalPInvoke
{
	[LibraryImport("user32.dll")]
	public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
