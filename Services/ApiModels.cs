using System.Text.Json;

namespace HerMentor;

public static class ApiConfig
{
	// 10.0.2.2 is the host machine as seen from the Android emulator.
	public static string BaseUrl =>
		Preferences.Get("api_url", DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5080" : "http://localhost:5080");

	public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
}

public record UserDto(int Id, string Name, string Email, string Role);
public record AuthResponse(string Token, UserDto User);
public record ApiError(string Error);

public record MenteeProfileDto(string Education, List<string> Skills, List<string> Interests, string Goal, string Availability);

public record CareerPathDto(string Id, string Name, int Fit, List<string> Have, List<string> Missing);
public record CareerPathsResponse(bool NeedsProfile, List<CareerPathDto> Paths);

public record ChatTurn(string Role, string Content);

public record GapSkillDto(string Name, bool Have, string Step);
public record RoleOptionDto(string Id, string Name, int Fit);
public record SkillGapResponse(
	bool NeedsProfile,
	string RoleId,
	string RoleName,
	int Coverage,
	List<GapSkillDto> Skills,
	List<string> NextSteps,
	List<RoleOptionDto> Roles,
	string SuggestedCategory);

public record AgendaItemDto(string Item, int Minutes);
public record PrepResponse(string Goal, string MentorName, List<string> Questions, List<AgendaItemDto> Agenda, bool AiGenerated);

public record SessionActionDto(int Id, string Text, bool Done);
public record SessionDto(
	int Id,
	int MentorId,
	string MentorName,
	string MentorInitials,
	string MenteeName,
	bool IsMentorView,
	DateTime StartsAt,
	string Format,
	string Topic,
	string Goal,
	string Status,
	string Summary,
	string MentorFeedback,
	List<SessionActionDto> Actions,
	PrepResponse? Prep)
{
	public string OtherName => IsMentorView ? MenteeName : MentorName;
	public bool IsBooked => Status == "Booked";
	public bool IsPending => Status == "Pending";
	public bool IsUpcoming => IsBooked || IsPending;
	public bool IsCompleted => Status == "Completed";
	public bool IsCancelled => Status == "Cancelled";
}

public record MentorDto(
	int Id,
	string Name,
	string Initials,
	string Title,
	string Category,
	string Bio,
	List<string> Tags,
	int Years,
	double Rating,
	bool IsAvailable,
	string Availability,
	int Match)
{
	public Mentor ToMentor() => new(Name, Initials, Title, Match, Category, [.. Tags], Id, Bio, Years, Rating, IsAvailable, Availability);
}
