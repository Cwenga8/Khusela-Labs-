namespace HerMentor;

public sealed record Mentor(
	string Name,
	string Initials,
	string Role,
	int Match,
	string Category,
	string[] Tags,
	int Id = 0,
	string Bio = "",
	int Years = 0,
	double Rating = 0,
	bool IsAvailable = true,
	string Availability = "");

public enum AppScreen
{
	Welcome,
	SignUp,
	Login,
	ProfileSetup,
	Home,
	CareerGuidance,
	SkillGap,
	FindMentors,
	MentorProfile,
	BookSession,
	SessionPrep,
	Messages,
	Progress,
	SessionOutcomes,
	MentorDashboard,
	ProfileSettings
}

public abstract partial class AppPage
{
	protected static readonly Mentor[] Mentors =
	[
		new("Naledi Mokoena", "NM", "Senior Engineer, Thutuka", 94, "Tech", ["Full-stack", "Career switch"]),
		new("Amara Dlamini", "AD", "Product Designer, Orbit", 88, "Design", ["UX", "Portfolio"]),
		new("Lerato Pillay", "LP", "Data Lead, Insight Co", 81, "Data", ["AI / Data", "Python"])
	];

	private static Mentor? _selectedMentor;

	protected static string UserName =>
		Backend.Auth.CurrentUser?.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "there";

	protected static Mentor CurrentMentor
	{
		get => _selectedMentor ?? Mentors[0];
		set => _selectedMentor = value;
	}
}
