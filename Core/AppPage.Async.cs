namespace HerMentor;

public abstract partial class AppPage
{
	/// <summary>Turns any failure into a message that is safe to show to the user.</summary>
	protected static string ErrorText(Exception ex) =>
		ex is ApiException ? ex.Message : "Something went wrong. Please try again.";

	/// <summary>Signs out and sends the user to the log-in screen (used when the server rejects the token).</summary>
	protected static void SessionExpired()
	{
		Backend.Auth.Logout();
		GoRoot(AppScreen.Login);
	}
}
