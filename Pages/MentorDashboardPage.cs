namespace HerMentor;

public sealed class MentorDashboardPage : AppPage
{
	protected override AppScreen? ActiveTab => AppScreen.Home;

	protected override View BuildContent()
	{
		var s = Begin("Mentor Dashboard", "Manage requests and sessions", back: true);
		var stats = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
		var data = new[] { ("2", "Requests"), ("5", "Sessions"), ("12", "Mentees") };
		for (var i = 0; i < data.Length; i++)
		{
			var box = new VerticalStackLayout { Spacing = 2 };
			box.Add(T(data[i].Item1, 17, Ink, true, true));
			box.Add(T(data[i].Item2, 10, Muted, false, true));
			stats.Add(Card(box), i, 0);
		}
		s.Add(stats);
		s.Add(Section("New requests"));
		s.Add(RequestCard("TN", "Thandi Nkosi", "Re-skilling · Tech"));
		s.Add(RequestCard("ZM", "Zinhle Mabasa", "Career switch · Design"));
		var avail = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
		avail.Add(Section("Availability"), 0, 0);
		avail.Add(T("Edit", 11, Pink), 1, 0);
		s.Add(avail);
		return s;
	}

	private static View RequestCard(string initials, string name, string note)
	{
		var stack = new VerticalStackLayout { Spacing = 10 };
		var head = new Grid { ColumnSpacing = 10, ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
		head.Add(Avatar(initials, 36), 0, 0);
		var info = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
		info.Add(T(name, 13, Ink, true));
		info.Add(T(note, 10, Muted));
		head.Add(info, 1, 0);
		stack.Add(head);
		var buttons = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
		buttons.Add(GradButton("Accept", () => { }, 36), 0, 0);
		buttons.Add(OutlineButton("Decline", () => { }, Pink, 36), 1, 0);
		stack.Add(buttons);
		return Card(stack);
	}
}
