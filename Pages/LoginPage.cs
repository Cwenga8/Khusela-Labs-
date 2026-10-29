namespace HerMentor;

public sealed class LoginPage : AppPage
{
	private bool _busy;

	protected override View BuildContent()
	{
		var s = Begin("Log in to HerMentor", "Your next step in the conversation story", back: true);
		var email = AddField(s, "Email", "name@email.com");
		var password = AddField(s, "Password", "Enter your password", true);
		var message = T("", 12, Pink);
		s.Add(message);
		s.Add(new Label { Text = "Forgot password?", FontSize = 11, TextColor = Pink, HorizontalOptions = LayoutOptions.End });

		async Task SubmitAsync()
		{
			if (_busy)
				return;

			var mail = email.Text?.Trim() ?? "";
			if (mail.Length == 0 || string.IsNullOrEmpty(password.Text))
			{
				message.TextColor = Pink;
				message.Text = "Enter your email and password.";
				return;
			}

			_busy = true;
			message.TextColor = Muted;
			message.Text = "Logging in...";
			try
			{
				await Backend.Auth.LoginAsync(mail, password.Text);
				await GoRoot(HomeScreen);
			}
			catch (Exception ex)
			{
				message.TextColor = Pink;
				message.Text = ErrorText(ex);
			}
			finally
			{
				_busy = false;
			}
		}

		s.Add(GradButton("Log in", () => _ = SubmitAsync()));
		s.Add(T("or continue with", 11, Muted, false, true));
		s.Add(OutlineButton("Continue with Google", () =>
		{
			message.TextColor = Muted;
			message.Text = "Google sign-in isn't available yet. Please use email.";
		}, Line));
		s.Add(Tap(T("New to HerMentor? Sign up", 11, Pink, false, true), () => Go(AppScreen.SignUp)));
		return s;
	}
}
