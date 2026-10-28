# HerMentor

HerMentor is a cross-platform .NET MAUI app project for a women's mentoring
initiative.

## Projects

- `HerMentor.csproj` is the MAUI client app.
- `HerMentor.Api/HerMentor.Api.csproj` is the standalone ASP.NET Core API. Its
  source is excluded from the MAUI app build, so it can be run and published
  independently.
- `HerMentor.slnx` contains both projects for convenient IDE access; it does not
  combine them into one deployable project.

## Requirements

- .NET 10 SDK
- .NET MAUI workload for the platform you want to run

## Run the app

Open `HerMentor.csproj` in Visual Studio or VS Code with the .NET MAUI
extension, select an available device or emulator, and start debugging.

The project targets Android, iOS, Mac Catalyst, and Windows where the
corresponding platform workloads are installed.

## Run the API

Run the API from the repository root in a separate terminal:

```sh
dotnet user-secrets set "Jwt:Key" "<random string of at least 32 bytes>" --project HerMentor.Api/HerMentor.Api.csproj
dotnet run --project HerMentor.Api
```

The local API listens on `http://localhost:5080` (Android emulator clients use
`http://10.0.2.2:5080`). To publish the API independently:

```sh
dotnet publish HerMentor.Api/HerMentor.Api.csproj --configuration Release --output ./publish/api
```

Set `Jwt:Key` in the hosting environment before starting the published API. To
point the app at a hosted API, set its `api_url` preference to the hosted base
URL (for example, `https://api.example.com`).

## App behavior

The mobile-first app implements its screens as separate MAUI pages and uses
the API for authentication, mentor discovery, profiles, sessions, career
guidance, and messaging. The main navigation links Home, Explore, Sessions,
Messages, and Profile.
