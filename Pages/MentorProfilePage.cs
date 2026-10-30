namespace HerMentor;

public sealed class MentorProfilePage : AppPage
{
	protected override AppScreen? ActiveTab => AppScreen.FindMentors;

	protected override View BuildContent()
	{
		var m = CurrentMentor;

		var s = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(18, 22, 18, 20) };
		s.Add(BackLink());
		s.Add(Avatar(m.Initials, 64));
		s.Add(T(m.Name, 17, Ink, true, true));
		s.Add(T(m.Years > 0 ? $"{m.Role} · {m.Years} yrs experience" : m.Role, 11, Muted, false, true));
		var pill = Pill($"{m.Match}% match");
		pill.HorizontalOptions = LayoutOptions.Center;
		s.Add(pill);

		var about = new VerticalStackLayout { Spacing = 6 };
		about.Add(Section("About"));
		about.Add(T(m.Bio.Length > 0 ? m.Bio : "This mentor hasn't added a bio yet.", 11, Muted));
		if (m.Tags.Length > 0)
			about.Add(ChipRow(m.Tags, false, []));
		s.Add(Card(about));

		var avail = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
		avail.Add(Section("Availability"), 0, 0);
		avail.Add(T(m.IsAvailable ? (m.Availability.Length > 0 ? m.Availability : "Available") : "Not taking mentees right now", 11, Pink), 1, 0);
		s.Add(avail);
		s.Add(GradButton("Request session", () => Go(AppScreen.BookSession)));
		s.Add(OutlineButton("Request a session to message", () => Go(AppScreen.BookSession)));
		return s;
	}
}
