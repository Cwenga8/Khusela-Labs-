namespace HerMentor;

public sealed class CareerGuidancePage : AppPage
{
	private static readonly string[] Suggestions =
	[
		"How do I land my first tech role?",
		"What should I learn next?",
		"How can a mentor help me?"
	];

	private VerticalStackLayout? _paths;
	private VerticalStackLayout? _thread;
	private View? _inputCard;
	private CancellationTokenSource? _loadCts;
	private bool _asking;

	protected override AppScreen? ActiveTab => AppScreen.CareerGuidance;

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_loadCts?.Cancel();
	}

	protected override View BuildContent()
	{
		var s = Begin("Career Guidance", "A path tailored to your goals");

		s.Add(Section("Careers that fit you"));
		_paths = new VerticalStackLayout { Spacing = 10 };
		s.Add(_paths);
		s.Add(GradButton("View skill gaps", () =>
		{
			SkillGapPage.SelectedRole = null;
			Go(AppScreen.SkillGap);
		}));

		s.Add(new BoxView { HeightRequest = 4, Color = Colors.Transparent });
		s.Add(Section("Ask HerMentor AI"));

		_thread = new VerticalStackLayout { Spacing = 8 };
		RenderThread();
		s.Add(_thread);

		var input = new Entry
		{
			Placeholder = "Ask HerMentor AI about your career...",
			FontSize = 12,
			TextColor = Ink,
			PlaceholderColor = Placeholder,
			BackgroundColor = Colors.Transparent,
			MaxLength = 500
		};
		input.Completed += (_, _) => Send(input);

		var ask = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 8 };
		ask.Add(input, 0, 0);
		ask.Add(Tap(IconCircle(IconSend, 28), () => Send(input)), 1, 0);
		_inputCard = Card(ask, 6);
		s.Add(_inputCard);

		_ = LoadPathsAsync();
		return s;
	}

	private async Task LoadPathsAsync()
	{
		var box = _paths;
		if (box is null)
			return;

		_loadCts?.Cancel();
		var cts = _loadCts = new CancellationTokenSource();

		if (box.Children.Count == 0)
			ShowText(box, "Finding careers that fit you...", Muted);

		try
		{
			var result = await Backend.Career.GetPathsAsync(cts.Token);
			if (cts.IsCancellationRequested || box != _paths)
				return;

			box.Children.Clear();
			if (result.NeedsProfile)
			{
				var card = new VerticalStackLayout { Spacing = 10 };
				card.Add(T("Add your skills and interests to see which careers fit you best.", 12, Ink));
				card.Add(GradButton("Build my profile", () => Go(AppScreen.ProfileSetup), 40));
				box.Add(Card(card, 14));
				return;
			}

			foreach (var path in result.Paths.Take(4))
				box.Add(PathCard(path));
		}
		catch (OperationCanceledException)
		{
			// Page closed or reloaded.
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
		}
		catch (Exception ex) when (!cts.IsCancellationRequested && box == _paths)
		{
			box.Children.Clear();
			box.Add(T(ErrorText(ex), 12, Pink));
			box.Add(OutlineButton("Try again", () => _ = LoadPathsAsync(), Line, 38));
		}
	}

	private static View PathCard(CareerPathDto path)
	{
		var stack = new VerticalStackLayout { Spacing = 6 };

		var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
		row.Add(T(path.Name, 12, Ink, true), 0, 0);
		row.Add(T($"{path.Fit}%", 11, Muted), 1, 0);
		stack.Add(row);
		stack.Add(Bar(path.Fit));

		var caption = path.Missing.Count == 0
			? "You have every core skill for this role."
			: $"To build: {string.Join(", ", path.Missing)}";
		stack.Add(T(caption, 10, Muted));
		return Tap(Card(stack), () =>
		{
			SkillGapPage.SelectedRole = path.Id;
			Go(AppScreen.SkillGap);
		});
	}

	private static void ShowText(VerticalStackLayout box, string text, Color color)
	{
		box.Children.Clear();
		box.Add(T(text, 12, color, false, true));
	}

	private void RenderThread()
	{
		var thread = _thread;
		if (thread is null)
			return;

		thread.Children.Clear();
		var chat = Backend.Assistant.CareerChat;

		if (chat.Count == 0)
		{
			thread.Add(T("Not sure where to start? Try one of these:", 11, Muted));
			var chips = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
			foreach (var suggestion in Suggestions)
			{
				var (chip, _) = Chip(suggestion, false);
				chip.Margin = new Thickness(0, 0, 6, 6);
				chips.Add(Tap(chip, () => _ = AskAsync(suggestion)));
			}
			thread.Add(chips);
			return;
		}

		foreach (var turn in chat)
			thread.Add(ChatBubble(Tidy(turn.Content), turn.Role == "user"));
	}

	private void Send(Entry input)
	{
		var question = input.Text?.Trim() ?? "";
		if (question.Length == 0 || _asking)
			return;

		input.Text = string.Empty;
		_ = AskAsync(question);
	}

	private async Task AskAsync(string question)
	{
		if (_asking || _thread is not { } thread)
			return;

		_asking = true;
		var chat = Backend.Assistant.CareerChat;
		var history = chat.ToList();

		if (chat.Count == 0)
			thread.Children.Clear();

		thread.Add(ChatBubble(question, true));
		var reply = ChatBubble("...", false);
		var label = BubbleLabel(reply);
		thread.Add(reply);
		if (_inputCard is not null)
			await ScrollToAsync(_inputCard);

		var answer = new System.Text.StringBuilder();
		try
		{
			await foreach (var chunk in Backend.Assistant.AskAsync(question, history))
			{
				answer.Append(chunk);
				label.Text = Tidy(answer.ToString());
				if (_inputCard is not null)
					await ScrollToAsync(_inputCard);
			}

			chat.Add(new ChatTurn("user", question));
			chat.Add(new ChatTurn("assistant", answer.ToString()));
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
		}
		catch (Exception ex)
		{
			label.TextColor = Pink;
			label.Text = ErrorText(ex);
		}
		finally
		{
			_asking = false;
		}
	}

	// Models like to add markdown; the chat bubble shows plain text.
	private static string Tidy(string text) =>
		text.Replace("**", "").Replace("__", "").Replace("\n# ", "\n").TrimStart('#', ' ');
}
