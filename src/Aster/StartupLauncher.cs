using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Aster;

internal static class StartupLauncher
{
	public static int Duration(int value)
	{
		if (value != 1600 && value != 4200)
		{
			return 2800;
		}
		return value;
	}

	public static string Style(string value)
	{
		if (!(value == "Warp") && !(value == "Aurora"))
		{
			return "Orbit";
		}
		return value;
	}

	public static void Run()
	{
		WorkspaceOptions workspaceOptions = WorkspaceOptions.Read();
		bool flag = workspaceOptions.StartupEnabled && workspaceOptions.Motion;
		if (flag)
		{
			using StartupScreen startupScreen = new StartupScreen(workspaceOptions);
			if (startupScreen.ShowDialog() != DialogResult.OK)
			{
				return;
			}
		}
		MainWindow mainWindow = new MainWindow();
		if (flag)
		{
			AttachFade(mainWindow);
		}
		Application.Run(mainWindow);
	}

	public static void AttachFade(Form main)
	{
		Timer fade = new Timer
		{
			Interval = 16
		};
		Stopwatch elapsed = new Stopwatch();
		main.Opacity = 0.0;
		main.Shown += delegate
		{
			elapsed.Start();
			fade.Start();
		};
		fade.Tick += delegate
		{
			main.Opacity = Math.Min(1.0, elapsed.Elapsed.TotalMilliseconds / 240.0);
			if (main.Opacity >= 1.0)
			{
				fade.Stop();
			}
		};
		main.FormClosed += delegate
		{
			fade.Dispose();
		};
	}
}
