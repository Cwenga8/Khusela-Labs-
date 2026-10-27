namespace HerMentor;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute("sign-up", typeof(SignUpPage));
		Routing.RegisterRoute("login", typeof(LoginPage));
		Routing.RegisterRoute("profile-setup", typeof(ProfileSetupPage));
		Routing.RegisterRoute("home", typeof(HomePage));
		Routing.RegisterRoute("career", typeof(CareerGuidancePage));
		Routing.RegisterRoute("skill-gap", typeof(SkillGapPage));
		Routing.RegisterRoute("mentors", typeof(FindMentorsPage));
		Routing.RegisterRoute("mentor-profile", typeof(MentorProfilePage));
		Routing.RegisterRoute("book-session", typeof(BookSessionPage));
		Routing.RegisterRoute("session-prep", typeof(SessionPrepPage));
		Routing.RegisterRoute("messages", typeof(MessagesPage));
		Routing.RegisterRoute("progress", typeof(ProgressPage));
		Routing.RegisterRoute("sessions", typeof(SessionOutcomesPage));
		Routing.RegisterRoute("mentor-dashboard", typeof(MentorDashboardPage));
		Routing.RegisterRoute("profile", typeof(ProfileSettingsPage));
	}
}
