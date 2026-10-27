namespace HerMentor;

public sealed class FindMentorsPage : AppPage
{
	private static readonly string[] Categories = ["All", "Tech", "Design", "Data", "Leadership"];

	private string _category = "All";
	private string _query = "";

	private VerticalStackLayout? _list;
	private Label? _liveLabel;
	private CancellationTokenSource? _loadCts;
	private CancellationTokenSource? _debounceCts;

	protected override AppScreen? ActiveTab => AppScreen.FindMentors;

	protected override void OnAppearing()
	{
		base.OnAppearing();
		Backend.Realtime.MentorsChanged += OnMentorsChanged;
		Backend.Realtime.ConnectionChanged += OnConnectionChanged;
		_ = Backend.Realtime.AcquireAsync();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		Backend.Realtime.MentorsChanged -= OnMentorsChanged;
		Backend.Realtime.ConnectionChanged -= OnConnectionChanged;
		_ = Backend.Realtime.ReleaseAsync();
		_loadCts?.Cancel();
		_debounceCts?.Cancel();
	}

	protected override View BuildContent()
	{
		var s = Begin("Find a Mentor", "Matched on industry, expertise and goals");

		_liveLabel = T("", 10, Pink);
		SetLive(Backend.Realtime.IsConnected);
		s.Add(_liveLabel);

		var query = new Entry
		{
			Placeholder = "Search by skill, industry or name",
			Text = _query,
			FontSize = 12,
			TextColor = Ink,
			PlaceholderColor = Placeholder,
			BackgroundColor = Colors.Transparent
		};
		query.TextChanged += (_, e) => OnQueryChanged(e.NewTextValue);

		var search = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 8 };
		search.Add(query, 0, 0);
		search.Add(IconCircle(IconSearch, 26), 1, 0);
		s.Add(Card(search, 6));

		s.Add(ChipRow(Categories, false, [Math.Max(0, Array.IndexOf(Categories, _category))], category =>
		{
			_category = category;
			_ = LoadAsync();
		}));

		_list = new VerticalStackLayout { Spacing = 10 };
		s.Add(_list);

		_ = LoadAsync();
		return s;
	}

	private void OnQueryChanged(string? text)
	{
		_query = text?.Trim() ?? "";
		_debounceCts?.Cancel();
		var cts = _debounceCts = new CancellationTokenSource();

		// Wait for a pause in typing so we don't hit the server on every keystroke.
		_ = Task.Delay(300, cts.Token).ContinueWith(
			t =>
			{
				if (!t.IsCanceled)
					MainThread.BeginInvokeOnMainThread(() => _ = LoadAsync());
			},
			TaskScheduler.Default);
	}

	private void OnMentorsChanged() =>
		MainThread.BeginInvokeOnMainThread(() => _ = LoadAsync(silent: true));

	private void OnConnectionChanged(bool connected) =>
		MainThread.BeginInvokeOnMainThread(() => SetLive(connected));

	private void SetLive(bool connected)
	{
		if (_liveLabel is null)
			return;

		_liveLabel.Text = connected ? "● Live — new mentors appear as they join" : "";
		_liveLabel.IsVisible = connected;
	}

	// silent = refresh in place without flashing the loading text (used for live updates).
	private async Task LoadAsync(bool silent = false)
	{
		var list = _list;
		if (list is null)
			return;

		_loadCts?.Cancel();
		var cts = _loadCts = new CancellationTokenSource();

		if (!silent || list.Children.Count == 0)
			ShowMessage(list, "Finding mentors...");

		try
		{
			var mentors = await Backend.Mentors.SearchAsync(_query, _category, cts.Token);
			if (cts.IsCancellationRequested || list != _list)
				return;

			list.Children.Clear();
			if (mentors.Count == 0)
			{
				ShowMessage(list, "No mentors match your search yet. Try a different skill or category.");
				return;
			}

			foreach (var mentor in mentors)
				list.Add(MentorCard(mentor.ToMentor()));
		}
		catch (OperationCanceledException)
		{
			// A newer search replaced this one.
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
		}
		catch (Exception ex) when (!cts.IsCancellationRequested && list == _list)
		{
			list.Children.Clear();
			list.Add(T(ErrorText(ex), 12, Pink));
			list.Add(OutlineButton("Try again", () => _ = LoadAsync(), Line, 38));
		}
	}

	private static void ShowMessage(VerticalStackLayout list, string text)
	{
		list.Children.Clear();
		list.Add(T(text, 12, Muted, false, true));
	}
}
