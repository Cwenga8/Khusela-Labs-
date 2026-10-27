using Microsoft.Maui.Controls.Shapes;

namespace HerMentor;

/// <summary>
/// Base class for every screen. A page only implements <see cref="BuildContent"/>
/// (and optionally <see cref="ActiveTab"/>); theming, navigation and shared widgets live in the partial files of this class.
/// </summary>
public abstract partial class AppPage : ContentPage
{
	private const double PhoneWidth = 440;

	private bool _built;
	private bool _builtDark;

	protected AppPage()
	{
		Shell.SetNavBarIsVisible(this, false);
		Title = GetType().Name.Replace("Page", string.Empty);

		Loaded += (_, _) =>
		{
			if (Application.Current is { } app)
				app.RequestedThemeChanged += OnThemeChanged;
		};
		Unloaded += (_, _) =>
		{
			if (Application.Current is { } app)
				app.RequestedThemeChanged -= OnThemeChanged;
		};
	}

	/// <summary>The scrollable body of the screen.</summary>
	protected abstract View BuildContent();

	/// <summary>The page's scroll container, for scrolling to new content (e.g. chat messages).</summary>
	protected ScrollView? Scroller { get; private set; }

	/// <summary>The bottom tab to highlight; null hides the tab bar.</summary>
	protected virtual AppScreen? ActiveTab => null;

	protected override void OnAppearing()
	{
		base.OnAppearing();
		if (!_built || _builtDark != Dark)
			Rebuild();
	}

	private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) =>
		MainThread.BeginInvokeOnMainThread(Rebuild);

	/// <summary>Rebuilds the page content, e.g. after async data has arrived.</summary>
	protected void Refresh() => MainThread.BeginInvokeOnMainThread(Rebuild);

	private void Rebuild()
	{
		_built = true;
		_builtDark = Dark;
		BackgroundColor = Bg;
		Content = BuildRoot();
	}

	private View BuildRoot()
	{
		var root = new Grid
		{
			MaximumWidthRequest = PhoneWidth,
			HorizontalOptions = LayoutOptions.Center,
			BackgroundColor = Bg,
			RowDefinitions =
			{
				new RowDefinition(GridLength.Star),
				new RowDefinition(GridLength.Auto)
			}
		};
		root.Add(Scroller = new ScrollView { Content = BuildContent(), VerticalScrollBarVisibility = ScrollBarVisibility.Never }, 0, 0);
		if (ActiveTab is { } tab)
			root.Add(TabBar(tab), 0, 1);
		return root;
	}
}
