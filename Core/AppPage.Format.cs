using System.Globalization;

namespace HerMentor;

public abstract partial class AppPage
{
	/// <summary>Session times arrive in UTC; people read them in local time.</summary>
	protected static string FormatWhen(DateTime utc) =>
		utc.ToLocalTime().ToString("ddd d MMM, HH:mm", CultureInfo.InvariantCulture);

	protected static Editor TextBox(string placeholder, string? text, int height = 80)
	{
		return new Editor
		{
			Placeholder = placeholder,
			Text = text,
			FontSize = 12,
			TextColor = Ink,
			PlaceholderColor = Placeholder,
			BackgroundColor = Colors.Transparent,
			HeightRequest = height,
			MaxLength = 600
		};
	}
}
