using Microsoft.Extensions.DependencyInjection;

namespace HerMentor;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		UserAppTheme = Preferences.Get("theme", "dark") == "light" ? AppTheme.Light : AppTheme.Dark;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}