namespace HerMentor;

public sealed class SignUpPage : AppPage
{
	private const string WantMentor = "I want a mentor";
	private const string WantToMentor = "I want to mentor";

	private string _role = "mentee";
	private bool _busy;

	protected override View BuildContent()
	{
		var s = Begin("Create your account", "Join as a mentee or a mentor", back: true);
		s.Add(ChipRow([WantMentor, WantToMentor], false, [_role == "mentor" ? 1 : 0],
			choice => _role = choice == WantToMentor ? "mentor" : "mentee"));
		var name = AddField(s, "Full name", "Your name");
		var email = AddField(s, "Email", "you@email.com");
		var password = AddField(s, "Password", "At least 8 characters", true);
		var confirm = AddField(s, "Confirm password", "Repeat password", true);

		var message = T("", 12, Pink);
		s.Add(message);

		async Task SubmitAsync()
		{
			if (_busy)
				return;

			var fullName = name.Text?.Trim() ?? "";
			var mail = email.Text?.Trim() ?? "";
			var pass = password.Text ?? "";

			if (fullName.Length < 2) { message.TextColor = Pink; message.Text = "Enter your name."; return; }
			if (!mail.Contains('@') || !mail.Contains('.')) { message.TextColor = Pink; message.Text = "Enter a valid email address."; return; }
			if (pass.Length < 8) { message.TextColor = Pink; message.Text = "Password must be at least 8 characters."; return; }
			if (pass != confirm.Text) { message.TextColor = Pink; message.Text = "Passwords don't match."; return; }

			_busy = true;
			message.TextColor = Muted;
			message.Text = "Creating your account...";
			try
			{
				await Backend.Auth.RegisterAsync(fullName, mail, pass, _role);
				GoRoot(_role == "mentor" ? AppScreen.MentorDashboard : AppScreen.ProfileSetup);
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

		s.Add(GradButton("Continue", () => _ = SubmitAsync()));
		s.Add(T("or continue with", 11, Muted, false, true));
		s.Add(OutlineButton("Continue with Google", () =>
		{
			message.TextColor = Muted;
			message.Text = "Google sign-in isn't available yet. Please use email.";
		}, Line));
		s.Add(Tap(T("Already have an account? Log in", 11, Pink, false, true), () => Go(AppScreen.Login)));
		return s;
	}
}
