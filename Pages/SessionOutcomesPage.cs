namespace HerMentor;

public sealed class SessionOutcomesPage : AppPage
{
	private List<SessionDto>? _sessions;
	private VerticalStackLayout? _body;
	private CancellationTokenSource? _cts;

	private int? _logging;
	private string _summary = "";
	private string _actions = "";
	private string _feedback = "";
	private string? _message;

	protected override AppScreen? ActiveTab => AppScreen.SessionOutcomes;

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_cts?.Cancel();
	}

	protected override View BuildContent()
	{
		var s = Begin("Sessions", "Your upcoming sessions and what came out of them");
		_body = new VerticalStackLayout { Spacing = 12 };
		s.Add(_body);

		if (_sessions is null)
			_ = LoadAsync();
		else
			RenderList();

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
		body.Add(T("Loading your sessions...", 12, Muted));

		try
		{
			var sessions = await Backend.Sessions.ListAsync(cts.Token);
			if (cts.IsCancellationRequested || body != _body)
				return;

			_sessions = sessions;
			RenderList();
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

	private void RenderList()
	{
		var body = _body;
		if (body is null || _sessions is null)
			return;

		body.Children.Clear();

		if (_message is not null)
			body.Add(T(_message, 11, Pink));

		if (_sessions.Count == 0)
		{
			var card = new VerticalStackLayout { Spacing = 10 };
			card.Add(T("You haven't booked a session yet.", 12, Ink));
			card.Add(GradButton("Find a mentor", () => GoRoot(AppScreen.FindMentors), 40));
			body.Add(Card(card, 14));
			return;
		}

		var upcoming = _sessions.Where(x => x.IsUpcoming).OrderBy(x => x.StartsAt).ToList();
		var past = _sessions.Where(x => !x.IsUpcoming).OrderByDescending(x => x.StartsAt).ToList();

		if (upcoming.Count > 0)
		{
			body.Add(Section("Upcoming"));
			foreach (var session in upcoming)
				body.Add(UpcomingCard(session));
		}

		if (past.Count > 0)
		{
			body.Add(Section("Past sessions"));
			foreach (var session in past)
				body.Add(session.IsCancelled ? CancelledCard(session) : CompletedCard(session));
		}
	}

	private View UpcomingCard(SessionDto session)
	{
		var stack = new VerticalStackLayout { Spacing = 6 };
		stack.Add(T(FormatWhen(session.StartsAt), 14, Ink, true));
		stack.Add(T($"{session.Topic} with {session.OtherName} · {session.Format}", 11, Muted));
		if (session.Goal.Length > 0)
			stack.Add(T($"Goal: {session.Goal}", 11, Muted));

		if (session.IsPending)
		{
			stack.Add(T(session.IsMentorView ? "Waiting for your answer" : "Awaiting mentor confirmation", 11, Pink, true));
			var pending = new Grid
			{
				ColumnSpacing = 8,
				ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }
			};
			if (session.IsMentorView)
			{
				pending.Add(GradButton("Accept", () => _ = RespondAsync(session, true), 36), 0, 0);
				pending.Add(OutlineButton("Decline", () => _ = RespondAsync(session, false), Pink, 36), 1, 0);
			}
			else
			{
				pending.Add(OutlineButton(session.Prep is null ? "Prepare" : "Prep", () => SessionPrepPage.Open(session), Line, 36), 0, 0);
				pending.Add(OutlineButton("Cancel request", () => _ = CancelAsync(session), Line, 36), 1, 0);
			}
			stack.Add(pending);
			return Card(stack, 14);
		}

		if (_logging == session.Id)
		{
			stack.Add(OutcomeForm(session));
			return Card(stack, 14);
		}

		var buttons = new Grid
		{
			ColumnSpacing = 8,
			ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }
		};
		buttons.Add(OutlineButton(session.Prep is null ? "Prepare" : "Prep", () => SessionPrepPage.Open(session), Line, 36), 0, 0);
		buttons.Add(OutlineButton("Log outcome", () => StartLogging(session), Line, 36), 1, 0);
		buttons.Add(OutlineButton("Cancel", () => _ = CancelAsync(session), Line, 36), 2, 0);
		stack.Add(buttons);
		return Card(stack, 14);
	}

	private View CancelledCard(SessionDto session)
	{
		var stack = new VerticalStackLayout { Spacing = 2 };
		stack.Add(T($"{FormatWhen(session.StartsAt)} · Cancelled", 11, Muted));
		stack.Add(T($"{session.Topic} with {session.OtherName}", 11, Muted));
		return Card(stack);
	}

	private View CompletedCard(SessionDto session)
	{
		var stack = new VerticalStackLayout { Spacing = 8 };
		stack.Add(T(FormatWhen(session.StartsAt), 13, Ink, true));
		stack.Add(T($"{session.Topic} with {session.OtherName}", 11, Muted));

		if (_logging == session.Id)
		{
			stack.Add(OutcomeForm(session));
			return Card(stack, 14);
		}

		if (session.Summary.Length > 0)
		{
			stack.Add(T("Summary", 12, Ink, true));
			stack.Add(T(session.Summary, 11, Muted));
		}

		if (session.Actions.Count > 0)
		{
			stack.Add(T("Action items", 12, Ink, true));
			foreach (var action in session.Actions)
			{
				var current = action;
				stack.Add(Tap(CheckRow(current.Text, current.Done), () => _ = ToggleAsync(session, current)));
			}
		}

		stack.Add(T("Mentor feedback", 12, Ink, true));
		stack.Add(T(session.MentorFeedback.Length > 0 ? session.MentorFeedback : "No feedback yet.", 11, Muted));

		stack.Add(OutlineButton(session.IsMentorView ? "Edit feedback" : "Edit outcome", () => StartLogging(session), Line, 36));
		return Card(stack, 14);
	}

	private View OutcomeForm(SessionDto session)
	{
		var form = new VerticalStackLayout { Spacing = 8 };

		if (session.IsMentorView)
		{
			form.Add(T("Your feedback for the mentee", 12, Ink, true));
			var feedback = TextBox("What went well and what to focus on next", _feedback);
			feedback.TextChanged += (_, e) => _feedback = e.NewTextValue ?? "";
			form.Add(Card(feedback, 6));
		}
		else
		{
			form.Add(T("What did you cover?", 12, Ink, true));
			var summary = TextBox("e.g. Reviewed my portfolio and agreed on next steps", _summary);
			summary.TextChanged += (_, e) => _summary = e.NewTextValue ?? "";
			form.Add(Card(summary, 6));

			form.Add(T("Action items (one per line)", 12, Ink, true));
			var actions = TextBox("Build a REST API project\nWrite a portfolio case study", _actions, 90);
			actions.TextChanged += (_, e) => _actions = e.NewTextValue ?? "";
			form.Add(Card(actions, 6));
		}

		form.Add(GradButton("Save outcome", () => _ = SaveAsync(session), 40));
		form.Add(OutlineButton("Close", () =>
		{
			_logging = null;
			RenderList();
		}, Line, 36));
		return form;
	}

	private void StartLogging(SessionDto session)
	{
		_logging = session.Id;
		_summary = session.Summary;
		_actions = string.Join("\n", session.Actions.Select(a => a.Text));
		_feedback = session.MentorFeedback;
		_message = null;
		RenderList();
	}

	private async Task SaveAsync(SessionDto session)
	{
		var actions = _actions
			.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.ToList();

		await Run(async () =>
		{
			var updated = await Backend.Sessions.LogOutcomeAsync(session.Id, _summary, actions, _feedback);
			_logging = null;
			Replace(updated);
		});
	}

	private Task CancelAsync(SessionDto session) =>
		Run(async () => Replace(await Backend.Sessions.CancelAsync(session.Id)));

	private Task RespondAsync(SessionDto session, bool accept) =>
		Run(async () => Replace(await Backend.Sessions.RespondAsync(session.Id, accept)));

	private Task ToggleAsync(SessionDto session, SessionActionDto action) =>
		Run(async () => Replace(await Backend.Sessions.ToggleActionAsync(session.Id, action.Id)));

	private async Task Run(Func<Task> work)
	{
		_message = null;
		try
		{
			await work();
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

		MainThread.BeginInvokeOnMainThread(RenderList);
	}

	private void Replace(SessionDto updated)
	{
		if (_sessions is null)
			return;

		var index = _sessions.FindIndex(x => x.Id == updated.Id);
		if (index >= 0)
			_sessions[index] = updated;
	}
}
