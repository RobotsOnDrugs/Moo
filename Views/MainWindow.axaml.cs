using Windows.Win32.System.Shutdown;

using Avalonia.Input;
using Avalonia.Threading;
using NLog;
using NLog.Targets;
using NLog.Config;
using System.Runtime.InteropServices;

namespace Moo.Views;

public partial class MainWindow : Window
{
	private int AltKeyPressed = 0;
	private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
	internal static readonly FileTarget default_logfile_config = new("logfile")
	{
		Layout = NLog.Layouts.Layout.FromString("[${longdate}]${when:when=exception != null: [${callsite-filename}${literal:text=\\:} ${callsite-linenumber}]} ${level}: ${message}${exception:format=@}"),
		FileName = "mainwindow.log",
		ArchiveOldFileOnStartupAboveSize = 1024 * 1024
	};
	public MainWindow()
	{
		LoggingConfiguration config = new();
		config.AddRule(LogLevel.Info, LogLevel.Fatal, default_logfile_config);
		LogManager.Configuration = config;
		KeyUp += ExitWithAlt;
#if RELEASE
		LostFocus += (_, _) => Persist();
		PointerCaptureLost += (_, _) => Persist();
		Activated += (_, _) => Persist();
		Closing += (_, e) => e.Cancel = true;
		Closed += (_, _) => Activate();
#endif
		InitializeComponent();
		Dispatcher.UIThread.InvokeAsync(MakeProgress);
	}

	private void ExitWithAlt(object? sender, KeyEventArgs e) => ExitWithAltInternal(e);
	private void ExitWithAltInternal(KeyEventArgs e)
	{
		if (e.Key is not Key.LeftAlt or Key.RightAlt)
		{
			AltKeyPressed = 0;
			return;
		}
		AltKeyPressed++;
		if (AltKeyPressed > 2)
		{
			KeyUp -= ExitWithAlt;
			try
			{
				logger.Info("Calling UnhideWindows");
				FunnyStuff.UnhideWindows();
			}
			catch (Exception ex) { logger.Error(ex); }
			LogManager.Flush();
			LogManager.Shutdown();
			Environment.Exit(0);
		}
	}
	internal async Task MakeProgress()
	{
		int progress_percentage = 0;
		AvaloniaProgressRing.ProgressRing progress_ring = this.FindControl<AvaloniaProgressRing.ProgressRing>("ProgressRing")!;
		TextBlock stage_tb = this.FindControl<TextBlock>("Stage")!;
		TextBlock turnoff_tb = this.FindControl<TextBlock>("TurnOff")!;
		TextBlock progress_tb = this.FindControl<TextBlock>("ProgressIndicator")!;
		stage_tb.Text = "Please wait updates are installing";
		turnoff_tb.Text = "Do not turn off your computer";
		Random rand = new();
		async Task ProgressScript()
		{
			progress_tb.Text = "0% complete";
			while (progress_percentage < 90)
			{
#if RELEASE
				await Task.Delay(rand.Next(2 * 60 * 1000, 5 * 60 * 1000));
#else
				await Task.Delay(1 * 100);
#endif
				int progress_increment = rand.Next(1, 3);
				int max_progress = progress_percentage + progress_increment;
				while (progress_percentage < max_progress)
				{
					await Task.Delay(rand.Next(100, 500));
					progress_percentage++;
					progress_tb.Text = $"{progress_percentage}% complete";
				}
			}
			while (true)
			{
#if RELEASE
				await Task.Delay(rand.Next(60 * 1000, 3 * 60 * 1000));
#else
				await Task.Delay(1 * 1000);
#endif
				if (progress_percentage == 99)
				{
					if (rand.Next(10) > 5)
						break;
					progress_percentage -= rand.Next(1, 8);
				}
				progress_percentage++;
				progress_tb.Text = $"{progress_percentage}% complete";
			}
		}
		await ProgressScript();
		stage_tb.Text = "There was an unexpected error, rolling back";
		await Task.Delay(5 * 1000);
		progress_percentage = 0;
		progress_tb.Text = $"{progress_percentage}% complete";
		await ProgressScript();
		progress_ring.IsActive = false;
		progress_tb.Text = "";
		stage_tb.Text = "Critical error rolling back updates";
		turnoff_tb.Text = "The system must restart to retry";
		await Task.Delay(10000);
		LogManager.Flush();
		// Combine EWX_REBOOT (0x2) or EWX_RESTARTAPPS (0x40) with EWX_FORCE (0x4), which is not in the EXIT_WINDOWS_FLAGS enum
		if (!ExitWindowsEx((EXIT_WINDOWS_FLAGS)0x00000006, SHUTDOWN_REASON.SHTDN_REASON_MAJOR_OTHER) && !ExitWindowsEx((EXIT_WINDOWS_FLAGS)0x00000044, SHUTDOWN_REASON.SHTDN_REASON_MAJOR_OTHER))
		{
			logger.Info(Marshal.GetLastPInvokeErrorMessage());
			LogManager.Flush();
			LogManager.Shutdown();
			await Task.Delay(2 * 60 * 1000);
			Environment.FailFast("Update failed: unknown error");
		}
	}
#if RELEASE
	private void Persist()
	{
		_ = SetWindowPos(GetHandle(), (HWND)(-1), 0, 0, App.ScreenResolution().Width, App.ScreenResolution().Height, SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW);
		_ = ClipCursor(App.ScreenBottomCorner());
		_ = SetCursor(null);
	}
#endif
	public HWND GetHandle() => (HWND)PlatformImpl!.Handle.Handle;
}
