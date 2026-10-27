namespace HerMentor;

public sealed class ProfileSetupPage : AppPage
{
	private static readonly string[] SkillOptions = ["React", "Node.js", "Figma", "Cloud", "SQL", "Python", "DevOps"];
	private static readonly string[] InterestOptions = ["Full-stack", "UX design", "Cloud", "AI / Data", "Leadership", "Career switch", "Product"];

	private readonly HashSet<string> _skills = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _interests = new(StringComparer.OrdinalIgnoreCase);
	private MenteeProfileDto? _loaded;
	private bool _busy;

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_ = LoadAsync();
	}

	// Pre-fill the form with whatever is already saved, then redraw once.
	private async Task LoadAsync()
	{
		if (_loaded is not null)
			return;

		try
		{
			_loaded = await Backend.Profile.GetAsync();
			_skills.UnionWith(_loaded.Skills);
			_interests.UnionWith(_loaded.Interests);
			Refresh();
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
		}
		catch
		{
			// Offline: the form still works for a fresh profile.
		}
	}

	protected override View BuildContent()
	{
		var s = Begin("Build your profile", "Tell us about you so we can match you with the right mentors", back: true);
		var education = AddField(s, "Education", "BSc Software Development, Year 3", text: _loaded?.Education);

		s.Add(Section("Skills you have"));
		s.Add(ChipRow(SkillOptions, true, Selected(SkillOptions, _skills), Toggle(_skills)));
		s.Add(Section("Career interests"));
		s.Add(ChipRow(InterestOptions, true, Selected(InterestOptions, _interests), Toggle(_interests)));

		s.Add(Section("What do you want from a mentor?"));
		var goal = AddField(s, "Main goal", "Land my first tech role", text: _loaded?.Goal);
		var availability = AddField(s, "Availability", "Weekday evenings", text: _loaded?.Availability);

		var message = T("", 12, Pink);
		s.Add(message);

		async Task SaveAsync()
		{
			if (_busy)
				return;

			_busy = true;
			message.TextColor = Muted;
			message.Text = "Saving...";
			try
			{
				_loaded = await Backend.Profile.SaveAsync(new MenteeProfileDto(
					education.Text?.Trim() ?? "",
					[.. _skills],
					[.. _interests],
					goal.Text?.Trim() ?? "",
					availability.Text?.Trim() ?? ""));
				GoRoot(AppScreen.FindMentors);
			}
			catch (ApiException ex) when (ex.IsUnauthorized)
			{
				SessionExpired();
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

		s.Add(GradButton("Save and find matches", () => _ = SaveAsync()));
		return s;
	}

	private static int[] Selected(string[] options, HashSet<string> chosen) =>
		[.. options.Select((o, i) => (o, i)).Where(x => chosen.Contains(x.o)).Select(x => x.i)];

	private static Action<string> Toggle(HashSet<string> set) =>
		item =>
		{
			if (!set.Remove(item))
				set.Add(item);
		};
}
