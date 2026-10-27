using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace HerMentor;

public abstract partial class AppPage
{
	protected static Brush Gradient => new LinearGradientBrush(
		new GradientStopCollection { new GradientStop(Pink, 0f), new GradientStop(Purple, 1f) },
		new Point(0, 0), new Point(1, 0));

	protected static RoundRectangle RR(double r) => new() { CornerRadius = new CornerRadius(r) };

	protected static TView Tap<TView>(TView view, Action action) where TView : View
	{
		view.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(action) });
		return view;
	}

	protected static Label T(string text, double size = 13, Color? color = null, bool bold = false, bool center = false) => new()
	{
		Text = text,
		FontSize = size,
		TextColor = color ?? Ink,
		FontFamily = bold ? "OpenSansSemibold" : "OpenSansRegular",
		HorizontalTextAlignment = center ? TextAlignment.Center : TextAlignment.Start,
		VerticalOptions = LayoutOptions.Center
	};

	protected static VerticalStackLayout Begin(string title, string? subtitle = null, bool back = false, double spacing = 12)
	{
		var s = new VerticalStackLayout { Spacing = spacing, Padding = new Thickness(18, 22, 18, 20) };
		if (back)
			s.Add(BackLink());
		var head = new VerticalStackLayout { Spacing = 3 };
		head.Add(T(title, 19, Ink, true));
		if (subtitle != null)
			head.Add(T(subtitle, 12, Muted));
		s.Add(head);
		return s;
	}

	protected static Label Section(string text) => T(text, 13, Ink, true);

	protected static Border Card(View content, double pad = 12, Color? bg = null, Color? stroke = null) => new()
	{
		Padding = new Thickness(pad),
		Background = bg ?? Surface,
		Stroke = stroke ?? Line,
		StrokeThickness = 1,
		StrokeShape = RR(10),
		Content = content
	};

	protected static Border GradButton(string text, Action action, double height = 44) => Tap(new Border
	{
		HeightRequest = height,
		Background = Gradient,
		StrokeThickness = 0,
		StrokeShape = RR(10),
		Content = T(text, 13, Colors.White, true, true)
	}, action);

	protected static Border OutlineButton(string text, Action action, Color? stroke = null, double height = 44) => Tap(new Border
	{
		HeightRequest = height,
		Background = Bg,
		Stroke = stroke ?? Pink,
		StrokeThickness = 1,
		StrokeShape = RR(10),
		Content = T(text, 13, Ink, true, true)
	}, action);

	protected static Border Pill(string text) => new()
	{
		Padding = new Thickness(8, 3),
		Background = PillBg,
		StrokeThickness = 0,
		StrokeShape = RR(10),
		VerticalOptions = LayoutOptions.Center,
		Content = T(text, 10, PillInk, true)
	};

	protected static View Icon(string data, double size, Color color, double stroke = 1.8) => new Microsoft.Maui.Controls.Shapes.Path
	{
		Data = (Geometry)new PathGeometryConverter().ConvertFromInvariantString(data)!,
		Stroke = new SolidColorBrush(color),
		StrokeThickness = stroke,
		StrokeLineCap = PenLineCap.Round,
		StrokeLineJoin = PenLineJoin.Round,
		Aspect = Stretch.Uniform,
		WidthRequest = size,
		HeightRequest = size,
		HorizontalOptions = LayoutOptions.Center,
		VerticalOptions = LayoutOptions.Center
	};

	protected static View BackLink() => Tap(new HorizontalStackLayout
	{
		Spacing = 4,
		Children = { Icon(IconChevronLeft, 14, Muted, 2), T("Back", 12, Muted) }
	}, GoBack);

	protected static View CheckRow(string text, bool done, double size = 12)
	{
		var row = new HorizontalStackLayout { Spacing = 10 };
		row.Add(Icon(done ? IconCheckSquare : IconSquare, 15, done ? Pink : Muted));
		row.Add(T(text, size, size > 11 ? Ink : Muted));
		return row;
	}

	protected static Border IconCircle(string data, double size) => new()
	{
		WidthRequest = size,
		HeightRequest = size,
		Background = Gradient,
		StrokeThickness = 0,
		StrokeShape = RR(size / 2),
		HorizontalOptions = LayoutOptions.Center,
		VerticalOptions = LayoutOptions.Center,
		Content = Icon(data, size * 0.5, Colors.White, 2)
	};

	protected static Border Avatar(string initials, double size) => new()
	{
		WidthRequest = size,
		HeightRequest = size,
		Background = Gradient,
		StrokeThickness = 0,
		StrokeShape = RR(size / 2),
		HorizontalOptions = LayoutOptions.Center,
		VerticalOptions = LayoutOptions.Center,
		Content = T(initials, size * 0.36, Colors.White, true, true)
	};

	protected static View Bar(int pct, double height = 6)
	{
		var fill = new Border
		{
			Background = Gradient,
			StrokeThickness = 0,
			StrokeShape = RR(height / 2)
		};
		var grid = new Grid
		{
			ColumnDefinitions =
			{
				new ColumnDefinition(new GridLength(Math.Max(pct, 1), GridUnitType.Star)),
				new ColumnDefinition(new GridLength(Math.Max(100 - pct, 0), GridUnitType.Star))
			}
		};
		grid.Add(fill, 0, 0);
		return new Border
		{
			HeightRequest = height,
			Background = Line,
			StrokeThickness = 0,
			StrokeShape = RR(height / 2),
			Content = grid
		};
	}

	protected static View LabeledBar(string label, int pct, bool boxed = false)
	{
		var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
		row.Add(T(label, 12, Ink, boxed), 0, 0);
		row.Add(T($"{pct}%", 11, Muted), 1, 0);
		var stack = new VerticalStackLayout { Spacing = 6 };
		stack.Add(row);
		stack.Add(Bar(pct));
		return boxed ? Card(stack) : stack;
	}

	protected static Entry AddField(VerticalStackLayout parent, string label, string placeholder, bool password = false, string? text = null)
	{
		var entry = new Entry
		{
			Placeholder = placeholder,
			Text = text,
			IsPassword = password,
			FontSize = 13,
			TextColor = Ink,
			PlaceholderColor = Placeholder,
			BackgroundColor = Colors.Transparent
		};
		var group = new VerticalStackLayout { Spacing = 4 };
		group.Add(T(label, 11, Muted));
		group.Add(new Border
		{
			Background = Surface,
			Stroke = Line,
			StrokeThickness = 1,
			StrokeShape = RR(8),
			Padding = new Thickness(8, 0),
			Content = entry
		});
		parent.Add(group);
		return entry;
	}

	protected static (Border Chip, Action<bool> Set) Chip(string text, bool selected)
	{
		var label = T(text, 11, Muted, false, true);
		var chip = new Border
		{
			Padding = new Thickness(11, 6),
			StrokeThickness = 1,
			StrokeShape = RR(14),
			Content = label
		};
		void Set(bool on)
		{
			chip.Background = on ? Gradient : new SolidColorBrush(Surface);
			chip.Stroke = on ? Purple : Line;
			label.TextColor = on ? Colors.White : Muted;
		}
		Set(selected);
		return (chip, Set);
	}

	protected static View ChipRow(string[] items, bool multi, int[] selected, Action<string>? onSelect = null)
	{
		var flex = new FlexLayout { Wrap = FlexWrap.Wrap };
		var setters = new List<Action<bool>>();
		for (var i = 0; i < items.Length; i++)
		{
			var index = i;
			var on = selected.Contains(i);
			var (chip, set) = Chip(items[i], on);
			chip.Margin = new Thickness(0, 0, 6, 6);
			setters.Add(set);
			Tap(chip, () =>
			{
				if (multi)
				{
					on = !on;
					set(on);
				}
				else
				{
					for (var j = 0; j < setters.Count; j++)
						setters[j](j == index);
				}
				onSelect?.Invoke(items[index]);
			});
			flex.Add(chip);
		}
		return flex;
	}

	protected static View TabBar(AppScreen active)
	{
		var tabs = new (string Name, string Icon, AppScreen Target)[]
		{
			("Home", IconHome, AppScreen.Home),
			("Career", IconBriefcase, AppScreen.CareerGuidance),
			("Mentors", IconUsers, AppScreen.FindMentors),
			("Sessions", IconCalendar, AppScreen.SessionOutcomes),
			("Profile", IconUser, AppScreen.ProfileSettings)
		};
		var grid = new Grid { Padding = new Thickness(4, 8, 4, 10), BackgroundColor = Bg };
		for (var i = 0; i < tabs.Length; i++)
		{
			var tab = tabs[i];
			var color = tab.Target == active ? Pink : Muted;
			grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			var item = new VerticalStackLayout { Spacing = 2, HorizontalOptions = LayoutOptions.Center };
			item.Add(Icon(tab.Icon, 20, color));
			item.Add(T(tab.Name, 9, color, false, true));
			Tap(item, () =>
			{
				if (tab.Target != active)
					GoRoot(tab.Target);
			});
			grid.Add(item, i, 0);
		}
		var bar = new VerticalStackLayout { Spacing = 0 };
		bar.Add(new BoxView { HeightRequest = 1, Color = Line });
		bar.Add(grid);
		return bar;
	}

	protected static View MentorCard(Mentor m)
	{
		var grid = new Grid
		{
			ColumnSpacing = 10,
			RowSpacing = 6,
			ColumnDefinitions =
			{
				new ColumnDefinition(GridLength.Auto),
				new ColumnDefinition(GridLength.Star),
				new ColumnDefinition(GridLength.Auto)
			}
		};
		grid.Add(Avatar(m.Initials, 38), 0, 0);
		var info = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
		info.Add(T(m.Name, 13, Ink, true));
		info.Add(T(m.Role, 10, Muted));
		grid.Add(info, 1, 0);
		grid.Add(Pill($"{m.Match}% match"), 2, 0);
		var tags = new HorizontalStackLayout { Spacing = 14 };
		foreach (var tag in m.Tags)
			tags.Add(T(tag, 10, Muted));
		grid.Add(tags, 1, 1);
		return Tap(Card(grid), () =>
		{
			CurrentMentor = m;
			Go(AppScreen.MentorProfile);
		});
	}
}
