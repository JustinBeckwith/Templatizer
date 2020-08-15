using Microsoft.Extensions.Logging;
using Google.Cloud.SecretManager.V1;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Templatizer.Models;
using System.IO;
using System.Net.Http.Json;

namespace Templatizer.Core
{
  public class AuthManager
  {
    private static readonly string GITHUB_KEY_NAME = "templatizer-github-key";
    private static readonly string GITHUB_WEBHOOK_SECRET_NAME = "templatizer-webhook-secret";

    private ILogger _logger;
    private IConfiguration _config;

    public AuthManager(ILogger logger, IConfiguration configuration)
    {
      this._logger = logger;
      this._config = configuration;
    }

    /// <summary>
    /// Obtains the Private Key from Secret Manager, and uses it to create a
    /// temporary JWT token that can be used to make requests to GitHub
    /// applications.
    /// </summary>
    /// <returns>A JWT token to be included as an authorization header.</returns>
    private async Task<string> GetJWT()
    {
      var privateKey = await GetSecret(AuthManager.GITHUB_KEY_NAME);

      // The header and footer of the PEM need to be stripped before base64 decoding
      var pemParts = new List<string>(privateKey.Split("\n"));
      privateKey = String.Join("\n", pemParts.GetRange(1, pemParts.Count - 3));
      var privateKeyBytes = Convert.FromBase64String(privateKey);
      var rsa = RSA.Create();
      rsa.ImportRSAPrivateKey(privateKeyBytes, out _);
      var securityKey = new RsaSecurityKey(rsa);
      var myIssuer = _config["GitHubAppId"];
      var tokenHandler = new JwtSecurityTokenHandler();
      var tokenDescriptor = new SecurityTokenDescriptor
      {
        Expires = DateTime.UtcNow.AddMinutes(9),
        Issuer = myIssuer,
        SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256)
      };
      var token = tokenHandler.CreateToken(tokenDescriptor);
      return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Obtain an access token capable of using the GitHub API for a given
    /// repository.
    /// </summary>
    /// <param name="installationId"></param>
    /// <returns></returns>
    private async Task<string> GetAccessToken(int installationId)
    {
      // TODO: future me, these expire in one hour after the request
      // https://api.github.com/app/installations/:installation_id/access_tokens
      var jwt = await GetJWT();
      var endpoint = $"https://api.github.com/app/installations/{installationId}/access_tokens";
      var requestMessage = new HttpRequestMessage
      {
        RequestUri = new Uri(endpoint),
        Method = HttpMethod.Post,
        Headers = {
          { "Authorization", $"Bearer {jwt}" },
          { "Accept", "application/vnd.github.machine-man-preview+json" },
          { "User-Agent", "Templatizer" }
        }
      };
      var client = new HttpClient();
      var response = await client.SendAsync(requestMessage);
      if (!response.IsSuccessStatusCode)
      {
        _logger.LogError("Request for Access Token failed.", response.StatusCode);
        var output = await response.Content.ReadAsStringAsync();
        return string.Empty;
      }
      var responseStream = await response.Content.ReadAsStreamAsync();
      var result = await JsonSerializer.DeserializeAsync<GitHubAccessTokenResult>(responseStream);
      return result.token;
    }

    /// <summary>
    /// Obtain a secret from Google Cloud Secret Manager.
    /// </summary>
    /// <param name="secretName">Name of the secret to fetch</param>
    /// <returns>The secret</returns>
    private async Task<string> GetSecret(string secretName)
    {
      var projectId = _config["ProjectId"];
      _logger.LogInformation($"Hi there! Using {projectId}");
      var secretVersionName = new SecretVersionName(projectId, secretName, "latest");
      var client = SecretManagerServiceClient.Create();
      var result = await client.AccessSecretVersionAsync(secretVersionName);
      var payload = result.Payload.Data.ToStringUtf8();
      return payload;
    }

    /// <summary>
    /// Ensure the payload hash matches the provided signature
    /// </summary>
    /// <param name="signatureFromHeader">Contents of the x-hub-signature HTTP header</param>
    /// <param name="payload">The full string representation of a payload</param>
    /// <returns></returns>
    public async Task<Boolean> ValidateSignature(string signatureFromHeader, string payload)
    {
      var webhookSecret = await GetSecret(AuthManager.GITHUB_WEBHOOK_SECRET_NAME);
      var sha1 = new HMACSHA1(Encoding.UTF8.GetBytes(webhookSecret.Trim()));
      var signature = sha1.ComputeHash(Encoding.UTF8.GetBytes(payload));
      var computedSignature = "sha1=" + BitConverter.ToString(signature).Replace("-", "").ToLower();
      return (computedSignature == signatureFromHeader);
    }
  }
}
