using Microsoft.AspNetCore.Mvc;
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
using Templatizer.Core;

namespace Templatizer.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class GitHubController : ControllerBase
  {
    private readonly ILogger<GitHubController> _logger;
    private IConfiguration _config;
    private AuthManager _authManager;

    public GitHubController(ILogger<GitHubController> logger, IConfiguration configuration)
    {
      _logger = logger;
      _config = configuration;
      this._authManager = new AuthManager(logger, configuration);
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
      var isSignatureValid = await this._authManager.ValidateSignature(signatureFromHeader, result);
      if (!isSignatureValid)
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

    /// <summary>
    /// Check if this repository is a source of template files that are meant
    /// to be copied to other repositories using the same config set. If so,
    /// apply the change to all configured target repositories.
    /// </summary>
    /// <param name="payload"></param>
    private void HandlePushEvent(PushEventPayload payload)
    {
      Console.WriteLine("I am in the push event!");
      Console.WriteLine($"Got a push to {payload.repository.full_name} from {payload.sender.login}");
      var config = GetConfig(
        payload.installation.id,
        payload.repository.owner.login,
        payload.repository.name
      );
      Console.WriteLine(config.ToString());
    }

    /// <summary>
    /// Obtain the config for the repository, looking in order of preference:
    /// - org/repo/.github/templatizer.yml
    /// - org/.github/.github/templatizer.yml
    /// - default settings
    /// </summary>
    /// <param name="org">Organiation for the repository</param>
    /// <param name="repo">Repository name</param>
    /// <returns>Configuration from the repo, org, or defaults.</returns>
    private async Task<AppConfig> GetConfig(int installationId, string org, string repo)
    {
      var token = await GetAccessToken(installationId);
      var client = new HttpClient();
      var endpoint = $"https://api.github.com/repos/{org}/{repo}/contents/.github/templatizer.yml";
      var requestMessage = new HttpRequestMessage {
        RequestUri = new Uri(endpoint),
        Method = HttpMethod.Get,
        Headers = {
          { "Authorization", $"token {token}"},
          { "User-Agent", "Templatizer" }
        }
      };
      var response = await client.SendAsync(requestMessage);
      if (!response.IsSuccessStatusCode)
      {
        _logger.LogError("Request for config failed.", response.StatusCode);
        return new AppConfig();
      }
      var data = await response.Content.ReadFromJsonAsync<GitHubContents>();
      var configBits = Convert.FromBase64String(data.content);
      var configData = Encoding.UTF8.GetString(configBits);
      var deserializer = new YamlDotNet.Serialization.Deserializer();
      var config = deserializer.Deserialize<AppConfig>(configData);
      return config;
    }

        /// <summary>
    /// Obtain the config given a specific url.async
    /// </summary>
    /// <param name="url">endpoint to query for config</param>
    /// <returns>Configuration from the repo, org, or defaults.</returns>
    private async Task<AppConfig> GetConfigByUrl(int installationId, string url)
    {
      var token = await GetAccessToken(installationId);
      var client = new HttpClient();
      var requestMessage = new HttpRequestMessage {
        RequestUri = new Uri(url),
        Method = HttpMethod.Get,
        Headers = {
          { "Authorization", $"token {token}"},
          { "User-Agent", "Templatizer" }
        }
      };
      var response = await client.SendAsync(requestMessage);
      if (!response.IsSuccessStatusCode)
      {
        _logger.LogError("Request for config failed.", response.StatusCode);
        return null;
      }
      var data = await response.Content.ReadFromJsonAsync<GitHubContents>();
      var configBits = Convert.FromBase64String(data.content);
      var configData = Encoding.UTF8.GetString(configBits);
      var deserializer = new YamlDotNet.Serialization.Deserializer();
      var config = deserializer.Deserialize<AppConfig>(configData);
      return config;
    }
  }
}
