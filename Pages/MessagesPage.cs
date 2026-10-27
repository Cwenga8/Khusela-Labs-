namespace HerMentor;

public sealed class MessagesPage : AppPage
{
	protected override AppScreen? ActiveTab => AppScreen.SessionOutcomes;

	protected override View BuildContent()
	{
		var s = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(18, 22, 18, 20) };
		s.Add(BackLink());
		var head = new VerticalStackLayout { Spacing = 2 };
		head.Add(T(CurrentMentor.Name, 17, Ink, true));
		head.Add(T("Mentor · online", 11, Muted));
		s.Add(head);

		var thread = new VerticalStackLayout { Spacing = 8 };
		void Add(string text, bool mine) => thread.Add(Bubble(text, mine));
		Add("Hi Naledi, thanks for accepting my request!", true);
		Add("Welcome, Thandi. Share your portfolio link before Thursday.", false);
		Add("Will do. I also prepared questions with the AI tool.", true);
		Add("Great, see you then.", false);
		s.Add(thread);

		var quick = new HorizontalStackLayout { Spacing = 6 };
		foreach (var text in new[] { "Share portfolio", "Reschedule", "Thank you" })
			quick.Add(Tap(Chip(text, false).Chip, () => Add(text, true)));
		s.Add(quick);

		var input = new Entry
		{
			Placeholder = "Type a message...",
			FontSize = 12,
			TextColor = Ink,
			PlaceholderColor = Placeholder,
			BackgroundColor = Colors.Transparent
		};
		void Send()
		{
			if (string.IsNullOrWhiteSpace(input.Text))
				return;
			Add(input.Text.Trim(), true);
			input.Text = string.Empty;
		}
		input.Completed += (_, _) => Send();
		var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 8 };
		row.Add(input, 0, 0);
		row.Add(Tap(IconCircle(IconSend, 28), Send), 1, 0);
		s.Add(Card(row, 6));
		return s;
	}

	private static Border Bubble(string text, bool mine) => new()
	{
		Padding = new Thickness(12, 9),
		MaximumWidthRequest = 280,
		HorizontalOptions = mine ? LayoutOptions.End : LayoutOptions.Start,
		Background = mine ? new SolidColorBrush(Purple) : new SolidColorBrush(Surface),
		Stroke = mine ? Purple : Line,
		StrokeThickness = 1,
		StrokeShape = RR(12),
		Content = T(text, 12, mine ? Colors.White : Ink)
	};
}
