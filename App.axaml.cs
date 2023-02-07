using System;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using static Windows.Win32.PInvoke;

using Windows.Win32.UI.WindowsAndMessaging;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using System.Linq;

namespace AvaloniaSandbox;
public partial class App : Application
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public override void OnFrameworkInitializationCompleted()
	{
		MainWindow mwindow = null!;
		ToastNotificationManagerCompat.History.Clear();
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			if (desktop.Args.Length != 0 && desktop.Args[0] is "-ToastActivated")
			{
				foreach (Process proc in Process.GetProcessesByName("WindowsUpdate").Where(p => p.Id != Environment.ProcessId))
					proc.Kill();
				desktop.MainWindow = new MainWindow();
				mwindow = (MainWindow)desktop.MainWindow;
				_ = SetWindowLongPtr(mwindow.GetHandle(), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)(WINDOW_EX_STYLE.WS_EX_TOPMOST | WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW));
			}
			else
				Notify();
		}
		base.OnFrameworkInitializationCompleted();
#if DEBUG
		_ = Task.Run(() => { Thread.Sleep(10000); Environment.Exit(0); });
#endif
	}
	private static void Notify()
	{
		Console.WriteLine("Notifications started.");
		ToastContentBuilder toastbuilder = new ToastContentBuilder()
			.AddText("You need important updates.")
			.AddText("Windows will install important updates and restart automatically.")
			.SetBackgroundActivation()
			.SetToastScenario(ToastScenario.Reminder);
		toastbuilder.Show();

		ToastNotificationManagerCompat.OnActivated += (e) =>
		{
			ProcessStartInfo startInfo = new() { FileName = Environment.ProcessPath, WindowStyle = ProcessWindowStyle.Hidden, Arguments = "-ToastActivated" };
			_ = Process.Start(startInfo);
			Environment.Exit(0);
		};
#if RELEASE
		const int duration = 60 * 1000;
#else
		const int duration = 10 * 1000;
#endif
		Thread.Sleep(duration);
		ProcessStartInfo startInfo = new() { FileName = Environment.ProcessPath, WindowStyle = ProcessWindowStyle.Hidden, Arguments = "-ToastActivated" };
		_ = Process.Start(startInfo);
		Environment.Exit(0);
	}
}
