using System;
using System.Linq;

namespace Aster;

internal class HelpTopic
{
	public string Title;

	public string Tags;

	public string Body;

	public string Page;

	public static HelpTopic[] All = new HelpTopic[10]
	{
		new HelpTopic
		{
			Title = "My PC feels slow",
			Tags = "slow lag cpu memory performance",
			Page = "Processes",
			Body = "Start with a measurement\r\n\r\n1. Open Processes and sort CPU or Memory.\r\n2. Observe while the slowdown happens.\r\n3. Save work before closing an app you recognize.\r\n4. Capture a baseline in Insights, make one change, then compare.\r\n\r\nHigh usage is not automatically a fault. Aster does not identify the root cause from utilization alone."
		},
		new HelpTopic
		{
			Title = "An update or Windows feature fails",
			Tags = "update repair dism sfc corruption",
			Page = "Windows advisor",
			Body = "Collect the error first\r\n\r\nRecord the exact error and the operation that failed. Search Error-code help in Windows advisor. Start with read-only diagnosis. A reviewed repair plan is available when the evidence supports it.\r\n\r\nPlans request administrator access and may take time. They stop on failure or a restart-required result. Aster never restarts Windows automatically."
		},
		new HelpTopic
		{
			Title = "Get started with local AI",
			Tags = "ai ollama model setup assistant",
			Page = "Windows advisor",
			Body = "Your optional AI counselor\r\n\r\nInstall Ollama and download a local model, then leave its service running. Open Windows advisor → AI counselor → Find local models. Select a model, review your question and click Ask.\r\n\r\nNo model is bundled. Offline guidance uses programmed rules. Including a diagnostic report is optional. Model answers cannot execute commands."
		},
		new HelpTopic
		{
			Title = "Prepare a useful support request",
			Tags = "support share report specs export",
			Page = "Hardware report",
			Body = "Give support useful evidence\r\n\r\nWrite the symptom, exact error, timing, reproduction steps and recent changes. Generate a hardware report, review it, then copy or save it. Hardware identifiers are excluded by the report filter.\r\n\r\nYou can also use the support-handoff template in the AI prompt studio. Nothing is sent to another person by Aster."
		},
		new HelpTopic
		{
			Title = "Keep monitoring lightweight",
			Tags = "battery eco efficiency overhead",
			Page = "Preferences",
			Body = "Choose a comfortable update rate\r\n\r\nPreferences offers one-, two-, three- and five-second sampling. Longer intervals reduce monitoring work. History charts retain 60 samples, so their time span depends on the interval.\r\n\r\nAura Eco lowers wallpaper animation settings; you can restore its prior profile. These controls tune Aster and Aura, not Windows security or services."
		},
		new HelpTopic
		{
			Title = "Some sensor values are unavailable",
			Tags = "gpu temperature sensors missing unavailable",
			Page = "GPU",
			Body = "Unavailable is an honest result\r\n\r\nWindows providers, permissions and vendor support determine which readings exist. Supported NVIDIA devices can provide temperature, clocks and power. Aster does not estimate missing CPU temperatures or fabricate sensor readings.\r\n\r\nTry refreshing the relevant hardware page. A blank reading does not by itself mean the hardware has failed."
		},
		new HelpTopic
		{
			Title = "Compare a change fairly",
			Tags = "baseline before after optimization compare",
			Page = "Insights",
			Body = "Change one thing at a time\r\n\r\nUse the same workload, capture a baseline, make one reversible change, and compare again. CPU and memory readings naturally fluctuate. Repeat your original task to see whether the symptom actually improved.\r\n\r\nInsights can prepare its readings for the AI counselor, but you still choose whether to send them."
		},
		new HelpTopic
		{
			Title = "Set up live wallpapers",
			Tags = "aura wallpaper video screen battery",
			Page = "Wallpaper studio",
			Body = "Preview before applying\r\n\r\nOpen Aura from Wallpaper studio. Choose a built-in scene or import a local image/video. Configure FPS, battery/fullscreen pause and sound before applying.\r\n\r\nDesktop attachment depends on Windows Explorer compatibility. If unavailable, Aura reports the limitation. Video playback depends on installed Windows codecs."
		},
		new HelpTopic
		{
			Title = "Find your saved work",
			Tags = "cases notes logs history saved",
			Page = "Windows advisor",
			Body = "Local records\r\n\r\nSaved cases stores your symptom, displayed report, last AI question/answer and notes only when you click Save. Restore marks the report as historical. Run a fresh scan for current readings.\r\n\r\nRecovery logs and existing cases remain in the legacy Pulse data folder for compatibility with earlier releases. They are plain local files, not cloud backups."
		},
		new HelpTopic
		{
			Title = "Make the workspace less distracting",
			Tags = "focus clean shortcuts pins navigation",
			Page = "Focus",
			Body = "Keep the essentials\r\n\r\nFocus shows CPU, memory, process count and uptime, plus recent sampled ranges. Toggle distraction-free mode there or press F11 to hide/show the sidebar. Ctrl+K still opens page navigation.\r\n\r\nPin frequent pages with the header star. Search the sidebar to find a page. Preferences saves your accent and monitoring interval."
		}
	};

	public override string ToString()
	{
		return Title;
	}

	public static HelpTopic[] Search(string query)
	{
		string[] words = query.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		return All.Where((HelpTopic t) => words.All((string w) => (t.Title + " " + t.Tags + " " + t.Body).IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
	}
}
