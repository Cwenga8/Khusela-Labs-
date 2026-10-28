namespace HerMentor;

public sealed class MentorService(ApiClient api)
{
	public Task<List<MentorDto>> SearchAsync(string? query, string? category, CancellationToken ct = default) =>
		api.GetAsync<List<MentorDto>>(
			$"api/mentors?query={Uri.EscapeDataString(query ?? "")}&category={Uri.EscapeDataString(category ?? "")}", ct);

	public Task<MentorDto> GetMineAsync(CancellationToken ct = default) =>
		api.GetAsync<MentorDto>("api/mentors/me", ct);

	public Task<MentorDto> SaveMineAsync(string title, string category, string bio, List<string> tags, int years, bool isAvailable, string availability, CancellationToken ct = default) =>
		api.PutAsync<MentorDto>("api/mentors/me", new { title, category, bio, tags, years, isAvailable, availability }, ct);
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

public sealed class PrepService(ApiClient api)
{
	public Task<PrepResponse> GenerateAsync(int mentorId, string goal, CancellationToken ct = default) =>
		api.PostAsync<PrepResponse>("api/prep", new { mentorId = mentorId > 0 ? mentorId : (int?)null, goal }, ct);
}
