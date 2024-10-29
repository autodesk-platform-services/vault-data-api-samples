
# Vault Data API
The Vault Data API can be accessed via a Vault server address or a Vault Gateway address. Both Autodesk ID 3-legged tokens and Vault tokens are acceptable.

To get the available API endpoints, please check out the Swagger spec file (YML format) which could be fetched from http://{server-url}/AutodeskDM/Services/api/vault/v2/openapi-spec.yml 

Note: For visual representation of the API endpoints, tools like https://editor.swagger.io/ could be used.

## Here are some examples of HTTP requests:
Example of getting user information by ID using a Vault token (eg. Bearer V:ce676ccb-01dd-41c0-8466-fb47701f5263) from a Vault server:
```sh
curl -X GET http://<server-url>/AutodeskDM/Services/api/vault/v2/users/9 -H "Accept: application/json" -H "Authorization: {Vault token}"
```
Example of getting user information by ID using an Autodesk ID 3-Legged Token from Vault Gateway:
```sh
curl -X GET https://******.vg.autodesk.com/AutodeskDM/Services/api/vault/v2/users/9 -H "Accept: application/json" -H "Authorization: Bearer eyJhbGciOiJSUzI1NiIsImtpZCI6IlU3c0dGRldUTzlBekNhSzBqZURRM2dQZXBURVdWN2VhIn0******"
```

## How can you obtain an Autodesk ID 3-legged token?

To do this, create a new Autodesk Platform Services (APS) application. This will provide you with a Client ID. You can then [Get a 3-Legged Token with Authorization Code Grant (PKCE)](https://aps.autodesk.com/en/docs/oauth/v2/tutorials/get-3-legged-token-pkce/) 

### Setting Up Your Autodesk Platform Services Application

Visit [this page](https://aps.autodesk.com/myapps/) to find your existing application or create a new one. Please select "Desktop, Mobile, Single-Page App". Upon the creation of an application, the client ID can be obtained, which will be necessary for API authentication. 


### Setting up CORS
Note: If you encounter CORS error when using our API, CORS could be enabled by updating the web.config in IIS as follows:

In web.config, Look for `<server>` under `<connectivity.web>` and create new `<restapi>` element as follows:-
```
    <server .... >
        <restapi>
            <cors enabled="true" origins="http://servera,https://serverb" />
        </restapi>
    </server>
```

origins -> Comma-separated list of origins that are allowed for cross-origin requests.

For more info on CORS: https://learn.microsoft.com/en-us/aspnet/web-api/overview/security/enabling-cross-origin-requests-in-web-api
