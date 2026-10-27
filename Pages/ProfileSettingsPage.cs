namespace HerMentor;

public sealed class ProfileSettingsPage : AppPage
{
	protected override AppScreen? ActiveTab => AppScreen.ProfileSettings;

	protected override View BuildContent()
	{
		var user = Backend.Auth.CurrentUser;
		var name = user?.Name ?? "Guest";

		var s = new VerticalStackLayout { Spacing = 12, Padding = new Thickness(18, 22, 18, 20) };
		s.Add(T("Profile", 19, Ink, true));
		s.Add(Avatar(Initials(name), 60));
		s.Add(T(name, 16, Ink, true, true));
		s.Add(T(user?.Email ?? "", 11, Muted, false, true));

		var list = new VerticalStackLayout { Spacing = 0 };
		void Row(string text, Action? action = null)
		{
			if (list.Children.Count > 0)
				list.Add(new BoxView { HeightRequest = 1, Color = Line });
			var row = new Grid { Padding = new Thickness(0, 10), ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
			row.Add(T(text, 12), 0, 0);
			row.Add(Icon(IconChevronRight, 14, Muted, 2), 1, 0);
			list.Add(action == null ? row : Tap(row, action));
		}
		Row("Edit profile", () => Go(AppScreen.ProfileSetup));
		Row("Career goals", () => Go(AppScreen.Progress));
		Row("Notifications");
		Row("Privacy and safety");
		Row("Switch to mentor mode", () => Go(AppScreen.MentorDashboard));
		Row("Log out", () =>
		{
			Backend.Auth.Logout();
			GoRoot(AppScreen.Welcome);
		});
		s.Add(Card(list));

		var appearance = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
		appearance.Add(T("Appearance", 12), 0, 0);
		appearance.Add(ChipRow(["Light", "Dark"], false, [Dark ? 1 : 0], SetTheme), 1, 0);
		s.Add(Card(appearance));
		return s;
	}

	private static string Initials(string name)
	{
		var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		return parts.Length switch
		{
			0 => "?",
			1 => parts[0][..1].ToUpperInvariant(),
			_ => $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
		};
	}
}
