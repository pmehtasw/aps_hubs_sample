using System;
using Autodesk.Authentication.Model;
using System.Collections.Generic;
using Autodesk.Authentication;
using Autodesk.DataManagement.Model;
using Autodesk.DataManagement;

public class Tokens
{
    public string InternalToken;
    public string PublicToken;
    public string RefreshToken;
    public DateTime ExpiresAt;
}

/// <summary>
/// Model methods
/// </summary>
public partial class APS
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _callbackUri;
    private readonly List<Scopes> InternalTokenScopes = [Scopes.DataRead, Scopes.ViewablesRead];
    private readonly List<Scopes> PublicTokenScopes = [Scopes.ViewablesRead];

    public APS(string clientId, string clientSecret, string callbackUri)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _callbackUri = callbackUri;
    }
}

/// <summary>
/// Auth methods
/// </summary>
public partial class APS
{
    public string GetAuthorizationURL()
    {
        var authenticationClient = new AuthenticationClient();
        return authenticationClient.Authorize(_clientId, ResponseType.Code, _callbackUri, InternalTokenScopes);
    }

    public async Task<Tokens> GenerateTokens(string code)
    {
        var authenticationClient = new AuthenticationClient();
        var internalAuth = await authenticationClient.GetThreeLeggedTokenAsync(_clientId, code, _callbackUri, clientSecret: _clientSecret);
        var publicAuth = await authenticationClient.RefreshTokenAsync(internalAuth.RefreshToken, _clientId, clientSecret: _clientSecret, scopes: PublicTokenScopes);
        return new Tokens
        {
            PublicToken = publicAuth.AccessToken,
            InternalToken = internalAuth.AccessToken,
            RefreshToken = publicAuth.RefreshToken,
            ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds((double)internalAuth.ExpiresIn)
        };
    }

    public async Task<Tokens> RefreshTokens(Tokens tokens)
    {
        var authenticationClient = new AuthenticationClient();
        var internalAuth = await authenticationClient.RefreshTokenAsync(tokens.RefreshToken, _clientId, clientSecret: _clientSecret, scopes: InternalTokenScopes);
        var publicAuth = await authenticationClient.RefreshTokenAsync(internalAuth.RefreshToken, _clientId, clientSecret: _clientSecret, scopes: PublicTokenScopes);
        return new Tokens
        {
            PublicToken = publicAuth.AccessToken,
            InternalToken = internalAuth.AccessToken,
            RefreshToken = publicAuth.RefreshToken,
            ExpiresAt = DateTime.Now.ToUniversalTime().AddSeconds((double)internalAuth.ExpiresIn)
        };
    }

    public async Task<UserInfo> GetUserProfile(Tokens tokens)
    {
        var authenticationClient = new AuthenticationClient();
        UserInfo userInfo = await authenticationClient.GetUserInfoAsync(tokens.InternalToken);
        return userInfo;
    }
}

/// <summary>
/// Hubs methods
/// </summary>
public partial class APS
{
    public async Task<IEnumerable<HubData>> GetHubs(Tokens tokens)
    {
        var dataManagementClient = new DataManagementClient();
        var hubs = await dataManagementClient.GetHubsAsync(accessToken: tokens.InternalToken);
        return hubs.Data;
    }

    public async Task<IEnumerable<ProjectData>> GetProjects(string hubId, Tokens tokens)
    {
        var dataManagementClient = new DataManagementClient();
        var projects = await dataManagementClient.GetHubProjectsAsync(hubId, accessToken: tokens.InternalToken);
        return projects.Data;
    }

    public async Task<IEnumerable<TopFolderData>> GetTopFolders(string hubId, string projectId, Tokens tokens)
    {
        var dataManagementClient = new DataManagementClient();
        var folders = await dataManagementClient.GetProjectTopFoldersAsync(hubId, projectId, accessToken: tokens.InternalToken);
        return folders.Data;
    }

    public async Task<IEnumerable<IFolderContentsData>> GetFolderContents(string projectId, string folderId, Tokens tokens)
    {
        var dataManagementClient = new DataManagementClient();
        var contents = await dataManagementClient.GetFolderContentsAsync(projectId, folderId, accessToken: tokens.InternalToken);
        return contents.Data;
    }

    public async Task<IEnumerable<VersionData>> GetVersions(string projectId, string itemId, Tokens tokens)
    {
        var dataManagementClient = new DataManagementClient();
        var versions = await dataManagementClient.GetItemVersionsAsync(projectId, itemId, accessToken: tokens.InternalToken);
        return versions.Data;
    }
}