namespace HerMentor;

public sealed class SkillGapPage : AppPage
{
	/// <summary>Role picked on Career Guidance; null means "best fit".</summary>
	public static string? SelectedRole { get; set; }

	private VerticalStackLayout? _body;
	private VerticalStackLayout? _plan;
	private CancellationTokenSource? _cts;
	private bool _planning;

	protected override AppScreen? ActiveTab => AppScreen.CareerGuidance;

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_cts?.Cancel();
	}

	protected override View BuildContent()
	{
		var s = Begin("Skill Gap Analysis", "Comparing your skills with your target role", back: true);
		_body = new VerticalStackLayout { Spacing = 12 };
		s.Add(_body);
		_ = LoadAsync();
		return s;
	}

	private async Task LoadAsync()
	{
		var body = _body;
		if (body is null)
			return;

		_cts?.Cancel();
		var cts = _cts = new CancellationTokenSource();
		body.Children.Clear();
		body.Add(T("Analysing your skills...", 12, Muted));

		try
		{
			var gap = await Backend.Career.GetGapAsync(SelectedRole, cts.Token);
			if (cts.IsCancellationRequested || body != _body)
				return;

			body.Children.Clear();
			if (gap.NeedsProfile)
			{
				var card = new VerticalStackLayout { Spacing = 10 };
				card.Add(T("Add your skills and interests so we can compare them with your target role.", 12, Ink));
				card.Add(GradButton("Build my profile", () => Go(AppScreen.ProfileSetup), 40));
				body.Add(Card(card, 14));
				return;
			}

			Render(body, gap);
		}
		catch (OperationCanceledException)
		{
			// Page closed or reloaded.
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
		}
		catch (Exception ex) when (!cts.IsCancellationRequested && body == _body)
		{
			body.Children.Clear();
			body.Add(T(ErrorText(ex), 12, Pink));
			body.Add(OutlineButton("Try again", () => _ = LoadAsync(), Line, 38));
		}
	}

	private void Render(VerticalStackLayout body, SkillGapResponse gap)
	{
		body.Add(Section("Target role"));
		var roles = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
		foreach (var role in gap.Roles)
		{
			var (chip, _) = Chip(role.Name, role.Id == gap.RoleId);
			chip.Margin = new Thickness(0, 0, 6, 6);
			var id = role.Id;
			roles.Add(Tap(chip, () =>
			{
				if (id == gap.RoleId)
					return;
				SelectedRole = id;
				_ = LoadAsync();
			}));
		}
		body.Add(roles);

		var summary = new VerticalStackLayout { Spacing = 6 };
		var head = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
		head.Add(T($"Skills for {gap.RoleName}", 12, Ink, true), 0, 0);
		head.Add(T($"{gap.Coverage}%", 11, Muted), 1, 0);
		summary.Add(head);
		summary.Add(Bar(gap.Coverage));
		foreach (var skill in gap.Skills)
		{
			var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
			row.Add(T(skill.Name, 12, Ink), 0, 0);
			row.Add(T(skill.Have ? "You have this" : "To build", 11, skill.Have ? Muted : Pink, !skill.Have), 1, 0);
			summary.Add(row);
		}
		body.Add(Card(summary));

		body.Add(Section("Recommended next"));
		foreach (var step in gap.NextSteps)
			body.Add(Card(T(step, 12)));

		body.Add(Section("Your 4-week plan"));
		_plan = new VerticalStackLayout { Spacing = 8 };
		_plan.Add(T("Get a personal plan from HerMentor AI based on the skills you still need.", 11, Muted));
		_plan.Add(OutlineButton("Generate my plan", () => _ = PlanAsync(gap.RoleId), Line, 40));
		body.Add(_plan);

		body.Add(GradButton("Find a mentor for these skills", () => GoRoot(AppScreen.FindMentors)));
	}

	private async Task PlanAsync(string roleId)
	{
		if (_planning || _plan is not { } box)
			return;

		_planning = true;
		box.Children.Clear();
		var label = T("Writing your plan...", 12, Ink, false, true);
		box.Add(Card(label));

		var text = new System.Text.StringBuilder();
		try
		{
			await foreach (var chunk in Backend.Assistant.PlanAsync(roleId))
			{
				text.Append(chunk);
				label.Text = Tidy(text.ToString());
			}

			if (text.Length == 0)
				label.Text = "The AI didn't return a plan. Please try again.";
		}
		catch (ApiException ex) when (ex.IsUnauthorized)
		{
			SessionExpired();
		}
		catch (Exception ex)
		{
			label.TextColor = Pink;
			label.Text = ErrorText(ex);
		}
		finally
		{
			_planning = false;
			if (box == _plan)
				box.Add(OutlineButton("Regenerate", () => _ = PlanAsync(roleId), Line, 38));
		}
	}

	private static string Tidy(string text) =>
		text.Replace("**", "").Replace("__", "").Replace("# ", "").Trim();
}
