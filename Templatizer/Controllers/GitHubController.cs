using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Text;
using System.Text.Json;
using Templatizer.Models;
using System.IO;
using Templatizer.Core;
using System.Net.Http;
using System.Net.Http.Json;
using System.Collections.Generic;

namespace Templatizer.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class GitHubController : ControllerBase
  {
    private readonly ILogger<GitHubController> _logger;
    private IConfiguration _config;
    private AuthManager _authManager;
    private ConfigManager _configManager;

    public GitHubController(ILogger<GitHubController> logger, IConfiguration config)
    {
      this._logger = logger;
      this._config = config;
      this._authManager = new AuthManager(logger, config);
      this._configManager = new ConfigManager(logger, config, this._authManager);
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
          await HandlePushEvent(payload);
          break;
      }
      return StatusCode(200);
    }

    /// <summary>
    /// Check if this repository is a source of template files that are meant
    /// to be copied to other repositories using the same config set. If so,
    /// apply the change to all configured target repositories.
    /// </summary>
    /// <param name="payload">Parsed JSON payload from the webhook</param>
    private async Task HandlePushEvent(PushEventPayload payload)
    {
      Console.WriteLine("I am in the push event!");
      Console.WriteLine($"Got a push to {payload.repository.full_name} from {payload.sender.login}");

      // Only look at commits that landed on the default branch
      if (payload.ref_ != payload.repository.default_branch)
      {
        return;
      }
      var config = await _configManager.GetConfig(
        payload.installation.id,
        payload.repository.owner.login,
        payload.repository.name
      );

      // TODO: Currently we are updating the configuration with push event in
      // Firestore.  This could be made more efficient by only updating the
      // config when it has changed by one of the commits that triggered the
      // push event.
      var fullConfig = new FullAppConfig
      {
        configSets = config.configSets,
        sourceSets = config.sourceSets,
        repo = payload.repository.full_name
      };
      await _configManager.StoreConfigInFirestore(fullConfig, payload.repository.id);
      Console.WriteLine(fullConfig.ToString());

      // If there are sourceSets defined, this is a source of template files that
      // make changes in other repositories.
      if (config.sourceSets == null) {
        return;
      }
      var repoName = payload.repository.full_name;
      var matchingConfigs = await _configManager.GetMatchingConfigs(repoName);

      // Iterate over each commit in the push. For each commit, iterate over
      // The list of matching configs, and generate a pull request with the
      // same contents.
    }
  }
}
