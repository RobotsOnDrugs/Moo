using System.Drawing;

Rectangle rect = new(0, 0, 500, 500);
Thread.Sleep(2000);

//BufferedGraphicsManager.Current
//EnumWindows((hwnd, lParam) =>
//{
//	return true;
//}, 0);

Gdi32.SafeHDC desktop = GetDC();
using Graphics g = Graphics.FromHdc(desktop.DangerousGetHandle());

g.Clear(Color.Transparent);
for (int i = 0; i < 500; i++)
{
	g.FillRectangle(Brushes.PeachPuff, rect);
	Thread.Sleep(10);
}
g.Clear(Color.Transparent);
_ = ReleaseDC(nint.Zero, desktop);
Thread.Sleep(2000);

Environment.Exit(0);