namespace HerMentor;

public sealed class BookSessionPage : AppPage
{
	private static readonly string[] Times = ["09:00", "10:00", "14:00", "16:00"];
	private static readonly string[] Formats = ["Video", "Voice", "Chat"];
	private const int DaysAhead = 7;

	private readonly DateTime _firstDay = DateTime.Today.AddDays(1);
	private int _day;
	private string? _time;
	private string _format = Formats[0];
	private string _topic = "";
	private string _goal = "";
	private HashSet<DateTime> _busy = [];
	private bool _busyLoaded;
	private bool _booking;

	private VerticalStackLayout? _times;
	private Label? _status;
	private CancellationTokenSource? _cts;

	protected override AppScreen? ActiveTab => AppScreen.SessionOutcomes;

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_cts?.Cancel();
	}

	protected override View BuildContent()
	{
		var mentor = CurrentMentor;
		if (mentor.Id <= 0)
		{
			var empty = Begin("Book a Session", null, back: true);
			empty.Add(Card(T("Pick a mentor first, then request a session from their profile.", 12, Ink)));
			empty.Add(GradButton("Find a mentor", () => GoRoot(AppScreen.FindMentors)));
			return empty;
		}

		var s = Begin("Book a Session", $"with {mentor.Name}", back: true);

		s.Add(Section("Pick a date"));
		var days = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
		var setters = new List<Action<bool>>();
		for (var i = 0; i < DaysAhead; i++)
		{
			var index = i;
			var (chip, set) = Chip(_firstDay.AddDays(i).ToString("ddd d", System.Globalization.CultureInfo.InvariantCulture), i == _day);
			chip.Margin = new Thickness(0, 0, 6, 6);
			setters.Add(set);
			days.Add(Tap(chip, () =>
			{
				_day = index;
				_time = null;
				for (var j = 0; j < setters.Count; j++)
					setters[j](j == index);
				RenderTimes();
			}));
		}
		s.Add(days);

		s.Add(Section("Time"));
		_times = new VerticalStackLayout();
		s.Add(_times);
		RenderTimes();

		s.Add(Section("Format"));
		s.Add(ChipRow(Formats, false, [Array.IndexOf(Formats, _format)], f => _format = f));

		var topic = AddField(s, "Topic", "Breaking into tech", text: _topic);
		topic.MaxLength = 80;
		topic.TextChanged += (_, e) => _topic = e.NewTextValue ?? "";
		var goal = AddField(s, "Goal for this session", "Review my portfolio", text: _goal);
		goal.MaxLength = 300;
		goal.TextChanged += (_, e) => _goal = e.NewTextValue ?? "";

		_status = T("", 11, Pink);
		s.Add(_status);
		s.Add(GradButton("Request session", () => _ = BookAsync(mentor)));

		if (!_busyLoaded)
			_ = LoadBusyAsync(mentor.Id);

		return s;
	}

	private async Task LoadBusyAsync(int mentorId)
	{
		_cts?.Cancel();
		var cts = _cts = new CancellationTokenSource();

		try
		{
			var taken = await Backend.Sessions.BusyAsync(mentorId, cts.Token);
			if (cts.IsCancellationRequested)
				return;

			_busy = taken.Select(t => t.ToLocalTime()).ToHashSet();
			_busyLoaded = true;
			MainThread.BeginInvokeOnMainThread(RenderTimes);
		}
		catch (OperationCanceledException)
		{
			// Page closed.
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
		}
		catch
		{
			// Without the busy list the server still rejects taken slots, so keep going.
		}
	}

	private DateTime Slot(string time)
	{
		var parts = time.Split(':');
		return _firstDay.AddDays(_day).AddHours(int.Parse(parts[0])).AddMinutes(int.Parse(parts[1]));
	}

	private void RenderTimes()
	{
		if (_times is null)
			return;

		_times.Children.Clear();
		var row = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
		var setters = new List<Action<bool>>();

		for (var i = 0; i < Times.Length; i++)
		{
			var time = Times[i];
			var taken = _busy.Contains(Slot(time));
			var (chip, set) = Chip(taken ? $"{time} · taken" : time, !taken && _time == time);
			chip.Margin = new Thickness(0, 0, 6, 6);
			setters.Add(set);

			if (taken)
			{
				chip.Opacity = 0.45;
			}
			else
			{
				var index = i;
				Tap(chip, () =>
				{
					_time = time;
					for (var j = 0; j < setters.Count; j++)
						setters[j](j == index);
				});
			}

			row.Add(chip);
		}

		_times.Add(row);
	}

	private async Task BookAsync(Mentor mentor)
	{
		if (_booking)
			return;

		if (_time is null)
		{
			Say("Pick a time first.");
			return;
		}

		_booking = true;
		Say("");
		try
		{
			var local = DateTime.SpecifyKind(Slot(_time), DateTimeKind.Local);
			var session = await Backend.Sessions.BookAsync(mentor.Id, local.ToUniversalTime(), _format, _topic, _goal);
			SessionPrepPage.Open(session);
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
		}
		catch (Exception ex)
		{
			Say(ErrorText(ex));
			if (ex is ApiException { StatusCode: 409 })
			{
				_busyLoaded = false;
				_time = null;
				await LoadBusyAsync(mentor.Id);
			}
		}
		finally
		{
			_booking = false;
		}
	}

	private void Say(string text) => MainThread.BeginInvokeOnMainThread(() =>
	{
		if (_status is not null)
			_status.Text = text;
	});
}
