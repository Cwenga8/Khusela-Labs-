namespace HerMentor;

public sealed class HomePage : AppPage
{
	private VerticalStackLayout? _body;
	private CancellationTokenSource? _cts;

	protected override AppScreen? ActiveTab => AppScreen.Home;

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_cts?.Cancel();
	}

	protected override View BuildContent()
	{
		var s = Begin($"Welcome back, {UserName}", "Your next step in the conversation story");
		_body = new VerticalStackLayout { Spacing = 12 };
		s.Add(_body);
		_ = LoadAsync();
		return s;
	}

	private async Task LoadAsync()
	{
		var body = _body;
		if (body is null)
			return;

		_cts?.Cancel();
		var cts = _cts = new CancellationTokenSource();
		body.Children.Clear();
		body.Add(T("Loading your dashboard...", 12, Muted));

		try
		{
			var sessions = Backend.Sessions.ListAsync(cts.Token);
			var profile = Backend.Profile.GetAsync(cts.Token);
			var gap = Backend.Career.GetGapAsync(null, cts.Token);
			var mentors = Backend.Mentors.SearchAsync(null, null, cts.Token);
			await Task.WhenAll(sessions, profile, gap, mentors);

			if (cts.IsCancellationRequested || body != _body)
				return;

			body.Children.Clear();
			Render(body, sessions.Result, profile.Result, gap.Result, mentors.Result);
		}
		catch (OperationCanceledException)
		{
			// Page closed or reloaded.
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
		}
		catch (Exception ex) when (!cts.IsCancellationRequested && body == _body)
		{
			body.Children.Clear();
			body.Add(T(ErrorText(ex), 12, Pink));
			body.Add(OutlineButton("Try again", () => _ = LoadAsync(), Line, 38));
		}
	}

	private static void Render(
		VerticalStackLayout body,
		List<SessionDto> sessions,
		MenteeProfileDto profile,
		SkillGapResponse gap,
		List<MentorDto> mentors)
	{
		var cutoff = DateTime.UtcNow.AddHours(-1);
		var next = sessions.Where(x => x.IsUpcoming && x.StartsAt >= cutoff).OrderBy(x => x.StartsAt).FirstOrDefault();

		var nextCard = new VerticalStackLayout { Spacing = 6 };
		if (next is null)
		{
			nextCard.Add(T("NEXT SESSION", 9, Pink, true));
			nextCard.Add(T("No session booked yet", 16, Ink, true));
			nextCard.Add(T("Find a mentor and book a conversation.", 11, Muted));
			nextCard.Add(GradButton("Find a mentor", () => GoRoot(AppScreen.FindMentors), 40));
		}
		else
		{
			nextCard.Add(T(next.IsPending ? "NEXT SESSION · AWAITING MENTOR" : "NEXT SESSION", 9, Pink, true));
			nextCard.Add(T(FormatWhen(next.StartsAt), 16, Ink, true));
			nextCard.Add(T($"{next.Topic} with {next.OtherName}", 11, Muted));
			nextCard.Add(GradButton(next.Prep is null ? "Prepare with AI" : "View my prep", () => SessionPrepPage.Open(next), 40));
		}
		body.Add(Card(nextCard, 14));

		var goal = new VerticalStackLayout { Spacing = 6 };
		if (gap.NeedsProfile)
		{
			goal.Add(T("Set your goal", 12, Ink, true));
			goal.Add(T("Add your skills and goal to track your progress.", 11, Muted));
			body.Add(Tap(Card(goal), () => Go(AppScreen.ProfileSetup)));
		}
		else
		{
			var title = string.IsNullOrWhiteSpace(profile.Goal) ? $"Goal: {gap.RoleName}" : $"Goal: {profile.Goal}";
			goal.Add(T(title, 12, Ink, true));
			goal.Add(Bar(gap.Coverage));
			goal.Add(T($"{gap.Coverage}% of the skills for {gap.RoleName}", 10, Muted));
			body.Add(Tap(Card(goal), () => Go(AppScreen.Progress)));

			var tip = new VerticalStackLayout { Spacing = 4 };
			tip.Add(T("AI TIP", 9, Pink, true));
			tip.Add(T(gap.NextSteps.FirstOrDefault() ?? "Keep going. You're on track.", 12, Ink));
			body.Add(Tap(Card(tip), () =>
			{
				SkillGapPage.SelectedRole = null;
				Go(AppScreen.SkillGap);
			}));
		}

		var top = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
		top.Add(Section("Top mentor matches"), 0, 0);
		top.Add(Tap(T("See all", 11, Pink), () => GoRoot(AppScreen.FindMentors)), 1, 0);
		body.Add(top);

		if (mentors.Count == 0)
			body.Add(T("No mentors available right now.", 12, Muted));
		else
			foreach (var mentor in mentors.Take(2))
				body.Add(MentorCard(mentor.ToMentor()));
	}
}
