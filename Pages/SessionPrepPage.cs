namespace HerMentor;

public sealed class SessionPrepPage : AppPage
{
	private static SessionDto? _session;

	/// <summary>Opens prep for a booked session so the result can be saved to it.</summary>
	public static void Open(SessionDto session)
	{
		_session = session;
		Go(AppScreen.SessionPrep);
	}

	private string _goal = _session?.Goal ?? "";
	private PrepResponse? _result = _session?.Prep;
	private bool _saving;

	private VerticalStackLayout? _body;
	private Entry? _goalEntry;
	private CancellationTokenSource? _cts;
	private bool _busy;

	protected override AppScreen? ActiveTab => AppScreen.SessionOutcomes;

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_cts?.Cancel();
	}

	protected override View BuildContent()
	{
		var session = _session;
		var subtitle = session is not null
			? $"{session.OtherName} · {FormatWhen(session.StartsAt)}"
			: CurrentMentor.Id > 0 ? $"AI-prepared for your session with {CurrentMentor.Name}" : "AI-prepared for your next session";
		var s = Begin("Session Prep", subtitle, back: true);

		s.Add(Section("Your session goal"));
		_goalEntry = new Entry
		{
			Placeholder = "e.g. Get feedback on my portfolio and plan my next 3 months",
			Text = _goal,
			FontSize = 12,
			TextColor = Ink,
			PlaceholderColor = Placeholder,
			BackgroundColor = Colors.Transparent,
			MaxLength = 300
		};
		_goalEntry.Completed += (_, _) => _ = GenerateAsync();
		_goalEntry.TextChanged += (_, e) => _goal = e.NewTextValue ?? "";
		s.Add(Card(_goalEntry, 6));

		_body = new VerticalStackLayout { Spacing = 12 };
		s.Add(_body);

		if (_result is { } cached)
			Render(cached);
		else
			ShowStart();

		return s;
	}

	private void ShowStart()
	{
		var body = _body;
		if (body is null)
			return;

		body.Children.Clear();
		body.Add(T("Tell us what you want from the session and HerMentor AI will suggest questions and an agenda.", 11, Muted));
		body.Add(GradButton("Prepare with AI", () => _ = GenerateAsync()));
	}

	private async Task GenerateAsync()
	{
		var body = _body;
		if (_busy || body is null)
			return;

		_busy = true;
		_cts?.Cancel();
		var cts = _cts = new CancellationTokenSource();
		body.Children.Clear();
		body.Add(T("HerMentor AI is preparing your session...", 12, Muted));

		try
		{
			_result = await Backend.Prep.GenerateAsync(_session?.MentorId ?? CurrentMentor.Id, _goal.Trim(), cts.Token);
			if (cts.IsCancellationRequested || body != _body)
				return;

			Render(_result);
		}
		catch (OperationCanceledException)
		{
			// Page closed.
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
		}
		catch (Exception ex) when (!cts.IsCancellationRequested && body == _body)
		{
			body.Children.Clear();
			body.Add(T(ErrorText(ex), 12, Pink));
			body.Add(OutlineButton("Try again", () => _ = GenerateAsync(), Line, 38));
		}
		finally
		{
			_busy = false;
		}
	}

	private void Render(PrepResponse prep)
	{
		var body = _body;
		if (body is null)
			return;

		body.Children.Clear();

		var goal = new VerticalStackLayout { Spacing = 4 };
		goal.Add(T("SESSION GOAL", 9, Pink, true));
		goal.Add(T(prep.Goal, 12));
		body.Add(Card(goal));

		body.Add(Section("Suggested questions"));
		for (var i = 0; i < prep.Questions.Count; i++)
			body.Add(Card(T($"{i + 1}. {prep.Questions[i]}", 12)));

		body.Add(Section("Suggested agenda"));
		var agenda = new VerticalStackLayout { Spacing = 6 };
		foreach (var item in prep.Agenda)
		{
			var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
			row.Add(T(item.Item, 11, Muted), 0, 0);
			row.Add(T($"{item.Minutes} min", 11, Pink), 1, 0);
			agenda.Add(row);
		}
		body.Add(Card(agenda));

		if (!prep.AiGenerated)
			body.Add(T("The AI is unavailable right now, so these are general suggestions.", 10, Muted));

		if (_session is { } session && !session.IsBooked)
			body.Add(GradButton("Done", () => GoRoot(AppScreen.SessionOutcomes)));
		else if (_session is not null)
			body.Add(GradButton(_saving ? "Saving..." : "Save to session", () => _ = SaveAsync(prep)));
		else
			body.Add(GradButton("Done", () => GoRoot(AppScreen.SessionOutcomes)));
		body.Add(OutlineButton("Regenerate", () => _ = GenerateAsync()));
	}

	private async Task SaveAsync(PrepResponse prep)
	{
		if (_saving || _session is not { } session)
			return;

		_saving = true;
		try
		{
			await Backend.Sessions.SavePrepAsync(session.Id, prep);
			GoRoot(AppScreen.SessionOutcomes);
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
		}
		catch (Exception ex)
		{
			if (_body is { } body)
				body.Add(T(ErrorText(ex), 11, Pink));
		}
		finally
		{
			_saving = false;
		}
	}
}
