using Microsoft.Maui.Controls.Shapes;

namespace HerMentor;

public abstract partial class AppPage
{
	/// <summary>A chat bubble whose text can be changed later via <see cref="BubbleLabel"/> (used while an answer streams in).</summary>
	protected static Border ChatBubble(string text, bool mine) => new()
	{
		Padding = new Thickness(12, 9),
		MaximumWidthRequest = 300,
		HorizontalOptions = mine ? LayoutOptions.End : LayoutOptions.Start,
		Background = mine ? new SolidColorBrush(Purple) : new SolidColorBrush(Surface),
		Stroke = mine ? Purple : Line,
		StrokeThickness = 1,
		StrokeShape = RR(12),
		Content = T(text, 12, mine ? Colors.White : Ink)
	};

	protected static Label BubbleLabel(Border bubble) => (Label)bubble.Content!;

	protected async Task ScrollToAsync(View target)
	{
		try
		{
			if (Scroller is not null)
				await Scroller.ScrollToAsync(target, ScrollToPosition.End, false);
		}
		catch
		{
			// The view may have been rebuilt (e.g. theme change) while scrolling.
		}
	}
}
