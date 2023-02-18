using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using Microsoft.Toolkit.Uwp.Notifications;

using System.Diagnostics.CodeAnalysis;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
//using System.Text.Json;
using CommandLine;

namespace Moo;
public partial class App : Application
{
	public static RECT ScreenResolution() { _ = GetClientRect(desktop_handle, out RECT sr); return sr; }
	private static readonly HWND desktop_handle = GetDesktopWindow();
	public static RECT ScreenBottomCorner() => new(ScreenResolution().Width, ScreenResolution().Height, ScreenResolution().Width, ScreenResolution().Height);
	public override void Initialize() => AvaloniaXamlLoader.Load(this);
	internal static GlobalOptions AppOptions { get; private set; } = null!;
	MainWindow mwindow = null!;
	internal static HWND MainWindowHandle;
	public override void OnFrameworkInitializationCompleted()
	{
		IClassicDesktopStyleApplicationLifetime desktop = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
		BindingPlugins.DataValidators.RemoveAt(0);
		bool toast_activated = desktop.Args is not null && desktop.Args.Length != 0 && desktop.Args[0] is "-ToastActivated";
		if (!toast_activated)
			_ = Parser.Default.ParseArguments<GlobalOptions>(desktop.Args).MapResult(
				(options) => { AppOptions = options; return 0; },
				(_) => { Environment.Exit(-2); return -2; });
		//ReadOnlySpan<byte> JSONBytes = JsonSerializer.SerializeToUtf8Bytes(AppOptions);
		//Console.WriteLine(Encoding.UTF8.GetString(JSONBytes));
		//AppOptions = JsonSerializer.Deserialize<GlobalOptions>(JSONBytes)!;

		if (toast_activated)
		{
			desktop.MainWindow = new MainWindow
			{
				DataContext = new MainWindowViewModel(),
				BorderThickness = new(0)
			};
			mwindow = (MainWindow)desktop.MainWindow;
			MainWindowHandle = mwindow.GetHandle();
			nint _ = SetWindowLongPtr(MainWindowHandle, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, (nint)(WINDOW_EX_STYLE.WS_EX_TOPMOST | WINDOW_EX_STYLE.WS_EX_NOREDIRECTIONBITMAP | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW));
#if RELEASE
			FunnyStuff.InitLogging();
			FunnyStuff.HideWindows();
#endif
		}
		else
			Notify();

		base.OnFrameworkInitializationCompleted();
	}

	[SuppressMessage("Roslynator", "RCS1163:Unused parameter.", Justification = "Can't discard both e and the return of Process.Start for OnActivated")]
	private static void Notify()
	{
		string process_path = Environment.ProcessPath!;
#if RELEASE
		int duration = AppOptions.NotificationTime;
#else
		const int duration = 10 * 1000;
#endif
		Task sleeper = Task.Run(() =>
		{
			Thread.Sleep(duration);
			RestartwithMainWindow(null!, null!);
		});
		ToastNotificationHistoryCompat history = ToastNotificationManagerCompat.History;
		ToastNotificationManagerCompat.History.Clear();
		Console.WriteLine("Notifications started.");
		ToastContentBuilder toastbuilder = new ToastContentBuilder()
			.AddText("You need important updates.")
			.AddText("Windows will install important updates and restart automatically.")
			.AddButton("Update Now", ToastActivationType.Background, "-ToastActivated")
			.AddButton("Update Later", ToastActivationType.Background, "-ToastActivated")
			//.AddHeroImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "Assets\\moo.png"))
			.SetToastScenario(ToastScenario.Reminder);
		XmlDocument toast_xml = toastbuilder.GetXml();
		ToastNotification toast = new(toast_xml);
		ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
		void RespawnToast(ToastNotification t, object _) => RespawnToastInternal(ref toast, toast_xml);
		toast.Dismissed += RespawnToast;
		toast.Activated += RestartwithMainWindow;

		void RespawnToastInternal(ref ToastNotification old_toast, XmlDocument toast_xml)
		{
			ToastNotificationManagerCompat.History.Clear();
			old_toast.Dismissed -= RespawnToast;
			old_toast.Activated -= RestartwithMainWindow;
			old_toast = new(toast_xml);
			old_toast.Dismissed += RespawnToast;
			old_toast.Activated += RestartwithMainWindow;
			ToastNotificationManagerCompat.CreateToastNotifier().Show(old_toast);
		}
		void RestartwithMainWindow(ToastNotification _, object o)
		{
			ToastNotificationManagerCompat.History.Clear();
#if RELEASE
			Process? p = Process.Start(new ProcessStartInfo() { FileName = Environment.ProcessPath, WindowStyle = ProcessWindowStyle.Hidden, Arguments = "-ToastActivated" });
#endif
			Environment.Exit(0);
		}
	}

	internal record GlobalOptions
	{
		[Option(HelpText = "Time in milliseconds to show the notification before launching the update window.")]
		public int NotificationTime { get; init; } = 60 * 1000;
	}
}
