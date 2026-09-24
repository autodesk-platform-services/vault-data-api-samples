# Vault Data API samples

## WPF Desktop Application

This sample demonstrates how to use the Vault Data API from a modern .NET WPF application.

The application includes examples for:

1. Signing in with an Autodesk ID through OAuth 2.0 Authorization Code with PKCE.
2. Connecting to a Vault Gateway and selecting a Vault.
3. Retrieving and visualizing file information.
4. Retrieving user information.
5. Reading external sync configuration, items, sync information, and tasks.
6. Creating, deleting, and resubmitting external sync tasks.

API operations display loading feedback. Vault API requests that do not complete within one minute are canceled and reported to the user.

## Requirements

- Windows
- .NET 10 SDK
- An Autodesk Platform Services application with a desktop or native OAuth callback
- Access to a Vault Gateway with the Vault Data API enabled

Visual Studio with the .NET desktop development workload can also be used to build and run the sample.

## Configuration

Application settings are stored in [`VaultDataAPISampleApp/appsettings.json`](VaultDataAPISampleApp/appsettings.json).

```json
{
  "Identity": {
    "AuthorizationEndpoint": "https://developer.api.autodesk.com/authentication/v2/authorize",
    "TokenEndpoint": "https://developer.api.autodesk.com/authentication/v2/token",
    "RedirectUri": "http://localhost:8080/",
    "Scope": "user:read data:read openid profapi:core-std-profile:read"
  },
  "Vault": {
    "ApiBaseUri": "/AutodeskDM/Services/api/vault/v2/"
  }
}
```

The configured `RedirectUri` must exactly match a callback URI registered for the Autodesk application. It must be a loopback HTTP URI without a query string or fragment.

The Autodesk application Client ID and Vault Gateway address are entered in the application window at runtime. Authentication is completed in the system browser; the sample does not embed a browser control.

## Build and run

From this directory:

```powershell
dotnet build VaultDataAPISampleApp.sln
dotnet run --project VaultDataAPISampleApp/VaultDataAPISampleApp.csproj
```

## Project organization

The UI is organized by feature:

```text
Features/
  Authentication/
  Files/
  Users/
  ExternalSync/
```

Each feature owns its views, view models, and feature-specific dialogs or resources. Shared navigation, platform integration, API clients, configuration, and session state remain outside the feature folders.

Feature registration is explicit in `App.xaml.cs`:

```csharp
builder.Services
    .AddAuthenticationFeature()
    .AddFilesSample()
    .AddUsersSample()
    .AddExternalSyncSample();
```

New sample pages should provide their own `AddXxxSample()` registration extension and register the page through `AddSamplePage<TPage>()`.
