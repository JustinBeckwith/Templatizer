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
    public string login;
    public int id;
    public string node_id;
    public string avatar_url;
    public string gravatar_id;
    public string url;
    public string html_url;
    public string followers_url;
    public string following_url;
    public string gists_url;
    public string starred_url;
    public string subscriptions_url;
    public string organizations_url;
    public string repos_url;
    public string events_url;
    public string received_events_url;
    public string type;
    public bool site_admin;
  }

  public class GitHubRepository
  {
    public int id;
    public string node_id;
    public string name;
    public string full_name;
    public GitHubUser owner;
    [JsonPropertyName("private")]
    public bool isPrivate { get; set; }
    public string html_url;
    public string description;
    public bool fork;
    public string url;
    public string archive_url;
    public string assignees_url;
    public string blobs_url;
    public string branches_url;
    public string collaborators_url;
    public string comments_url;
    public string commits_url;
    public string compare_url;
    public string contents_url;
    public string contributors_url;
    public string deployments_url;
    public string downloads_url;
    public string events_url;
    public string forks_url;
    public string git_commits_url;
    public string git_refs_url;
    public string git_tags_url;
    public string git_url;
    public string issue_comment_url;
    public string issue_events_url;
    public string issues_url;
    public string keys_url;
    public string labels_url;
    public string languages_url;
    public string merges_url;
    public string milestones_url;
    public string notifications_url;
    public string pulls_url;
    public string releases_url;
    public string ssh_url;
    public string stargazers_url;
    public string statuses_url;
    public string subscribers_url;
    public string subscription_url;
    public string tags_url;
    public string teams_url;
    public string trees_url;
  }

  public class GitHubOrganization
  {
    public string login;
    public int id;
    public string node_id;
    public string url;
    public string repos_url;
    public string events_url;
    public string hooks_url;
    public string issues_url;
    public string members_url;
    public string public_members_url;
    public string avatar_url;
    public string description;
  }

  public class GitHubAppInstallation
  {
    public int id;
    public GitHubUser account;
    public string repository_selection;
    public string access_tokens_url;
    public string repositories_url;
    public string html_url;
    public int app_id;
    public int target_id;
    public string target_type;
    public Dictionary<string, string> permissions;
    public string[] events;
    public int created_at;
    public int updated_at;
    public string single_file_name;
  }

  public class GitHubAccessTokenResult
  {
    public string token;
    public string expires_at;
    public string repository_selection;
    public Dictionary<string, string> permissions;

  }
}
