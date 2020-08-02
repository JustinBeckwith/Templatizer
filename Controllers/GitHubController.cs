using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Templatizer.Controllers
{
  [ApiController]
  [Route("[controller]")]
  public class GitHubController : ControllerBase
  {
    private readonly ILogger<GitHubController> _logger;

    public GitHubController(ILogger<GitHubController> logger)
    {
      _logger = logger;
    }

    [HttpGet("boop")]
    public string Boop()
    {
      return "👋";
    }
  }
}
