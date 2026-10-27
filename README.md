# HerMentor

HerMentor is a cross-platform .NET MAUI app project for a women's mentoring
initiative.

## Requirements

- .NET 10 SDK
- .NET MAUI workload for the platform you want to run

## Run

Open `HerMentor.csproj` in Visual Studio or VS Code with the .NET MAUI
extension, select an available device or emulator, and start debugging.

The project targets Android, iOS, Mac Catalyst, and Windows where the
corresponding platform workloads are installed.

## Prototype screens

The mobile-first prototype implements each screen as a separate MAUI page,
registered with Shell navigation: welcome, login, registration, password reset,
home, mentor discovery and profiles, session booking and confirmation, session
details, messages and conversations, goals, profile editing, notifications,
and settings. The main navigation links Home, Explore, Sessions, Messages, and
Profile. Layouts are sized for mobile and adapt to wider windows.

Prototype interactions use local sample content; authentication, messaging,
bookings, and profile data are not connected to a backend.
