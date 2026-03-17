using Chirp.Core.Classes;
using Chirp.Repositories.Repositories;
using Chirp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json.Serialization;

namespace Chirp.Razor.Controllers;

[ApiController]
public class Controller : ControllerBase
{
    private readonly ChirpDBContext _context;
    private readonly IServiceProvider _provider;
    private readonly ICheepService _service;
    private readonly UserManager<Author> _userManager;
    private readonly IUserStore<Author> _userStore;
    private readonly IUserEmailStore<Author> _emailStore;
    private readonly ILogger<Controller> _logger;

    private static int _latest = 0;

    public Controller(ChirpDBContext context, IServiceProvider provider, ICheepService service,
        UserManager<Author> userManager, IUserStore<Author> userStore, ILogger<Controller> logger)
    {
        _context = context;
        _provider = provider;
        _service = service;
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = (IUserEmailStore<Author>)_userStore;
        _logger = logger;
    }

    private static readonly object Forbidden =
        new { status = 403, error_msg = "You are not authorized to use this resource!" };

    private bool IsAuthorized(string authorization) =>
        authorization == "Basic c2ltdWxhdG9yOnN1cGVyX3NhZmUh";

    private void UpdateLatest(int? latest)
    {
        if (latest.HasValue) _latest = latest.Value;
    }

    [HttpGet("/latest")]
    public IActionResult Latest()
    {
        _logger.LogInformation("Latest value requested: {Latest}", _latest);
        return Ok(new { latest = _latest });
    }

    [HttpGet("/fllws/{username}")]
    public IActionResult Follows(string username, [FromHeader] string authorization,
        [FromQuery] int? latest, [FromQuery] int? no)
    {
        if (!IsAuthorized(authorization))
        {
            _logger.LogWarning("Unauthorized request for follows of user {Username}", username);
            return StatusCode(403, Forbidden);
        }

        var user = _service.GetAuthorByName(username);
        if (user == null)
        {
            _logger.LogWarning("Follows requested for unknown user {Username}", username);
            return NotFound();
        }

        UpdateLatest(latest);

        var follows = _context.Follows
            .Where(f => f.Follower.UserName == username)
            .OrderByDescending(f => f.Followed.UserName)
            .Take(no ?? 100)
            .Select(f => _context.Authors.FirstOrDefault(a => a.Id == f.FollowedId)!.UserName)
            .ToList();

        _logger.LogInformation("Fetched {Count} follows for user {Username}", follows.Count, username);
        return Ok(new { follows });
    }

    [HttpPost("/fllws/{username}")]
    public IActionResult Follow(string username, [FromHeader] string authorization,
        [FromQuery] int? latest, [FromBody] FollowRequest request)
    {
        if (!IsAuthorized(authorization))
        {
            _logger.LogWarning("Unauthorized follow/unfollow request for user {Username}", username);
            return StatusCode(403, Forbidden);
        }

        var follower = _service.GetAuthorByName(username);
        if (follower == null)
        {
            _logger.LogWarning("Follow/unfollow failed: user {Username} not found", username);
            return NotFound();
        }

        if (request.Follow != null)
        {
            var followed = _service.GetAuthorByName(request.Follow);
            if (followed == null)
            {
                _logger.LogWarning("Follow failed: {Username} tried to follow {Target}, but {Target} does not exist", username, request.Follow);
                return NotFound();
            }
            _service.Follow(follower, followed);
            _logger.LogInformation("User {Username} followed {Target}", username, request.Follow);
        }
        else if (request.Unfollow != null)
        {
            var unfollowed = _service.GetAuthorByName(request.Unfollow);
            if (unfollowed == null)
            {
                _logger.LogWarning("Unfollow failed: {Username} tried to unfollow {Target}, but {Target} does not exist", username, request.Unfollow);
                return NotFound();
            }
            _service.Unfollow(follower, unfollowed);
            _logger.LogInformation("User {Username} unfollowed {Target}", username, request.Unfollow);
        }
        else
        {
            _logger.LogWarning("Follow/unfollow request from {Username} had neither follow nor unfollow field", username);
            return BadRequest(new { status = 400, error_msg = "Request must contain either 'follow' or 'unfollow'" });
        }

        UpdateLatest(latest);
        return NoContent();
    }

    public class FollowRequest
    {
        [JsonPropertyName("follow")]
        public string? Follow { get; set; }
        [JsonPropertyName("unfollow")]
        public string? Unfollow { get; set; }
    }

