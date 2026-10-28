namespace HerMentor;

public abstract partial class AppPage
{
	protected static string Route(AppScreen s) => s switch
	{
		AppScreen.SignUp => "sign-up",
		AppScreen.Login => "login",
		AppScreen.ProfileSetup => "profile-setup",
		AppScreen.Home => "home",
		AppScreen.CareerGuidance => "career",
		AppScreen.SkillGap => "skill-gap",
		AppScreen.FindMentors => "mentors",
		AppScreen.MentorProfile => "mentor-profile",
		AppScreen.BookSession => "book-session",
		AppScreen.SessionPrep => "session-prep",
		AppScreen.Messages => "messages",
		AppScreen.Progress => "progress",
		AppScreen.SessionOutcomes => "sessions",
		AppScreen.MentorDashboard => "mentor-dashboard",
		AppScreen.ProfileSettings => "profile",
		_ => "welcome"
	};

	protected static bool IsMentorUser => Backend.Auth.CurrentUser?.Role == "mentor";

	/// <summary>Where "home" is for the signed-in user: mentors land on their dashboard.</summary>
	protected static AppScreen HomeScreen => IsMentorUser ? AppScreen.MentorDashboard : AppScreen.Home;

	protected static void Go(AppScreen s) => _ = Shell.Current.GoToAsync(Route(s));

	protected static void GoBack() => _ = Shell.Current.GoToAsync("..");

	protected static void GoRoot(AppScreen s) => _ = RootAsync(s);

	private static async Task RootAsync(AppScreen s)
	{
		await Shell.Current.Navigation.PopToRootAsync();
		if (s != AppScreen.Welcome)
			await Shell.Current.GoToAsync(Route(s));
	}
}
