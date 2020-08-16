using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Text;
using System.Net.Http;
using Templatizer.Models;
using System.Net.Http.Json;
using Google.Cloud.Firestore;

namespace Templatizer.Core
{
  public class ConfigManager
  {
    private readonly ILogger _logger;
    private AuthManager _authManager;
    private IConfiguration _config;
    private FirestoreDb _firestoreDb;

    public ConfigManager(ILogger logger, IConfiguration config, AuthManager authManager)
    {
      this._logger = logger;
      this._config = config;
      this._authManager = authManager;
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
    public async Task<AppConfig> GetConfig(int installationId, string org, string repo)
    {
      var token = await this._authManager.GetAccessToken(installationId);
      var rootUrl = $"https://api.github.com/repos/{org}";
      var repoConfigUrl = $"{rootUrl}/{repo}/contents/.github/templatizer.yml";
      var orgConfigUrl = $"{rootUrl}/.github/contents/.github/templatizer.yml";
      var config = await this.GetConfigByUrl(installationId, repoConfigUrl);
      if (config == null)
      {
        config = await this.GetConfigByUrl(installationId, orgConfigUrl);
      }
      return config;
    }

    /// <summary>
    /// Obtain the config given a specific url.async
    /// </summary>
    /// <param name="url">endpoint to query for config</param>
    /// <returns>Configuration from the repo, org, or defaults.</returns>
    private async Task<AppConfig> GetConfigByUrl(int installationId, string url)
    {
      var token = await this._authManager.GetAccessToken(installationId);
      var client = new HttpClient();
      var requestMessage = new HttpRequestMessage
      {
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

    /// <summary>
    /// Store an AppConfig, using the `org/repo` as a key in Firestore
    /// </summary>
    /// <param name="config">AppConfig of the repository</param>
    /// <param name="repoId">numeric repository id</param>
    /// <returns></returns>
    public async Task StoreConfigInFirestore(FullAppConfig config, int repoId)
    {
      if (config == null)
      {
        return;
      }
      var db = GetFirestoreDb();
      var doc = db.Collection("configs").Document(repoId.ToString());
      await doc.SetAsync(config);
    }

    /// <summary>
    /// Fetch an AppConfig from Firestore for a given org/repo.
    /// </summary>
    /// <param name="repo"></param>
    /// <returns></returns>
    public async Task<FullAppConfig> GetConfigFromFirestore(int repoId)
    {
      var db = GetFirestoreDb();
      var doc = db.Collection("configs").Document(repoId.ToString());
      var snapshot = await doc.GetSnapshotAsync();
      if (snapshot.Exists) {
        Console.WriteLine("Document data for {0} document:", snapshot.Id);
        var config = snapshot.ConvertTo<FullAppConfig>();
        return config;
      } else {
        Console.WriteLine("Document {0} does not exist!", snapshot.Id);
        return null;
      }
    }

    /// <summary>
    /// Fetch and cache an instance of a FirestoreDB
    /// </summary>
    /// <returns></returns>
    private FirestoreDb GetFirestoreDb()
    {
      if (_firestoreDb == null) {
        var projectId = _config["ProjectId"];
        _firestoreDb = FirestoreDb.Create(projectId);
      }
      return _firestoreDb;
    }
  }
}