    [HttpGet("/msgs")]
    public IActionResult RecentMessages(
        [FromHeader] string authorization,
        [FromQuery] int? latest,
        [FromQuery] int? no)
    {
        if (!IsAuthorized(authorization))
        {
            _logger.LogWarning("Unauthorized request for recent messages");  
            return StatusCode(403, Forbidden);
        }

        UpdateLatest(latest);

        var cheeps = _context.Cheeps
            .Include(c => c.Author)
            .OrderByDescending(c => c.TimeStamp)
            .Take(no ?? 100)
            .ToList();
        
        _logger.LogInformation("Fetched {Count} recent messages", cheeps.Count);  

        return Ok(cheeps.Select(c => new
        {
            content = c.Text,
            pub_date = c.TimeStamp,
            user = c.Author.UserName
        }));
    }

    [HttpGet("/msgs/{username}")]
    public IActionResult MessagesByUser(
        string username,
        [FromHeader] string authorization,
        [FromQuery] int? latest,
        [FromQuery] int? no)
    {
        if (!IsAuthorized(authorization))
        {
            _logger.LogWarning("Unauthorized request for messages of user {Username}", username);  
            return StatusCode(403, Forbidden);
        }
        
        var user = _service.GetAuthorByName(username);
        if (user == null)
        {
            _logger.LogWarning("Messages requested for unknown user {Username}", username);
            return NotFound();
        }

        UpdateLatest(latest);

        var cheeps = _context.Cheeps
            .Include(c => c.Author)
            .Where(c => c.Author.UserName != null && username != null && c.Author.UserName.ToLower() == username.ToLower())
            .OrderByDescending(c => c.TimeStamp)
            .Take(no ?? 100)
            .ToList();
        _logger.LogInformation("Fetched {Count} messages for user {Username}", cheeps.Count, username);

        return Ok(cheeps.Select(c => new
        {
            content = c.Text,
            pub_date = c.TimeStamp,
            user = c.Author.UserName
        }));
    }

    [HttpPost("/msgs/{username}")]
    public IActionResult PostNewMessage(
        string username,
        [FromQuery] int? latest,
        [FromHeader] string authorization,
        [FromBody] MessageRequest request)
    {
        if (!IsAuthorized(authorization))
        {
            _logger.LogWarning("Unauthorized request to create a cheep by user {Username}", username);
            return StatusCode(403, Forbidden);
        }

        var author = _service.GetAuthorByName(username);
        if (author == null)
        {
            _logger.LogWarning("Post new cheep failed, because user {Username} does not exist. Dropped content: \"{Content}\"", username, request.Content);
            return NotFound();
        }

        UpdateLatest(latest);

        var cheep = new Chirp.Core.DTO.CheepDTO
        {
            Text = request.Content,
            TimeStamp = DateTime.UtcNow,
            Author = _service.ToDomain(author)
        };

        _service.CreateCheep(cheep);
        Chirp.Razor.ChirpMetrics.CheepsCreated.Inc();
        _logger.LogInformation("New cheep message of length {messageLength} was created by {username}", request.Content.Length, username);
        return NoContent();
    }

    public class MessageRequest
    {
        [JsonPropertyName("content")]
        public required string Content { get; set; }
    }

    public class RegisterRequest
    {
        [Required]
        [JsonPropertyName("username")]
        public string Username { get; set; }

        [Required]
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [Required]
        [JsonPropertyName("pwd")]
        public string Password { get; set; }
    }

    [HttpPost("/register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest credentials, [FromQuery] int? latest)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { status = 400, error_msg = "Invalid request body" });
        }
        
        if (await _userManager.FindByNameAsync(credentials.Username) != null)
        {
            _logger.LogWarning("Registration failed: Username already exists: {Username}", credentials.Username);
            return BadRequest(new { status = 400, error_msg = "Username already exists" });
        }

        var user = Activator.CreateInstance<Author>();
        await _userStore.SetUserNameAsync(user, credentials.Username, CancellationToken.None);
        await _emailStore.SetEmailAsync(user, credentials.Email, CancellationToken.None);
        var result = await _userManager.CreateAsync(user, credentials.Password);

        UpdateLatest(latest);

        if (result.Succeeded)
        {
            _logger.LogInformation("User successfully registered: {Username}", credentials.Username);
            return StatusCode(StatusCodes.Status204NoContent);
        }

        var errorMsg = string.Join("; ", result.Errors.Select(e => e.Description));
        _logger.LogWarning("Registration failed for {Username}: {Errors}", credentials.Username, errorMsg);
        return BadRequest(new { status = 400, error_msg = errorMsg });
    }
}
