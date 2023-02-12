using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using Microsoft.Toolkit.Uwp.Notifications;

using System.Diagnostics.CodeAnalysis;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

namespace Moo;
public partial class App : Application
{
	public static RECT ScreenResolution() { _ = GetClientRect(desktop_handle, out RECT sr); return sr; }
	private static readonly HWND desktop_handle = GetDesktopWindow();
	public static RECT ScreenBottomCorner() => new(ScreenResolution().Width, ScreenResolution().Height, ScreenResolution().Width, ScreenResolution().Height);
	public override void Initialize() => AvaloniaXamlLoader.Load(this);
	MainWindow mwindow = null!;
	public override void OnFrameworkInitializationCompleted()
	{
		IClassicDesktopStyleApplicationLifetime desktop = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
		BindingPlugins.DataValidators.RemoveAt(0);
		if (desktop.Args is not null && desktop.Args.Length != 0 && desktop.Args[0] is "-ToastActivated")
		{
#if RELEASE
			FunnyStuff.MessWithWindows();
#endif
			desktop.MainWindow = new MainWindow
			{
				DataContext = new MainWindowViewModel(),
				BorderThickness = new(0)
			};
			mwindow = (MainWindow)desktop.MainWindow;
			nint _ = SetWindowLongPtr(mwindow.GetHandle(), WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)(WINDOW_EX_STYLE.WS_EX_TOPMOST | WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW));
	}
		else
			Notify();
		base.OnFrameworkInitializationCompleted();
	}

	[SuppressMessage("Roslynator", "RCS1163:Unused parameter.", Justification = "Can't discard both e and the return of Process.Start for OnActivated")]
	private static void Notify()
	{
#if RELEASE
		const int duration = 60 * 1000;
#else
		const int duration = 10 * 1000;
#endif
		Task sleeper = Task.Run(() => Thread.Sleep(duration));
		ToastNotificationHistoryCompat history = ToastNotificationManagerCompat.History;
		ToastNotificationManagerCompat.History.Clear();
		Console.WriteLine("Notifications started.");
		ToastContentBuilder toastbuilder = new ToastContentBuilder()
			.AddText("You need important updates.")
			.AddText("Windows will install important updates and restart automatically.")
			.AddButton("Update Now", ToastActivationType.Background, "-ToastActivated")
			.AddButton("Update Later", ToastActivationType.Background, "-ToastActivated")
			.SetToastScenario(ToastScenario.Alarm);
		XmlDocument toast_xml = toastbuilder.GetXml();
		ToastNotification toast = new(toast_xml);
		ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
		void RespawnToast(ToastNotification t, object _) => RespawnToastInternal(ref toast, toast_xml);
		toast.Dismissed += RespawnToast;
		toast.Activated += RestartwithMainWindow;

		void RespawnToastInternal(ref ToastNotification old_toast, XmlDocument toast_xml)
		{
			Console.WriteLine("test");
			ToastNotificationManagerCompat.History.Clear();
			old_toast = new(toast_xml);
			old_toast.Dismissed -= RespawnToast;
			old_toast.Dismissed += RespawnToast;
			toast.Activated -= RestartwithMainWindow;
			toast.Activated += RestartwithMainWindow;
			ToastNotificationManagerCompat.CreateToastNotifier().Show(old_toast);
		}
		void RestartwithMainWindow(ToastNotification _, object o)
		{
			ToastNotificationManagerCompat.History.Clear();
			Process? p = Process.Start(new ProcessStartInfo() { FileName = Environment.ProcessPath, WindowStyle = ProcessWindowStyle.Hidden, Arguments = "-ToastActivated" });
			Environment.Exit(0);
		}
	}
}
