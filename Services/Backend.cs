using Microsoft.Extensions.DependencyInjection;

namespace HerMentor;

/// <summary>Access to the app's backend services from pages (which Shell creates without constructor injection).</summary>
public static class Backend
{
	public static void Register(IServiceCollection services)
	{
		services.AddSingleton<ApiClient>();
		services.AddSingleton<AuthService>();
		services.AddSingleton<MentorService>();
		services.AddSingleton<ProfileService>();
		services.AddSingleton<CareerService>();
		services.AddSingleton<PrepService>();
		services.AddSingleton<SessionService>();
		services.AddSingleton<MessageService>();
		services.AddSingleton<AssistantService>();
		services.AddSingleton<RealtimeService>();
	}

	public static AuthService Auth => Get<AuthService>();
	public static MentorService Mentors => Get<MentorService>();
	public static ProfileService Profile => Get<ProfileService>();
	public static CareerService Career => Get<CareerService>();
	public static PrepService Prep => Get<PrepService>();
	public static SessionService Sessions => Get<SessionService>();
	public static MessageService Messages => Get<MessageService>();
	public static AssistantService Assistant => Get<AssistantService>();
	public static RealtimeService Realtime => Get<RealtimeService>();

	private static T Get<T>() where T : notnull =>
		IPlatformApplication.Current!.Services.GetRequiredService<T>();
}
