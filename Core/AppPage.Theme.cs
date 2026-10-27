namespace HerMentor;

public abstract partial class AppPage
{
	protected static bool Dark => Application.Current?.RequestedTheme != AppTheme.Light;

	protected static Color Bg => Dark ? Color.FromArgb("#0B0A10") : Color.FromArgb("#F8F6FA");
	protected static Color Surface => Dark ? Color.FromArgb("#15121B") : Colors.White;
	protected static Color Line => Dark ? Color.FromArgb("#2B2535") : Color.FromArgb("#E6DFEC");
	protected static Color Ink => Dark ? Colors.White : Color.FromArgb("#251C2B");
	protected static Color Muted => Dark ? Color.FromArgb("#A39BAF") : Color.FromArgb("#6F667B");
	protected static Color Pink => Color.FromArgb("#E0559F");
	protected static Color Purple => Color.FromArgb("#8B4FD8");
	protected static Color PillBg => Dark ? Color.FromArgb("#34204F") : Color.FromArgb("#EFE3FB");
	protected static Color PillInk => Dark ? Color.FromArgb("#D9B8FF") : Color.FromArgb("#6A2FB0");
	protected static Color Placeholder => Dark ? Color.FromArgb("#6F687B") : Color.FromArgb("#9A92A6");

	protected static void SetTheme(string name)
	{
		var light = name == "Light";
		Preferences.Set("theme", light ? "light" : "dark");
		if (Application.Current is { } app)
			app.UserAppTheme = light ? AppTheme.Light : AppTheme.Dark;
	}
}
