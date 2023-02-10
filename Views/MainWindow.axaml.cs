using System;
using System.Threading.Tasks;

using Avalonia.Input;
using Avalonia.Threading;

namespace Moo.Views;

public partial class MainWindow : Window
{
	private int AltKeyPressed = 0;
	partial void InitializeComponent();
	public MainWindow()
	{
		KeyUp += ExitWithAlt;
#if RELEASE
		Closing += (_, e) => e.Cancel = true;
#endif
		InitializeComponent();
		Dispatcher.UIThread.InvokeAsync(MakeProgress);
	}

	private void ExitWithAlt(object? sender, KeyEventArgs e)
	{
		if (e.Key is not Key.LeftAlt or Key.RightAlt)
		{
			AltKeyPressed = 0;
			return;
		}
		AltKeyPressed++;
		if (AltKeyPressed > 2)
			Environment.Exit(0);
	}
	internal async Task MakeProgress()
	{
		int progress_percentage = 0;
		TextBlock ptb = this.FindControl<TextBlock>("ProgressIndicator")!;
		Random rand = new();
		while (progress_percentage < 90)
		{
			await Task.Delay(rand.Next(500, 10000));
			int progress_increment = rand.Next(1, 5);
			int max_progress = progress_percentage + progress_increment;
			while (progress_percentage < max_progress)
			{
				await Task.Delay(rand.Next(100, 500));
				progress_percentage++;
				ptb.Text = $"{progress_percentage}% complete";
			}
		}
		while (true)
		{
			await Task.Delay(rand.Next(5000, 15000));
			if (progress_percentage == 99)
				progress_percentage -= rand.Next(1, 8);
			else
				progress_percentage++;
			ptb.Text = $"{progress_percentage}% complete";
		}
	}
	public HWND GetHandle() => (HWND)PlatformImpl!.Handle.Handle;
}
