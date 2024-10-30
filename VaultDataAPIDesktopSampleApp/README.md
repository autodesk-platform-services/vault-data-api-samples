# Vault Data API samples

## Example: WPF Desktop Application
This sample demonstrates how to use the Vault Data API with a C# desktop application.
This application illustrates:

1. Logging in with the user's Autodesk ID.
2. Connecting to the Vault Gateway address and selecting a Vault.
3. Retrieving File or User information via the Vault Data API.
4. Analyzing data and generating a chart.

### Build Environment

You will need Visual Studio 2022 to build this project.

### Configuration

Users can configure certain values in the `app.config` file. Please ensure that the `RedirectUri` value matches your Autodesk app's redirect URI.
Example code:

```python
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <appSettings>
    <add key="GetAccessTokenUrl" value="https://developer.api.autodesk.com/authentication/v2/token" />
    <add key="LoginUrl" value="https://developer.api.autodesk.com/authentication/v2/authorize" />
    <add key="RedirectUri" value="http://localhost:8080/"/>
    <add key="ApiBaseUri" value="/AutodeskDM/Services/api/vault/v2/"/>
  </appSettings>
</configuration>
```