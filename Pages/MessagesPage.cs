namespace HerMentor;

public sealed class MessagesPage : AppPage
{
	private static int? _sessionId;
	private SessionDto? _session;
	private List<ChatMessageDto> _messages = [];
	private int? _loadedSessionId;
	private CancellationTokenSource? _cts;
	private string? _error;
	private string? _notice;
	private string _draft = "";
	private bool _loading;
	private bool _busy;

	protected override AppScreen? ActiveTab => AppScreen.SessionOutcomes;

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_ = LoadAsync();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_cts?.Cancel();
	}

	public static Task OpenFor(SessionDto session)
	{
		_sessionId = session.Id;
		return Go(AppScreen.Messages);
	}

	protected override View BuildContent()
	{
		var s = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(18, 22, 18, 20) };
		s.Add(BackLink());

		var isCurrentConversation = _loadedSessionId == _sessionId;
		var session = isCurrentConversation ? _session : null;
		var head = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
		var title = new VerticalStackLayout { Spacing = 2 };
		title.Add(T(session?.OtherName ?? "Messages", 17, Ink, true));
		title.Add(T(session is null ? "Choose a session to open its conversation." : $"Session · {session.Topic}", 11, Muted));
		head.Add(title, 0, 0);
		if (session is not null && _messages.Count > 0)
			head.Add(OutlineButton("Clear chat", () => _ = ClearChatAsync(), Pink, 34), 1, 0);
		s.Add(head);

		if (_loading || !isCurrentConversation)
		{
			s.Add(T("Loading your conversation...", 12, Muted));
			return s;
		}

		if (_error is not null)
		{
			s.Add(Card(T(_error, 12, Pink)));
			s.Add(OutlineButton("Retry", () => _ = LoadAsync(), Line, 36));
			return s;
		}

		if (_notice is not null)
			s.Add(Card(T(_notice, 11, Pink)));

		if (session is null)
		{
			s.Add(T("Open a session to view its conversation.", 12, Muted));
			return s;
		}

		if (_messages.Count == 0)
			s.Add(T("No messages yet. Start the conversation below.", 12, Muted));
		else
			foreach (var message in _messages)
				s.Add(MessageCard(message));

		var input = new Entry
		{
			Placeholder = "Type a message...",
			Text = _draft,
			MaxLength = 2000,
			FontSize = 12,
			TextColor = Ink,
			PlaceholderColor = Placeholder,
			BackgroundColor = Colors.Transparent,
			IsEnabled = !_busy
		};
		input.TextChanged += (_, e) => _draft = e.NewTextValue ?? "";
		input.Completed += (_, _) => _ = SendAsync();
		var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 8 };
		row.Add(input, 0, 0);
		row.Add(GradButton(_busy ? "Sending..." : "Send", () => _ = SendAsync(), 38), 1, 0);
		s.Add(Card(row, 6));

		return s;
	}

	private View MessageCard(ChatMessageDto message)
	{
		var stack = new VerticalStackLayout
		{
			Spacing = 4,
			HorizontalOptions = message.IsMine ? LayoutOptions.End : LayoutOptions.Start
		};
		stack.Add(T($"{message.SenderName} · {message.CreatedAt.ToLocalTime():dd MMM, HH:mm}", 10, Muted));
		stack.Add(new Border
		{
			Padding = new Thickness(12, 9),
			MaximumWidthRequest = 280,
			Background = message.IsMine ? new SolidColorBrush(Purple) : new SolidColorBrush(Surface),
			Stroke = message.IsMine ? Purple : Line,
			StrokeThickness = 1,
			StrokeShape = RR(12),
			Content = T(message.Content, 12, message.IsMine ? Colors.White : Ink)
		});
		stack.Add(OutlineButton("Delete message", () => _ = DeleteMessageAsync(message), Line, 30));
		return stack;
	}

	private async Task LoadAsync()
	{
		var sessionId = _sessionId;
		if (sessionId is null)
		{
			_loadedSessionId = null;
			_session = null;
			_error = null;
			_loading = false;
			Refresh();
			return;
		}

		_cts?.Cancel();
		var cts = _cts = new CancellationTokenSource();
		_loadedSessionId = sessionId;
		_session = null;
		_messages = [];
		_error = null;
		_notice = null;
		_loading = true;
		Refresh();

		try
		{
			var sessions = await Backend.Sessions.ListAsync(cts.Token);
			_session = sessions.FirstOrDefault(x => x.Id == sessionId.Value);
			if (_session is null)
			{
				_error = "This conversation is not available for your account.";
				return;
			}

			_messages = await Backend.Messages.ListAsync(sessionId.Value, cts.Token);
		}
		catch (OperationCanceledException)
		{
			// Page closed or another conversation was selected.
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
		}
		catch (Exception ex) when (!cts.IsCancellationRequested)
		{
			_error = ErrorText(ex);
		}
		finally
		{
			if (!cts.IsCancellationRequested && _loadedSessionId == sessionId)
			{
				_loading = false;
				Refresh();
			}
		}
	}

	private async Task SendAsync()
	{
		if (_busy || _session is null || string.IsNullOrWhiteSpace(_draft))
			return;

		_busy = true;
		_notice = null;
		var content = _draft.Trim();
		try
		{
			var sent = await Backend.Messages.SendAsync(_session.Id, content);
			_messages.Add(sent);
			_draft = "";
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
			return;
		}
		catch (Exception ex)
		{
			_notice = ErrorText(ex);
		}
		finally
		{
			_busy = false;
		}

		Refresh();
	}

	private async Task DeleteMessageAsync(ChatMessageDto message)
	{
		if (_busy || _session is null)
			return;

		var confirmed = await DisplayAlert(
			"Delete message?",
			"This will delete the message for both people in this conversation. This cannot be undone.",
			"Delete",
			"Cancel");
		if (!confirmed)
			return;

		_busy = true;
		_notice = null;
		try
		{
			await Backend.Messages.DeleteAsync(_session.Id, message.Id);
			_messages.RemoveAll(m => m.Id == message.Id);
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
			return;
		}
		catch (Exception ex)
		{
			_notice = ErrorText(ex);
		}
		finally
		{
			_busy = false;
		}

		Refresh();
	}

	private async Task ClearChatAsync()
	{
		if (_busy || _session is null || _messages.Count == 0)
			return;

		var confirmed = await DisplayAlert(
			"Clear chat?",
			"This permanently deletes every message in this conversation for both people. This cannot be undone.",
			"Clear chat",
			"Cancel");
		if (!confirmed)
			return;

		_busy = true;
		_notice = null;
		try
		{
			await Backend.Messages.ClearAsync(_session.Id);
			_messages.Clear();
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
			return;
		}
		catch (Exception ex)
		{
			_notice = ErrorText(ex);
		}
		finally
		{
			_busy = false;
		}

		Refresh();
	}
}
