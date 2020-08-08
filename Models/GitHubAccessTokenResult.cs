using System.Collections.Generic;

namespace Templatizer.Models
{
  public class GitHubAccessTokenResult
  {
    public string token;
    public string expires_at;
    public string repository_selection;
    public Dictionary<string, string> permissions;

  }
}
