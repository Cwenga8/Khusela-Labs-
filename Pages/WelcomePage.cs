namespace HerMentor;

public sealed class WelcomePage : AppPage
{
	// Only restore a saved session when the app starts. Navigation helpers pop back to this page,
	// and redirecting every time it appears would override where the user actually asked to go.
	private static bool _startupChecked;

	protected override void OnAppearing()
	{
		base.OnAppearing();
		if (_startupChecked)
			return;

		_startupChecked = true;
		_ = RedirectIfSignedInAsync();
	}

	private static async Task RedirectIfSignedInAsync()
	{
		await Backend.Auth.EnsureLoadedAsync();
		if (Backend.Auth.IsSignedIn)
			MainThread.BeginInvokeOnMainThread(() => GoRoot(HomeScreen));
	}

	protected override View BuildContent()
	{
		var s = new VerticalStackLayout { Spacing = 14, Padding = new Thickness(24, 40, 24, 28) };
		s.Add(new Image
		{
			Source = Dark ? "hermentor_logo_dark.png" : "hermentor_logo.png",
			HeightRequest = 170,
			WidthRequest = 170,
			Aspect = Aspect.AspectFit,
			HorizontalOptions = LayoutOptions.Center
		});
		s.Add(new Label { Text = "KHUSELA LABS", FontSize = 9, CharacterSpacing = 2, TextColor = Pink, FontFamily = "OpenSansSemibold", HorizontalTextAlignment = TextAlignment.Center });
		s.Add(new BoxView { HeightRequest = 6, Color = Colors.Transparent });
		s.Add(Card(T("AI-powered career guidance", 12), 12));
		s.Add(Card(T("Matched with mentors who fit your goals", 12), 12));
		s.Add(Card(T("Structured sessions and progress tracking", 12), 12));
		s.Add(new BoxView { HeightRequest = 6, Color = Colors.Transparent });
		s.Add(GradButton("Get started", () => Go(AppScreen.SignUp)));
		s.Add(OutlineButton("I already have an account", () => Go(AppScreen.Login), Line));
		return s;
	}
}
