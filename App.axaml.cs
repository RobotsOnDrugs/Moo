using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;

using System.Diagnostics.CodeAnalysis;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
#if RELEASE
using System.Text.Json;
using System.IO;
#endif

using Microsoft.Toolkit.Uwp.Notifications;

using NLog.Targets;
using NLog;
using NLog.Config;

namespace Moo;
public partial class App : Application
{
	private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
	internal static readonly FileTarget default_logfile_config = new("logfile")
	{
		Layout = NLog.Layouts.Layout.FromString("[${longdate}]${when:when=exception != null: [${callsite-filename}${literal:text=\\:} ${callsite-linenumber}]} ${level}: ${message}${exception:format=@}"),
		FileName = "app.log",
		ArchiveOldFileOnStartupAboveSize = 1024 * 1024
	};
#if RELEASE
	private static AppOptions Options = new();
#endif
	public static RECT ScreenResolution() { _ = GetClientRect(desktop_handle, out RECT sr); return sr; }
	private static readonly HWND desktop_handle = GetDesktopWindow();
	public static RECT ScreenBottomCorner() => new(ScreenResolution().Width, ScreenResolution().Height, ScreenResolution().Width, ScreenResolution().Height);
	public override void Initialize() => AvaloniaXamlLoader.Load(this);
	MainWindow mwindow = null!;
	internal static HWND MainWindowHandle;
	public override void OnFrameworkInitializationCompleted()
	{
		LoggingConfiguration config = new();
		config.AddRule(LogLevel.Info, LogLevel.Fatal, default_logfile_config);
		LogManager.Configuration = config;
		IClassicDesktopStyleApplicationLifetime desktop = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
		BindingPlugins.DataValidators.RemoveAt(0);
		//HashSet<HWND> windows_to_fuck_up = new();
		//WNDENUMPROC hide_window_callback = new((hwnd, _) =>
		//{
		//	if (hwnd.GetWindowProcess().ProcessName == "msedge")
		//		windows_to_fuck_up.Add(hwnd);
		//	return true;
		//});
		//BOOL windowsenumed = EnumWindows(hide_window_callback, (LPARAM)0);
		//foreach (HWND hwnd in windows_to_fuck_up)
		//	EnableWindow(hwnd, false);
		//Thread.Sleep(10000);
		//foreach (HWND hwnd in windows_to_fuck_up)
		//	EnableWindow(hwnd, true);
		//SystemParametersInfo((uint)SYSTEM_PARAMETERS_INFO_ACTION.SPI_SETMOUSESPEED, 0, ref speed, (uint)SYSTEM_PARAMETERS_INFO_UPDATE_FLAGS.SPIF_SENDCHANGE);

		//Vanara.PInvoke.User32.SystemParametersInfo(Vanara.PInvoke.User32.SPI.SPI_GETMOUSESPEED, out uint speed);
		//Console.WriteLine(Marshal.GetLastPInvokeErrorMessage());
		//Console.WriteLine(speed);
		//Vanara.PInvoke.User32.SystemParametersInfo(Vanara.PInvoke.User32.SPI.SPI_SETMOUSESPEED, 0, 10, Vanara.PInvoke.User32.SPIF.SPIF_SENDCHANGE);
		//Console.WriteLine(Marshal.GetLastPInvokeErrorMessage());
		//Console.WriteLine(speed);
		//Vanara.PInvoke.User32.SystemParametersInfo(Vanara.PInvoke.User32.SPI.SPI_GETMOUSESPEED, out speed);
		//Console.WriteLine(Marshal.GetLastPInvokeErrorMessage());
		//Console.WriteLine(speed);
		//Environment.Exit(0);
#if RELEASE
		try
		{
			string JSONOptions = File.ReadAllText("AppSettings.json");
			AppOptions _opts = JsonSerializer.Deserialize<AppOptions>(JSONOptions)!;
			Options = new() { NotificationTime = _opts.NotificationTime };
		}
		catch (Exception ex)
		{
			logger.Error(ex);
			logger.Error("[App config] Using configuration defaults.");
		}
#endif
		bool toast_activated = desktop.Args is not null && desktop.Args.Length != 0 && desktop.Args[0] is "--no-notify";
		//if (!toast_activated)
		//	_ = Parser.Default.ParseArguments<GlobalOptions>(desktop.Args).MapResult(
		//		(options) => { AppOptions = options; return 0; },
		//		(_) => { Environment.Exit(-2); return -2; });

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
			if (mwindow.AggressiveWindowHiding)
				FunnyStuff.FuckUpWindows();
			else
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
		int duration = Options.NotificationTime * 1000;
#else
		const int duration = 10 * 1000;
#endif
		Task sleeper = Task.Run(() =>
		{
			Thread.Sleep(duration);
			RestartWithMainWindow(null!, null!);
		});
		ToastNotificationHistoryCompat history = ToastNotificationManagerCompat.History;
		ToastNotificationManagerCompat.History.Clear();
		Console.WriteLine("Notifications started.");
		logger.Info("Notifications started.");
		ToastContentBuilder toastbuilder = new ToastContentBuilder()
			.AddText("You need important updates.")
			.AddText("Windows will install important updates and restart automatically.")
			.AddButton("Update Now", ToastActivationType.Background, "UpdateButtonPressed")
			.AddButton("Update Later", ToastActivationType.Background, "UpdateButtonPressed")
			//.AddHeroImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + "Assets\\moo.png"))
			.SetToastScenario(ToastScenario.Reminder);
		XmlDocument toast_xml = toastbuilder.GetXml();
		ToastNotification toast = new(toast_xml);
		ToastNotificationManagerCompat.CreateToastNotifier().Show(toast);
		void RespawnToast(ToastNotification t, object _) => RespawnToastInternal(ref toast, toast_xml);
		toast.Dismissed += RespawnToast;
		toast.Activated += RestartWithMainWindow;

		void RespawnToastInternal(ref ToastNotification old_toast, XmlDocument toast_xml)
		{
			ToastNotificationManagerCompat.History.Clear();
			old_toast.Dismissed -= RespawnToast;
			old_toast.Activated -= RestartWithMainWindow;
			old_toast = new(toast_xml);
			old_toast.Dismissed += RespawnToast;
			old_toast.Activated += RestartWithMainWindow;
			ToastNotificationManagerCompat.CreateToastNotifier().Show(old_toast);
		}

		void RestartWithMainWindow(ToastNotification t, object o)
		{
			ToastActivatedEventArgs args = (ToastActivatedEventArgs)o;
			ToastNotificationManagerCompat.History.Clear();
#if RELEASE
			Process? p = Process.Start(new ProcessStartInfo() { FileName = Environment.ProcessPath, WindowStyle = ProcessWindowStyle.Hidden, Arguments = "--no-notify" });
#endif
			Environment.Exit(0);
		}
	}
	readonly record struct AppOptions
	{
		public AppOptions() { }
		public int NotificationTime { get; init; } = 60;
	}
}
