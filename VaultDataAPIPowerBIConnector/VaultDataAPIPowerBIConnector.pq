// This file contains your Data Connector logic
[Version = "1.0.0"]
section VaultDataAPIPowerBIConnector;

// Native client flow (PKCE)
// User will have to update with their client_id and redirect_uri
client_id = "RnDabRYTESTAPPVjVh6WHs8tL66pThOpfIG2QhQFVA";
redirect_uri = "http://localhost:8080/";

// vault Id, if you want query other vault, you need to change this value
vault_id = "1";

// OAuth settings, production: https://developer.api.autodesk.com/authentication/v2/
OAuthBaseUrl = "https://developer.api.autodesk.com/authentication/v2/";
AuthorizeUrl = Uri.Combine(OAuthBaseUrl, "authorize");
TokenUrl = Uri.Combine(OAuthBaseUrl, "token");


// The sample provides two code_challenge_method examples: "plain" and "S256".
code_challenge_method = "S256";

// Other OAuth settings
windowWidth = 720;
windowHeight = 1024;

[DataSource.Kind = "VaultDataAPIPowerBIConnector", Publish = "VaultDataAPIPowerBIConnector.UI"]
shared VaultDataAPIPowerBIConnector.Contents = Value.ReplaceType(VaultDataAPIPowerBIImpl, VaultDataAPIPowerBIScopeType);

// Data Source Kind description
VaultDataAPIPowerBIConnector = [
    Authentication = [
        OAuth = [
            StartLogin = StartLogin,
            FinishLogin = FinishLogin,
            Refresh = Refresh
        ],
        Anonymous = []
    ],
    Label = Extension.LoadString("DataSourceLabel")
];

//
// UI Export definition
//
VaultDataAPIPowerBIConnector.UI = [
    Beta = false,
    Category = "Other",
    ButtonText = {Extension.LoadString("ButtonTitle"), Extension.LoadString("ButtonHelp")},
    SourceImage = VaultDataAPIPowerBIConnector.Icons,
    SourceTypeImage = VaultDataAPIPowerBIConnector.Icons
];

VaultDataAPIPowerBIConnector.Icons = [
    Icon16 = { Extension.Contents("VaultDataAPIPowerBIConnector16.png"), Extension.Contents("VaultDataAPIPowerBIConnector20.png"), Extension.Contents("VaultDataAPIPowerBIConnector24.png"), Extension.Contents("VaultDataAPIPowerBIConnector32.png") },
    Icon32 = { Extension.Contents("VaultDataAPIPowerBIConnector32.png"), Extension.Contents("VaultDataAPIPowerBIConnector40.png"), Extension.Contents("VaultDataAPIPowerBIConnector48.png"), Extension.Contents("VaultDataAPIPowerBIConnector64.png") }
];

VaultDataAPIPowerBIScopeType = type function (
    url as (
        Uri.Type meta [
            Documentation.FieldCaption = "Vault GateWay Server URL ",
            Documentation.FieldDescription = "The URL of the Vault server to connect to",
            Documentation.SampleValues = { "https://******.vg.autodesk.com" }
        ]
    ),
    optional apis as (
        type text meta [
            Documentation.FieldCaption = "API List(GetUsers by default)",
            Documentation.FieldDescription = "The list of APIs to call",
            Documentation.SampleValues = { "GetUsers", "GetFiles", "GetItems" },
            Documentation.AllowedValues = { "GetUsers", "GetFiles", "GetItems" }
        ]
    )     
) as table meta [
    Documentation.Name = "Vault Data API sample connector",
    Documentation.LongDescription = "Returns a table with the list of scopes available for the given Vault server URL"
];

