using Microsoft.AspNetCore.SignalR.Client;

namespace HerMentor;

/// <summary>
/// One shared SignalR connection. Pages call <see cref="AcquireAsync"/> when they appear and <see cref="ReleaseAsync"/> when they leave;
/// the connection stays open while at least one page needs it.
/// </summary>
public sealed class RealtimeService(ApiClient api)
{
	private readonly SemaphoreSlim _gate = new(1, 1);
	private HubConnection? _hub;
	private int _users;

	public bool IsConnected => _hub?.State == HubConnectionState.Connected;

	/// <summary>Raised (on a background thread) when any mentor is added or updated.</summary>
	public event Action? MentorsChanged;

	/// <summary>Raised (on a background thread) when the connection goes up or down.</summary>
	public event Action<bool>? ConnectionChanged;

	public async Task AcquireAsync()
	{
		await _gate.WaitAsync();
		try
		{
			_users++;
			if (_hub is { State: not HubConnectionState.Disconnected })
				return;

			_hub ??= Build();
			try
			{
				await _hub.StartAsync();
				ConnectionChanged?.Invoke(true);
			}
			catch
			{
				// Server offline: pages still work from plain requests; the next page visit retries.
				ConnectionChanged?.Invoke(false);
			}
		}
		finally
		{
			_gate.Release();
		}
	}

	public async Task ReleaseAsync()
	{
		await _gate.WaitAsync();
		try
		{
			_users = Math.Max(0, _users - 1);
			if (_users == 0 && _hub is not null)
			{
				await _hub.StopAsync();
				ConnectionChanged?.Invoke(false);
			}
		}
		catch
		{
			// Already stopped.
		}
		finally
		{
			_gate.Release();
		}
	}

	private HubConnection Build()
	{
		var hub = new HubConnectionBuilder()
			.WithUrl($"{ApiConfig.BaseUrl}/hubs/mentors", o => o.AccessTokenProvider = () => Task.FromResult(api.Token))
			.WithAutomaticReconnect()
			.Build();

		hub.On("MentorsChanged", () => MentorsChanged?.Invoke());
		hub.Reconnecting += _ => { ConnectionChanged?.Invoke(false); return Task.CompletedTask; };
		hub.Reconnected += _ => { ConnectionChanged?.Invoke(true); MentorsChanged?.Invoke(); return Task.CompletedTask; };
		hub.Closed += _ => { ConnectionChanged?.Invoke(false); return Task.CompletedTask; };
		return hub;
	}
}
