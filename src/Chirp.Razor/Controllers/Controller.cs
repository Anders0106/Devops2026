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

    private static int _latest = 0;

    public Controller(ChirpDBContext context, IServiceProvider provider, ICheepService service,
        UserManager<Author> userManager, IUserStore<Author> userStore)
    {
        _context = context;
        _provider = provider;
        _service = service;
        _userManager = userManager;
        _userStore = userStore;
        _emailStore = (IUserEmailStore<Author>)_userStore;
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
    public IActionResult Latest() => Ok(new { latest = _latest });

    [HttpGet("/fllws/{username}")]
    public IActionResult Follows(string username, [FromHeader] string authorization,
        [FromQuery] int? latest, [FromQuery] int? no)
    {
        if (!IsAuthorized(authorization))
            return StatusCode(403, Forbidden);

        var user = _service.GetAuthorByName(username);
        if (user == null) return NotFound();

        UpdateLatest(latest);

        var follows = _context.Follows
            .Where(f => f.Follower.UserName == username)
            .OrderByDescending(f => f.Followed.UserName)
            .Take(no ?? 100)
            .Select(f => _context.Authors.FirstOrDefault(a => a.Id == f.FollowedId)!.UserName)
            .ToList();

        return Ok(new { follows });
    }

    [HttpPost("/fllws/{username}")]
    public IActionResult Follow(string username, [FromHeader] string authorization,
        [FromQuery] int? latest, [FromBody] FollowRequest request)
    {
        if (!IsAuthorized(authorization))
            return StatusCode(403, Forbidden);

        var follower = _service.GetAuthorByName(username);
        if (follower == null) return NotFound();

        if (request.Follow != null)
        {
            var followed = _service.GetAuthorByName(request.Follow);
            if (followed == null) return NotFound();
            _service.Follow(follower, followed);
        }
        else if (request.Unfollow != null)
        {
            var unfollowed = _service.GetAuthorByName(request.Unfollow);
            if (unfollowed == null) return NotFound();
            _service.Unfollow(follower, unfollowed);
        }
        else
        {
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
            return StatusCode(403, Forbidden);

        UpdateLatest(latest);

        var cheeps = _context.Cheeps
            .Include(c => c.Author)
            .OrderByDescending(c => c.TimeStamp)
            .Take(no ?? 100)
            .ToList();

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
            return StatusCode(403, Forbidden);

        var user = _service.GetAuthorByName(username);
        if (user == null) return NotFound();

        UpdateLatest(latest);

        var cheeps = _context.Cheeps
            .Include(c => c.Author)
            .Where(c => EF.Functions.Collate(c.Author.UserName, "NOCASE") == username)
            .OrderByDescending(c => c.TimeStamp)
            .Take(no ?? 100)
            .ToList();

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
            return StatusCode(403, Forbidden);

        var author = _service.GetAuthorByName(username);
        if (author == null) return NotFound();

        UpdateLatest(latest);

        var cheep = new Chirp.Core.DTO.CheepDTO
        {
            Text = request.Content,
            TimeStamp = DateTime.UtcNow,
            Author = _service.ToDomain(author)
        };

        _service.CreateCheep(cheep);
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
            return BadRequest(new { status = 400, error_msg = "Invalid request body" });

        if (await _userManager.FindByNameAsync(credentials.Username) != null)
            return BadRequest(new { status = 400, error_msg = "Username already exists" });

        var user = Activator.CreateInstance<Author>();
        await _userStore.SetUserNameAsync(user, credentials.Username, CancellationToken.None);
        await _emailStore.SetEmailAsync(user, credentials.Email, CancellationToken.None);
        var result = await _userManager.CreateAsync(user, credentials.Password);

        UpdateLatest(latest);

        if (result.Succeeded)
            return StatusCode(StatusCodes.Status204NoContent);

        var errorMsg = string.Join("; ", result.Errors.Select(e => e.Description));
        return BadRequest(new { status = 400, error_msg = errorMsg });
    }
}
