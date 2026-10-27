using System.Text.Json;

namespace HerMentor;

public sealed class AuthService(ApiClient api)
{
	private const string TokenKey = "hermentor.token";
	private const string UserKey = "hermentor.user";

	private bool _loaded;

	public UserDto? CurrentUser { get; private set; }

	public bool IsSignedIn => CurrentUser is not null && api.Token is not null;

	/// <summary>Raised after the user signs out so services can drop per-user state.</summary>
	public event Action? SignedOut;

	/// <summary>Restores a previous session from device storage (once per app run).</summary>
	public async Task EnsureLoadedAsync()
	{
		if (_loaded)
			return;
		_loaded = true;

		var token = await TokenStorage.GetAsync(TokenKey);
		var userJson = Preferences.Get(UserKey, "");
		if (string.IsNullOrEmpty(token) || userJson.Length == 0)
			return;

		try
		{
			CurrentUser = JsonSerializer.Deserialize<UserDto>(userJson, ApiConfig.Json);
			api.Token = CurrentUser is null ? null : token;
		}
		catch (JsonException)
		{
			Logout();
		}
	}

	public async Task<UserDto> RegisterAsync(string name, string email, string password, string role) =>
		await SaveAsync(await api.PostAsync<AuthResponse>("api/auth/register", new { name, email, password, role }));

	public async Task<UserDto> LoginAsync(string email, string password) =>
		await SaveAsync(await api.PostAsync<AuthResponse>("api/auth/login", new { email, password }));

	public void Logout()
	{
		api.Token = null;
		CurrentUser = null;
		Preferences.Remove(UserKey);
		TokenStorage.Remove(TokenKey);
		SignedOut?.Invoke();
	}

	private async Task<UserDto> SaveAsync(AuthResponse response)
	{
		_loaded = true;
		api.Token = response.Token;
		CurrentUser = response.User;
		Preferences.Set(UserKey, JsonSerializer.Serialize(response.User, ApiConfig.Json));
		await TokenStorage.SetAsync(TokenKey, response.Token);
		return response.User;
	}
}

/// <summary>Keeps the token in SecureStorage, falling back to Preferences where secure storage isn't available (e.g. unpackaged Windows).</summary>
internal static class TokenStorage
{
	public static async Task<string?> GetAsync(string key)
	{
		try
		{
			var value = await SecureStorage.Default.GetAsync(key);
			if (!string.IsNullOrEmpty(value))
				return value;
		}
		catch
		{
			// Fall back below.
		}

		var fallback = Preferences.Get(key, "");
		return fallback.Length == 0 ? null : fallback;
	}

	public static async Task SetAsync(string key, string value)
	{
		try
		{
			await SecureStorage.Default.SetAsync(key, value);
			Preferences.Remove(key);
		}
		catch
		{
			Preferences.Set(key, value);
		}
	}

	public static void Remove(string key)
	{
		try
		{
			SecureStorage.Default.Remove(key);
		}
		catch
		{
			// Nothing stored securely.
		}

		Preferences.Remove(key);
	}
}
