using System.Net;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace HerMentor;

/// <summary>Talks to the AI assistant hub and streams answers token by token. Also keeps the current chat so it survives page rebuilds.</summary>
public sealed class AssistantService
{
	private readonly ApiClient _api;
	private readonly SemaphoreSlim _gate = new(1, 1);
	private HubConnection? _hub;
	private string? _connectedToken;

	public AssistantService(ApiClient api, AuthService auth)
	{
		_api = api;
		auth.SignedOut += Reset;
	}

	/// <summary>The career chat so far (completed turns only).</summary>
	public List<ChatTurn> CareerChat { get; } = [];

	public IAsyncEnumerable<string> AskAsync(
		string question,
		IReadOnlyList<ChatTurn> history,
		CancellationToken ct = default) =>
		StreamAsync("Ask", [question, history], ct);

	public IAsyncEnumerable<string> PlanAsync(string roleId, CancellationToken ct = default) =>
		StreamAsync("Plan", [roleId], ct);

	private async IAsyncEnumerable<string> StreamAsync(
		string method,
		object?[] args,
		[EnumeratorCancellation] CancellationToken ct)
	{
		var hub = await ConnectAsync(ct);
		var stream = hub.StreamAsyncCore<string>(method, args, ct);
		await using var enumerator = stream.GetAsyncEnumerator(ct);

		while (true)
		{
			bool hasNext;
			string item = "";
			try
			{
				hasNext = await enumerator.MoveNextAsync();
				if (hasNext)
					item = enumerator.Current;
			}
			catch (HubException ex)
			{
				throw new ApiException(CleanMessage(ex.Message), 0);
			}

			if (!hasNext)
				yield break;

			yield return item;
		}
	}

	private async Task<HubConnection> ConnectAsync(CancellationToken ct)
	{
		await _gate.WaitAsync(ct);
		try
		{
			// A different user (or a refreshed token) needs a fresh connection.
			if (_hub is not null && _connectedToken != _api.Token)
			{
				await _hub.DisposeAsync();
				_hub = null;
			}

			_hub ??= new HubConnectionBuilder()
				.WithUrl($"{ApiConfig.BaseUrl}/hubs/assistant", o => o.AccessTokenProvider = () => Task.FromResult(_api.Token))
				.WithAutomaticReconnect()
				.Build();

			if (_hub.State == HubConnectionState.Disconnected)
			{
				try
				{
					await _hub.StartAsync(ct);
					_connectedToken = _api.Token;
				}
				catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
				{
					throw new ApiException("Your session has expired. Please log in again.", 401);
				}
				catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
				{
					throw new ApiException("Can't reach the HerMentor server. Check your connection and that the API is running.", 0);
				}
			}

			return _hub;
		}
		finally
		{
			_gate.Release();
		}
	}

	private void Reset()
	{
		CareerChat.Clear();
		var hub = _hub;
		_hub = null;
		if (hub is not null)
			_ = hub.DisposeAsync().AsTask();
	}

	// SignalR prefixes server errors; keep only the message we wrote.
	private static string CleanMessage(string message)
	{
		const string marker = "HubException: ";
		var i = message.IndexOf(marker, StringComparison.Ordinal);
		return i >= 0 ? message[(i + marker.Length)..] : "The AI assistant is unavailable right now. Please try again.";
	}
}
