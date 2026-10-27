namespace HerMentor;

public sealed class ProgressPage : AppPage
{
	protected override AppScreen? ActiveTab => AppScreen.Home;

	protected override View BuildContent()
	{
		var s = Begin("My Progress", "Track goals, skills and sessions", back: true);
		var stats = new Grid { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
		var data = new[] { ("6", "Sessions"), ("3/5", "Goals"), ("+4", "Skills") };
		for (var i = 0; i < data.Length; i++)
		{
			var box = new VerticalStackLayout { Spacing = 2 };
			box.Add(T(data[i].Item1, 17, Ink, true, true));
			box.Add(T(data[i].Item2, 10, Muted, false, true));
			stats.Add(Card(box), i, 0);
		}
		s.Add(stats);
		s.Add(Section("Goals"));
		s.Add(LabeledBar("Build portfolio", 70, true));
		s.Add(LabeledBar("Learn cloud basics", 40, true));
		s.Add(LabeledBar("Apply to 10 roles", 30, true));
		s.Add(Section("Milestones"));
		var milestones = new VerticalStackLayout { Spacing = 8 };
		milestones.Add(CheckRow("Completed profile", true, 11));
		milestones.Add(CheckRow("First mentor session", true, 11));
		milestones.Add(CheckRow("Portfolio v1 reviewed", false, 11));
		s.Add(Card(milestones));
		return s;
	}
}
