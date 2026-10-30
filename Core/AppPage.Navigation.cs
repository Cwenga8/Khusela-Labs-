
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

    protected static bool IsMentorUser =>
        string.Equals(Backend.Auth.CurrentUser?.Role, "mentor", StringComparison.OrdinalIgnoreCase);

    // Returns the appropriate home screen for the signed-in user.
    // Mentors go to their dashboard; mentees go to Home.
    protected static AppScreen HomeScreen =>
        IsMentorUser ? AppScreen.MentorDashboard : AppScreen.Home;


    /// Navigate directly to the requested screen.
    protected static async Task Go(AppScreen s)
    {
        await Shell.Current.GoToAsync(Route(s));
    }

    /// Return to the previous screen.
    protected static async Task GoBack()
    {
        await Shell.Current.GoToAsync("..");
    }

    /// Navigate to a registered route and discard intermediate pages without displaying the root.
    protected static async Task GoRoot(AppScreen s)
    {
        var shell = Shell.Current;
        if (s == AppScreen.Welcome)
        {
            await shell.Navigation.PopToRootAsync();
            return;
        }

        await shell.GoToAsync(Route(s));

        var navigationStack = shell.Navigation.NavigationStack;
        for (var index = navigationStack.Count - 2; index > 0; index--)
            shell.Navigation.RemovePage(navigationStack[index]);
    }
}
