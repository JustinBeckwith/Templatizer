using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace Templatizer.Models
{
  public class WebhookPayload
  {
    public string action;
    public GitHubUser sender;
    public GitHubRepository repository;
    public GitHubOrganization organization;
    public GitHubAppInstallation installation;
  }

  public class GitHubUser
  {
    public string login { get; set; }
    public int id { get; set; }
    public string node_id { get; set; }
    public string avatar_url { get; set; }
    public string gravatar_id { get; set; }
    public string url { get; set; }
    public string html_url { get; set; }
    public string followers_url { get; set; }
    public string following_url { get; set; }
    public string gists_url { get; set; }
    public string starred_url { get; set; }
    public string subscriptions_url { get; set; }
    public string organizations_url { get; set; }
    public string repos_url { get; set; }
    public string events_url { get; set; }
    public string received_events_url { get; set; }
    public string type { get; set; }
    public bool site_admin { get; set; }
  }

  public class GitHubRepository
  {
    public int id { get; set; }
    public string node_id { get; set; }
    public string name { get; set; }
    public string full_name { get; set; }
    public GitHubUser owner { get; set; }
    [JsonPropertyName("private")]
    public bool isPrivate { get; set; }
    public string html_url { get; set; }
    public string description { get; set; }
    public bool fork { get; set; }
    public string url { get; set; }
    public string archive_url { get; set; }
    public string assignees_url { get; set; }
    public string blobs_url { get; set; }
    public string branches_url { get; set; }
    public string collaborators_url { get; set; }
    public string comments_url { get; set; }
    public string commits_url { get; set; }
    public string compare_url { get; set; }
    public string contents_url { get; set; }
    public string contributors_url { get; set; }
    public string deployments_url { get; set; }
    public string downloads_url { get; set; }
    public string events_url { get; set; }
    public string forks_url { get; set; }
    public string git_commits_url { get; set; }
    public string git_refs_url { get; set; }
    public string git_tags_url { get; set; }
    public string git_url { get; set; }
    public string issue_comment_url { get; set; }
    public string issue_events_url { get; set; }
    public string issues_url { get; set; }
    public string keys_url { get; set; }
    public string labels_url { get; set; }
    public string languages_url { get; set; }
    public string merges_url { get; set; }
    public string milestones_url { get; set; }
    public string notifications_url { get; set; }
    public string pulls_url { get; set; }
    public string releases_url { get; set; }
    public string ssh_url { get; set; }
    public string stargazers_url { get; set; }
    public string statuses_url { get; set; }
    public string subscribers_url { get; set; }
    public string subscription_url { get; set; }
    public string tags_url { get; set; }
    public string teams_url { get; set; }
    public string trees_url { get; set; }
  }

  public class GitHubOrganization
  {
    public string login { get; set; }
    public int id { get; set; }
    public string node_id { get; set; }
    public string url { get; set; }
    public string repos_url { get; set; }
    public string events_url { get; set; }
    public string hooks_url { get; set; }
    public string issues_url { get; set; }
    public string members_url { get; set; }
    public string public_members_url { get; set; }
    public string avatar_url { get; set; }
    public string description { get; set; }
  }

  public class GitHubAppInstallation
  {
    public int id { get; set; }
    public GitHubUser account { get; set; }
    public string repository_selection { get; set; }
    public string access_tokens_url { get; set; }
    public string repositories_url { get; set; }
    public string html_url { get; set; }
    public int app_id { get; set; }
    public int target_id { get; set; }
    public string target_type { get; set; }
    public Dictionary<string, string> permissions { get; set; }
    public string[] events { get; set; }
    public int created_at { get; set; }
    public int updated_at { get; set; }
    public string single_file_name { get; set; }
  }

  public class GitHubAccessTokenResult
  {
    public string token { get; set; }
    public string expires_at { get; set; }
    public string repository_selection { get; set; }
    public Dictionary<string, string> permissions { get; set; }
  }

  public class GitUser
  {
    public string name { get; set; }
    public string email { get; set; }
    public string? username { get; set; }
  }

  public class Commit
  {
    public string id { get; set; }
    public string tree_id { get; set; }
    public bool distinct { get; set; }
    public string message { get; set; }
    public string timestamp { get; set; }
    public string url { get; set; }
    public GitUser author { get; set; }
    public GitUser committer { get; set; }
    public string[] added { get; set; }
    public string[] removed { get; set; }
    public string[] modified { get; set; }
  }

  public class GitHubInstallation
  {
    public int id { get; set; }
    public string node_id { get; set; }
  }

  public class PushEventPayload
  {
    [JsonPropertyName("ref")]
    public string ref_ { get; set; }
    public string before { get; set; }
    public string after { get; set; }
    public GitHubRepository repository { get; set; }
    public GitUser pusher { get; set; }
    public GitHubUser sender { get; set; }
    public GitHubInstallation installation { get; set; }
    public bool created { get; set; }
    public bool deleted { get; set; }
    public bool forced { get; set; }
    public string? base_ref { get; set; }
    public string compare { get; set; }
    public Commit[] commits { get; set; }
    public Commit head_commit { get; set; }
  }
}
