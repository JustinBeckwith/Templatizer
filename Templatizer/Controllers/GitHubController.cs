﻿using Microsoft.AspNetCore.Mvc;
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

namespace Templatizer.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class GitHubController : ControllerBase
  {
    private readonly ILogger<GitHubController> _logger;
    private IConfiguration _config;

    static readonly string GITHUB_KEY_NAME = "templatizer-github-key";
    static readonly string GITHUB_WEBHOOK_SECRET_NAME = "templatizer-webhook-secret";

    public GitHubController(ILogger<GitHubController> logger, IConfiguration configuration)
    {
      _logger = logger;
      _config = configuration;
    }

    /// <summary>
    /// Webhook invoked by GitHub when repository events are dispatched.
    /// </summary>
    /// <returns></returns>
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook()
    {
      // Fetch the JSON body as raw text
      var reader = new StreamReader(Request.Body, Encoding.UTF8);
      var result = await reader.ReadToEndAsync();
      _logger.LogInformation(result);

      var headers = Request.Headers;
      foreach (var header in Request.Headers)
      {
        Console.WriteLine(header.ToString());
      }

      // Compare hash signatures to validate this request came from GitHub
      var signatureFromHeader = Request.Headers["x-hub-signature"];
      var webhookSecret = await GetSecret(GitHubController.GITHUB_WEBHOOK_SECRET_NAME);
      var sha1 = new HMACSHA1(Encoding.UTF8.GetBytes(webhookSecret.Trim()));
      var signature = sha1.ComputeHash(Encoding.UTF8.GetBytes(result));
      var computedSignature = "sha1=" + BitConverter.ToString(signature).Replace("-", "").ToLower();
      if (computedSignature != signatureFromHeader)
      {
        return StatusCode(403);
      }

      // Dispatch methods based on the event payload type
      var evt = Request.Headers["x-github-event"];
      switch (evt)
      {
        case "push":
          var payload = JsonSerializer.Deserialize<PushEventPayload>(result);
          HandlePushEvent(payload);
          break;
      }
      return StatusCode(200);
    }

    private void HandlePushEvent(PushEventPayload payload)
    {
      Console.WriteLine("I am in the push event!");
      Console.WriteLine($"Got a push to {payload.repository.full_name} from {payload.sender.login}");
    }

    /// <summary>
    /// Obtains the Private Key from Secret Manager, and uses it to create a
    /// temporary JWT token that can be used to make requests to GitHub
    /// applications.
    /// </summary>
    /// <returns>A JWT token to be included as an authorization header.</returns>
    private async Task<string> GetJWT()
    {
      var privateKey = await GetSecret(GitHubController.GITHUB_KEY_NAME);

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
    /// Obtain an access token capable of using the GitHub API for a given
    /// repository.
    /// </summary>
    /// <param name="installationId"></param>
    /// <returns></returns>
    private async Task<string> GetAccessToken(string installationId)
    {
      // TODO: future me, these expire in one hour after the request
      // https://api.github.com/app/installations/:installation_id/access_tokens
      var jwt = await GetJWT();
      var endpoint = $"https://api.github.com/app/installations/${installationId}/access_tokens";
      var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
      var client = new HttpClient();
      requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
      requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.machine-man-preview+json"));
      var response = await client.SendAsync(requestMessage);
      if (!response.IsSuccessStatusCode)
      {
        _logger.LogError("Request for Access Token failed.", response.StatusCode);
        return String.Empty;
      }
      var responseStream = await response.Content.ReadAsStreamAsync();
      var result = await JsonSerializer.DeserializeAsync<GitHubAccessTokenResult>(responseStream);
      return result.token;
    }
  }
}
