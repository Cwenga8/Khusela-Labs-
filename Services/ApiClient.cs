using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HerMentor;

public sealed class ApiException(string message, int statusCode) : Exception(message)
{
	public int StatusCode { get; } = statusCode;
	public bool IsUnauthorized => StatusCode == 401;
}

/// <summary>Thin JSON-over-HTTP client that attaches the bearer token and turns failures into <see cref="ApiException"/>.</summary>
public sealed class ApiClient
{
	private readonly HttpClient _http = new() { BaseAddress = new Uri(ApiConfig.BaseUrl), Timeout = TimeSpan.FromSeconds(15) };

	public string? Token { get; set; }

	public Task<T> GetAsync<T>(string path, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Get, path, null, ct);

	public Task<T> PostAsync<T>(string path, object body, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Post, path, body, ct);

	public Task<T> PutAsync<T>(string path, object body, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Put, path, body, ct);

	public Task<T> DeleteAsync<T>(string path, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Delete, path, null, ct);

	private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
	{
		using var request = new HttpRequestMessage(method, path);
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		if (Token is not null)
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
		if (body is not null)
			request.Content = JsonContent.Create(body, options: ApiConfig.Json);

		HttpResponseMessage response;
		try
		{
			response = await _http.SendAsync(request, ct);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			throw;
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
		{
			throw new ApiException("Can't reach the HerMentor server. Check your connection and that the API is running.", 0);
		}

		using (response)
		{
			if (response.IsSuccessStatusCode)
				return (await response.Content.ReadFromJsonAsync<T>(ApiConfig.Json, ct))!;

			throw new ApiException(await ReadErrorAsync(response, ct), (int)response.StatusCode);
		}
	}

	private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
	{
		try
		{
			var error = await response.Content.ReadFromJsonAsync<ApiError>(ApiConfig.Json, ct);
			if (!string.IsNullOrWhiteSpace(error?.Error))
				return error.Error;
		}
		catch
		{
			// Body wasn't our error shape; fall through to the generic message.
		}

		return (int)response.StatusCode switch
		{
			401 => "Your session has expired. Please log in again.",
			405 => "The server does not support this request method. Check that the API route is enabled.",
			406 => "The server cannot return the requested data format. Check that the API supports JSON responses.",
			429 => "Too many attempts. Please wait a minute and try again.",
			var code => $"Something went wrong (error {code})."
		};
	}
}
