# Vault Data API

## Example: Power BI custom connector

This application demonstrates how to use a custom PowerBI connector to retrieve data with the Vault Data API. 
This application illustrates:
1. Logging in with AutodeskID
2. Selecting "GetUsers" or "GetFiles" API to query the data
3. Formatting the data to generate a PowerBI query

### Build Environment

You will need Visual Studio Code to build this project.

Before you build the connector, please:

1. Install the "Power Query SDK" extensions for VS Code.
2. Change the `client_id` and `redirect_uri` values in the `VaultDataAPIPowerBIConnector.pq` file.
3. Update the `vault_id` value to the vault you want to query. You can get the vault ID using the `Get Vaults (vault/v2/vaults)` endpoint.

Example code:

```python
client_id = "RnDabRYTESTAPPVjVh6WHs8tL66pThOpfIG2QhQFVA";
redirect_uri = "http://localhost:8080/";
vault_id = "101";
```

Run the build task from terminal to generate the VaultDataAPIPowerBIConnector.mez file


### Load this Custom Connector in PowerBI Desktop

1) After a successful build, you will find the file `VaultDataAPIPowerBIConnector.mez` in the `.\bin\AnyCPU\Debug\` folder. Place this file into the folder: `C:\Users\***\Documents\Microsoft Power BI Desktop\Custom Connectors`. If the folder doesn't exist, create it. 
2) Additional step to allow any third party connector without validation will be required to load the sample connector and restart of PowerBI will be required after making the change.
`Check the option "(Not Recommended) Allow any extension to load without validation or warning in Power BI Desktop (under File | Options and settings | Options | Security | Data Extensions)."` 

After completing the above steps, you should then be able find the sample custom connector in PowerBI's GetData dialog.
