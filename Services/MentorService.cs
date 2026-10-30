using System.Text.Json;

namespace HerMentor;

public sealed class MentorService(ApiClient api, AuthService auth)
{
	private string? CacheKey =>
		auth.CurrentUser is { Id: > 0 } user ? $"hermentor.mentor-profile.{user.Id}" : null;

	public Task<List<MentorDto>> SearchAsync(string? query, string? category, CancellationToken ct = default) =>
		api.GetAsync<List<MentorDto>>(
			$"api/mentors?query={Uri.EscapeDataString(query ?? "")}&category={Uri.EscapeDataString(category ?? "")}", ct);

	public async Task<MentorDto> GetMineAsync(CancellationToken ct = default)
	{
		try
		{
			var mentor = await api.GetAsync<MentorDto>("api/mentors/me", ct);
			Cache(mentor);
			return mentor;
		}
		catch (ApiException ex) when (ex.StatusCode == 405)
		{
			var cached = GetCachedMine();
			if (cached is not null)
				return cached;

			throw;
		}
	}

	public async Task<MentorDto> SaveMineAsync(string title, string category, string bio, List<string> tags, int years, bool isAvailable, string availability, CancellationToken ct = default)
	{
		var mentor = await api.PutAsync<MentorDto>(
			"api/mentors/me",
			new { title, category, bio, tags, years, isAvailable, availability },
			ct);
		Cache(mentor);
		return mentor;
	}

	public MentorDto? GetCachedMine()
	{
		var key = CacheKey;
		if (key is null)
			return null;

		var json = Preferences.Get(key, "");
		if (json.Length == 0)
			return null;

		try
		{
			return JsonSerializer.Deserialize<MentorDto>(json, ApiConfig.Json);
		}
		catch (JsonException)
		{
			Preferences.Remove(key);
			return null;
		}
	}

	private void Cache(MentorDto mentor)
	{
		if (CacheKey is { } key)
			Preferences.Set(key, JsonSerializer.Serialize(mentor, ApiConfig.Json));
	}
}

public sealed class ProfileService(ApiClient api)
{
	public Task<MenteeProfileDto> GetAsync(CancellationToken ct = default) =>
		api.GetAsync<MenteeProfileDto>("api/profile", ct);

	public Task<MenteeProfileDto> SaveAsync(MenteeProfileDto profile, CancellationToken ct = default) =>
		api.PutAsync<MenteeProfileDto>("api/profile", profile, ct);
}

public sealed class CareerService(ApiClient api)
{
	public Task<CareerPathsResponse> GetPathsAsync(CancellationToken ct = default) =>
		api.GetAsync<CareerPathsResponse>("api/career/paths", ct);

	public Task<SkillGapResponse> GetGapAsync(string? roleId, CancellationToken ct = default) =>
		api.GetAsync<SkillGapResponse>($"api/career/gap?role={Uri.EscapeDataString(roleId ?? "")}", ct);
}

public sealed class SessionService(ApiClient api)
{
	public Task<List<SessionDto>> ListAsync(CancellationToken ct = default) =>
		api.GetAsync<List<SessionDto>>("api/sessions", ct);

	public Task<List<DateTime>> BusyAsync(int mentorId, CancellationToken ct = default) =>
		api.GetAsync<List<DateTime>>($"api/sessions/busy?mentorId={mentorId}", ct);

	public Task<SessionDto> BookAsync(int mentorId, DateTime startsAtUtc, string format, string topic, string goal, CancellationToken ct = default) =>
		api.PostAsync<SessionDto>("api/sessions", new { mentorId, startsAt = startsAtUtc, format, topic, goal }, ct);

	public Task<SessionDto> CancelAsync(int id, CancellationToken ct = default) =>
		api.PostAsync<SessionDto>($"api/sessions/{id}/cancel", new { }, ct);

	public Task<SessionDto> RespondAsync(int id, bool accept, CancellationToken ct = default) =>
		api.PostAsync<SessionDto>($"api/sessions/{id}/respond", new { accept }, ct);

	public Task<SessionDto> SavePrepAsync(int id, PrepResponse prep, CancellationToken ct = default) =>
		api.PutAsync<SessionDto>($"api/sessions/{id}/prep", new { prep }, ct);

	public Task<SessionDto> LogOutcomeAsync(int id, string summary, List<string> actions, string feedback, CancellationToken ct = default) =>
		api.PostAsync<SessionDto>($"api/sessions/{id}/outcome", new { summary, actions, feedback }, ct);

	public Task<SessionDto> ToggleActionAsync(int id, int actionId, CancellationToken ct = default) =>
		api.PostAsync<SessionDto>($"api/sessions/{id}/actions/{actionId}/toggle", new { }, ct);
}

public sealed class MessageService(ApiClient api)
{
	public Task<List<ChatMessageDto>> ListAsync(int sessionId, CancellationToken ct = default) =>
		api.GetAsync<List<ChatMessageDto>>($"api/sessions/{sessionId}/messages", ct);

	public Task<ChatMessageDto> SendAsync(int sessionId, string content, CancellationToken ct = default) =>
		api.PostAsync<ChatMessageDto>($"api/sessions/{sessionId}/messages", new { content }, ct);

	public Task<MessageDeletedDto> DeleteAsync(int sessionId, int messageId, CancellationToken ct = default) =>
		api.DeleteAsync<MessageDeletedDto>($"api/sessions/{sessionId}/messages/{messageId}", ct);

	public Task<ChatClearedDto> ClearAsync(int sessionId, CancellationToken ct = default) =>
		api.DeleteAsync<ChatClearedDto>($"api/sessions/{sessionId}/messages", ct);
}

public sealed class PrepService(ApiClient api)
{
	public Task<PrepResponse> GenerateAsync(int mentorId, string goal, CancellationToken ct = default) =>
		api.PostAsync<PrepResponse>("api/prep", new { mentorId = mentorId > 0 ? mentorId : (int?)null, goal }, ct);
}
