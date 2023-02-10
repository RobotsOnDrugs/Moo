using System.Threading;
using System;

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using CommunityToolkit.WinUI.Notifications;

using Moo.ViewModels;
using Moo.Views;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Moo;
public partial class App : Application
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);
	MainWindow mwindow = null!;
	public override void OnFrameworkInitializationCompleted()
	{
		IClassicDesktopStyleApplicationLifetime desktop = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
		BindingPlugins.DataValidators.RemoveAt(0);
		if (desktop.Args is not null && desktop.Args.Length != 0 && desktop.Args[0] is "-ToastActivated")
		{
			desktop.MainWindow = new MainWindow
			{
				DataContext = new MainWindowViewModel(),
				BorderThickness = new(0)
			};
			mwindow = (MainWindow)desktop.MainWindow;
			_ = SetWindowLongPtr(mwindow.GetHandle(), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)(WINDOW_EX_STYLE.WS_EX_TOPMOST | WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW));
#if RELEASE
			FunnyStuff.MessWithWindows(mwindow.GetHandle());
#endif
		}
		else
			Notify();
		base.OnFrameworkInitializationCompleted();
	}

	[SuppressMessage("Roslynator", "RCS1163:Unused parameter.", Justification = "Can't discard both e and the return of Process.Start for OnActivated")]
	private static void Notify()
	{
		ToastNotificationManagerCompat.History.Clear();
		Console.WriteLine("Notifications started.");
		ToastContentBuilder toastbuilder = new ToastContentBuilder()
			.AddText("You need important updates.")
			.AddText("Windows will install important updates and restart automatically.")
			.SetBackgroundActivation()
			.SetToastScenario(ToastScenario.Reminder);
		toastbuilder.Show();

		ToastNotificationManagerCompat.OnActivated += (e) =>
		{
			_ = Process.Start(new ProcessStartInfo() { FileName = Environment.ProcessPath, WindowStyle = ProcessWindowStyle.Hidden, Arguments = "-ToastActivated" });
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