// VaultDataAPIPowerBI.Contents = (url as text) as table =>
VaultDataAPIPowerBIImpl = (url as text, optional apis as text) as table =>
    let
        result = if apis = "GetUsers" then
            VaultDataAPIPowerBI.GetUsers(url)
        else if apis = "GetFiles" then
            VaultDataAPIPowerBI.GetFiles(url)
        else if apis = "GetItems" then
            VaultDataAPIPowerBI.GetItems(url)
        else
            VaultDataAPIPowerBI.GetUsers(url)
    in
        result;

VaultDataAPIPowerBI.GetUsers = (url as text) as table =>
    let
        content = Web.Contents(
            url, 
            [
                RelativePath = "/AutodeskDM/Services/api/vault/v2/users"
            ]
        ),
        json = Json.Document(content),
        results = json[results],
        headers =
        let
            allHeaders = List.Combine(List.Transform(results, Record.FieldNames)),
            uniqueHeaders = List.Distinct(allHeaders)
        in
            uniqueHeaders,
        table = Table.FromRecords(results, headers, MissingField.UseNull)
    in
        table;

VaultDataAPIPowerBI.GetFileDetail = (url as text, id as text) as any => 
    let
        secondQuery = (id_inner as text) as any => 
            let
                content = Web.Contents(
                    url, 
                    [
                        RelativePath = "/AutodeskDM/Services/api/vault/v2/vaults/" & vault_id & "/file-versions/" & id_inner
                    ]
                ),
                json = Json.Document(content),
                results = json[results]
            in
                results
    in
        secondQuery(id);

