namespace HerMentor;

public sealed class MentorDashboardPage : AppPage
{
	private static readonly string[] Categories = ["Tech", "Design", "Data", "Leadership"];

	private VerticalStackLayout? _body;
	private CancellationTokenSource? _cts;
	private List<SessionDto>? _sessions;
	private MentorDto? _mentor;
	private bool _editing;
	private bool _saving;
	private string? _message;

	protected override AppScreen? ActiveTab => AppScreen.MentorDashboard;

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_cts?.Cancel();
	}

	protected override View BuildContent()
	{
		var s = Begin("Mentor Dashboard", "Manage requests and sessions");
		_body = new VerticalStackLayout { Spacing = 12 };
		s.Add(_body);

		if (_sessions is null || _mentor is null)
			_ = LoadAsync();
		else
			RenderAll();

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
			var mentor = Backend.Mentors.GetMineAsync(cts.Token);
			await Task.WhenAll(sessions, mentor);

			if (cts.IsCancellationRequested || body != _body)
				return;

			_sessions = sessions.Result;
			_mentor = mentor.Result;
			RenderAll();
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

	private void RenderAll()
	{
		var body = _body;
		if (body is null || _sessions is null || _mentor is null)
			return;

		body.Children.Clear();
		if (_message is not null)
			body.Add(T(_message, 11, Pink));

		var mine = _sessions.Where(x => x.IsMentorView).ToList();
		var requests = mine.Where(x => x.IsPending).OrderBy(x => x.StartsAt).ToList();
		var upcoming = mine.Where(x => x.IsBooked).OrderBy(x => x.StartsAt).ToList();
		var mentees = mine.Where(x => !x.IsCancelled).Select(x => x.MenteeName).Distinct(StringComparer.OrdinalIgnoreCase).Count();

		var stats = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
		var data = new[] { (requests.Count, "Requests"), (upcoming.Count, "Upcoming"), (mentees, "Mentees") };
		for (var i = 0; i < data.Length; i++)
		{
			var box = new VerticalStackLayout { Spacing = 2 };
			box.Add(T(data[i].Item1.ToString(), 17, Ink, true, true));
			box.Add(T(data[i].Item2, 10, Muted, false, true));
			stats.Add(Card(box), i, 0);
		}
		body.Add(stats);

		body.Add(Section("New requests"));
		if (requests.Count == 0)
			body.Add(T("No new requests right now.", 11, Muted));
		foreach (var request in requests)
			body.Add(RequestCard(request));

		body.Add(Section("Upcoming sessions"));
		if (upcoming.Count == 0)
			body.Add(T("Accepted sessions will show up here.", 11, Muted));
		foreach (var session in upcoming)
			body.Add(UpcomingCard(session));
		if (upcoming.Count > 0)
			body.Add(OutlineButton("Open all sessions", () => GoRoot(AppScreen.SessionOutcomes), Line, 38));

		body.Add(_editing ? EditCard() : ProfileCard());
	}

	private View RequestCard(SessionDto request)
	{
		var stack = new VerticalStackLayout { Spacing = 10 };
		var head = new Grid { ColumnSpacing = 10, ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
		head.Add(Avatar(Initials(request.MenteeName), 36), 0, 0);
		var info = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
		info.Add(T(request.MenteeName, 13, Ink, true));
		info.Add(T($"{request.Topic} · {request.Format}", 10, Muted));
		info.Add(T(FormatWhen(request.StartsAt), 10, Pink));
		head.Add(info, 1, 0);
		stack.Add(head);

		if (request.Goal.Length > 0)
			stack.Add(T($"Goal: {request.Goal}", 11, Muted));

		var buttons = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
		buttons.Add(GradButton("Accept", () => _ = RespondAsync(request, true), 36), 0, 0);
		buttons.Add(OutlineButton("Decline", () => _ = RespondAsync(request, false), Pink, 36), 1, 0);
		stack.Add(buttons);
		return Card(stack);
	}

	private static View UpcomingCard(SessionDto session)
	{
		var stack = new VerticalStackLayout { Spacing = 4 };
		stack.Add(T(FormatWhen(session.StartsAt), 13, Ink, true));
		stack.Add(T($"{session.Topic} with {session.MenteeName} · {session.Format}", 11, Muted));
		if (session.Goal.Length > 0)
			stack.Add(T($"Goal: {session.Goal}", 11, Muted));
		return Card(stack);
	}

	private View ProfileCard()
	{
		var mentor = _mentor!;
		var stack = new VerticalStackLayout { Spacing = 6 };

		var head = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
		head.Add(Section("Your mentor profile"), 0, 0);
		head.Add(Tap(T("Edit", 11, Pink), () =>
		{
			_editing = true;
			_message = null;
			RenderAll();
		}), 1, 0);
		stack.Add(head);

		stack.Add(T(mentor.IsAvailable ? "Accepting new requests" : "Not taking new requests", 11, mentor.IsAvailable ? Pink : Muted, true));
		stack.Add(T($"{mentor.Title} · {mentor.Category}" + (mentor.Years > 0 ? $" · {mentor.Years} yrs" : ""), 11, Ink));
		stack.Add(T(mentor.Availability.Length > 0 ? $"Available: {mentor.Availability}" : "Add when you're available, e.g. Weekday evenings", 11, Muted));
		stack.Add(T(mentor.Bio.Length > 0 ? mentor.Bio : "Add a short bio so mentees know how you can help.", 11, Muted));
		if (mentor.Tags.Count > 0)
			stack.Add(T(string.Join(" · ", mentor.Tags), 10, Muted));
		return Card(stack, 14);
	}

	private View EditCard()
	{
		var mentor = _mentor!;
		var category = Categories.Contains(mentor.Category) ? mentor.Category : Categories[0];
		var accepting = mentor.IsAvailable;
		var bioText = mentor.Bio;

		var stack = new VerticalStackLayout { Spacing = 10 };
		stack.Add(Section("Edit your mentor profile"));

		var title = AddField(stack, "Headline", "e.g. Senior Software Engineer", text: mentor.Title == "New mentor" ? "" : mentor.Title);
		stack.Add(T("Category", 11, Muted));
		stack.Add(ChipRow(Categories, false, [Array.IndexOf(Categories, category)], c => category = c));
		var years = AddField(stack, "Years of experience", "0", text: mentor.Years.ToString());
		var tags = AddField(stack, "Skills (comma separated)", "React, Node.js, Career growth", text: string.Join(", ", mentor.Tags));
		var availability = AddField(stack, "When are you available?", "Weekday evenings", text: mentor.Availability);

		stack.Add(T("About you", 11, Muted));
		var bio = TextBox("A short bio for mentees", bioText, 90);
		bio.TextChanged += (_, e) => bioText = e.NewTextValue ?? "";
		stack.Add(Card(bio, 6));

		stack.Add(T("New requests", 11, Muted));
		stack.Add(ChipRow(["Accepting", "Paused"], false, [accepting ? 0 : 1], c => accepting = c == "Accepting"));

		stack.Add(GradButton(_saving ? "Saving..." : "Save profile", () =>
			_ = SaveAsync(
				title.Text?.Trim() ?? "",
				category,
				bioText.Trim(),
				(tags.Text ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
				int.TryParse(years.Text, out var y) ? y : 0,
				accepting,
				availability.Text?.Trim() ?? ""), 40));
		stack.Add(OutlineButton("Cancel", () =>
		{
			_editing = false;
			RenderAll();
		}, Line, 36));
		return Card(stack, 14);
	}

	private async Task SaveAsync(string title, string category, string bio, List<string> tags, int years, bool accepting, string availability)
	{
		if (_saving)
			return;

		_saving = true;
		_message = null;
		try
		{
			_mentor = await Backend.Mentors.SaveMineAsync(title.Length > 0 ? title : "Mentor", category, bio, tags, years, accepting, availability);
			_editing = false;
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
			return;
		}
		catch (Exception ex)
		{
			_message = ErrorText(ex);
		}
		finally
		{
			_saving = false;
		}

		MainThread.BeginInvokeOnMainThread(RenderAll);
	}

	private async Task RespondAsync(SessionDto request, bool accept)
	{
		_message = null;
		try
		{
			var updated = await Backend.Sessions.RespondAsync(request.Id, accept);
			var index = _sessions!.FindIndex(x => x.Id == updated.Id);
			if (index >= 0)
				_sessions[index] = updated;
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
			return;
		}
		catch (Exception ex)
		{
			_message = ErrorText(ex);
		}

		MainThread.BeginInvokeOnMainThread(RenderAll);
	}

	private static string Initials(string name)
	{
		var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return parts.Length switch
		{
			0 => "?",
			1 => parts[0][..1].ToUpperInvariant(),
			_ => $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
		};
	}
}