VaultDataAPIPowerBI.GetFiles = (url as text) as table =>
    let
        // Combine the properties with the results
        CombineProperies = (resultsTableRaw as table, propertyDefinitionsTable as table) as table =>
            let
                #"Expanded Properties" = Table.ExpandListColumn(resultsTableRaw, "properties"),
                #"Expanded Records" = Table.ExpandRecordColumn(#"Expanded Properties", "properties", {"propertyDefinitionId", "value"}, {"propertyDefinitionId", "value"}),
                #"Pivoted Column" = Table.Pivot(#"Expanded Records", List.Distinct(#"Expanded Records"[propertyDefinitionId]), "propertyDefinitionId", "value"),
                #"Renamed Columns" = Table.TransformColumnNames(#"Pivoted Column", each let currentName = _ in let newName = try Table.SelectRows(propertyDefinitionsTable, each [propertyDefinitionId] = currentName)[displayName]{0} otherwise currentName in newName)
            in
                #"Renamed Columns",

        // Get the property definitions
        GetPropertyTable = (url as text) as table =>
            let
                content = Web.Contents(
                    url, 
                    [
                        RelativePath = "/AutodeskDM/Services/api/vault/v2/vaults/" & vault_id & "/property-definitions"
                    ]
                ),
                jsonRecord = Json.Document(content),
                propertyDefinitionTableRaw  = Table.FromRecords(jsonRecord[results]),
                propertyDefinitionsTable = Table.RenameColumns(propertyDefinitionTableRaw, {{"id", "propertyDefinitionId"}})
            in
                propertyDefinitionsTable,

        // Get the first page of results
        GetFirstPage = (url as text) as record =>
        let
            content = Web.Contents(
                url, 
                [
                    RelativePath = "/AutodeskDM/Services/api/vault/v2/vaults/" & vault_id & "/file-versions",
                    Query = [limit = "1000", #"option[propDefIds]" = "all"]
                ]
            ),
            // Convert JSON text to record
            jsonRecord = Json.Document(content)
        in
            jsonRecord,

        // Get all pages of results
        GetNextPage = (baseUrl as text, originalNextUrl as text, resultsTableRaw as table) as table =>
        let
            nextUrl = if originalNextUrl <> null then Text.Combine({baseUrl, originalNextUrl}, "") else null,
            content = Web.Contents(nextUrl),
            jsonRecord = Json.Document(content),
            nextNextUrl = try jsonRecord[pagination][nextUrl] otherwise null,
            fields = {"id", "name", "state", "stateColor", "revision", "category", "categoryColor", "classification", "lastModifiedDate", "isCheckedOut", "hasVisualizationAttachment", "visualizationAttachmentStatus", "version", "size", "isCloaked", "checkinDate", "isHidden", "isReadOnly", "parentFolderId", "createDate", "createUserName","properties"},

            combinedResults  = Table.Combine({resultsTableRaw, Table.FromRecords(jsonRecord[results], fields, MissingField.UseNull)}),
            finalResults = if nextNextUrl <> null then @GetNextPage(baseUrl, nextNextUrl, combinedResults) else combinedResults
        in
            finalResults,

        // Call the function with the initial URL
        firstPage = GetFirstPage(url),
        fields = {"id", "name", "state", "stateColor", "revision", "category", "categoryColor", "classification", "lastModifiedDate", "isCheckedOut", "hasVisualizationAttachment", "visualizationAttachmentStatus", "version", "size", "isCloaked", "checkinDate", "isHidden", "isReadOnly", "parentFolderId", "createDate", "createUserName","properties"},
        firstResult  = Table.FromRecords(firstPage[results], fields, MissingField.UseNull),
        nextUrl = try firstPage[pagination][nextUrl] otherwise null,
        allPages = if nextUrl <> null then GetNextPage(url, nextUrl, firstResult) else firstResult,
        propertyTable = GetPropertyTable(url),
        finalTable = CombineProperies(allPages, propertyTable)
    in
        finalTable;


VaultDataAPIPowerBI.GetFilesWithOnePage = (url as text) as table =>
    let
        content = Web.Contents(
            url, 
            [
                RelativePath = "/AutodeskDM/Services/api/vault/v2/vaults/" & vault_id & "/file-versions",
                Query = [limit = "1000", #"option[fullModels]" = "true"]
            ]
        ),


        // Convert JSON text to record
        jsonRecord = Json.Document(content),
        // Get the results array from the JSON document
        resultsArray = jsonRecord[results],
        fields = {"id", "name", "state", "stateColor", "revision", "category", "categoryColor", "classification", "lastModifiedDate", "isCheckedOut", "hasVisualizationAttachment", "visualizationAttachmentStatus", "version", "size", "isCloaked", "checkinDate", "isHidden", "isReadOnly", "parentFolderId", "createDate", "createUserName","properties"},
        resultsTableRaw  = Table.FromRecords(resultsArray, fields, MissingField.UseNull),

        // Get the propertyDefinition part of the record
        propertyDefinitionsRecord = jsonRecord[included][propertyDefinition],

        // Convert the record to a table
        propertyDefinitionsTableRaw = Record.ToTable(propertyDefinitionsRecord),

        // Extract displayName from each record
        extractDisplayName = Table.TransformColumns(propertyDefinitionsTableRaw, {{"Value", each _[displayName], type text}}),

        // Rename the columns
        propertyDefinitionsTable = Table.RenameColumns(extractDisplayName, {{"Name", "propertyDefinitionId"}, {"Value", "displayName"}}),

        #"Expanded Properties" = Table.ExpandListColumn(resultsTableRaw, "properties"),
        #"Expanded Records" = Table.ExpandRecordColumn(#"Expanded Properties", "properties", {"propertyDefinitionId", "value"}, {"propertyDefinitionId", "value"}),
        #"Pivoted Column" = Table.Pivot(#"Expanded Records", List.Distinct(#"Expanded Records"[propertyDefinitionId]), "propertyDefinitionId", "value"),
        #"Removed Errors" = Table.RemoveRowsWithErrors(#"Pivoted Column", {"checkoutDate"}),
        #"Renamed Columns" = Table.TransformColumnNames(#"Pivoted Column", each let currentName = _ in let newName = try Table.SelectRows(propertyDefinitionsTable, each [propertyDefinitionId] = currentName)[displayName]{0} otherwise currentName in newName)
    in
        #"Renamed Columns";


VaultDataAPIPowerBI.GetItems = (url as text) as table =>
    let
        content = Web.Contents(
            url, 
            [
                RelativePath = "/AutodeskDM/Services/api/vault/v2/vaults/" & vault_id & "/items",
                Query = [limit = "1000"]
            ]
        ),
        json = Json.Document(content),
        results = json[results],

        initial_table = Table.FromRecords(results),
        fields = {"id", "name", "revision", "lastModifiedUserName", "lastModifiedDate", "number", "title", "description", "comment", "state", "stateColor", "category", "categoryColor", "version", "isReadOnly", "isCloaked"},
        final_table_with_itemVersion = List.Accumulate(fields, initial_table, (table, field) => 
            Table.FromRecords(List.Transform(Table.ToRecords(table), each Record.AddField(_, "ItemVersion_" & field, Record.Field(_[itemVersion], field))))
        ),
        final_table = Table.RemoveColumns(final_table_with_itemVersion, {"itemVersion"})
    in
        final_table;        

StartLogin = (resourceUrl, state, display) =>
    let
        // We'll generate our code verifier using Guids
        plainTextCodeVerifier = Text.NewGuid() & Text.NewGuid(),
        codeVerifier =
            if (code_challenge_method = "plain") then
                plainTextCodeVerifier
            else if (code_challenge_method = "S256") then
                Base64Url.Encode(Crypto.CreateHash(CryptoAlgorithm.SHA256, Text.ToBinary(plainTextCodeVerifier)))
            else
                error "Unexpected code_challenge_method",

        AuthorizeUrl = AuthorizeUrl
            & "?"
            & Uri.BuildQueryString(
                [
                    client_id = client_id,
                    response_type = "code",
                    code_challenge_method = code_challenge_method,
                    code_challenge = codeVerifier,
                    state = state,
                    redirect_uri = redirect_uri
                ]
            )
    in
        [
            LoginUri = AuthorizeUrl,
            CallbackUri = redirect_uri,
            WindowHeight = windowHeight,
            WindowWidth = windowWidth,
            // Need to roundtrip this value to FinishLogin
            Context = plainTextCodeVerifier
        ];

// The code verifier will be passed in through the context parameter.
FinishLogin = (context, callbackUri, state) =>
    let
        Parts = Uri.Parts(callbackUri)[Query]
    in
        TokenMethod(Parts[code], "authorization_code", context);


// Verifier is optional to support both the original FinishLogin call
// (which has a verifier) and the Refresh call (which does not).
TokenMethod = (code, grant_type, optional verifier) =>
    let
        codeVerifier = if (verifier <> null) then [code_verifier = verifier] else [],
        codeParameter = if (grant_type = "authorization_code") then [code = code] else [refresh_token = code],
        query = codeVerifier
            & codeParameter
            & [
                client_id = client_id,
                // Native client flows should not require a client_secret when using PKCE, but some still do.
                // client_secret = client_secret,
                grant_type = grant_type,
                redirect_uri = redirect_uri
            ],
        // Set this if your API returns a non-2xx status for login failures
        // ManualHandlingStatusCodes = {400, 403}
        ManualHandlingStatusCodes = {},
        Response = Web.Contents(
            TokenUrl,
            [
                Content = Text.ToBinary(Uri.BuildQueryString(query)),
                Headers = [
                    #"Content-type" = "application/x-www-form-urlencoded",
                    #"Accept" = "application/json"
                ],
                ManualStatusHandling = ManualHandlingStatusCodes
            ]
        ),
        Parts = Json.Document(Response)
    in
        // check for error in response
        if (Parts[error]? <> null) then
            error Error.Record(Parts[error], Parts[message]?)
        else
            Parts;


Refresh = (resourceUrl, refresh_token) => TokenMethod(refresh_token, "refresh_token");

Base64Url.Encode = (s) => Text.Replace(Text.Replace(Text.BeforeDelimiter(Binary.ToText(s,BinaryEncoding.Base64),"="),"+","-"),"/","_");
