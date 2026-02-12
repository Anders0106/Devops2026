using System.ComponentModel.DataAnnotations;
using Chirp.Core.Classes;
using Chirp.Repositories.Repositories;
using Chirp.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    private int _latest = 0;

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

    private bool IsAuthorized(string authorization)
    {
        return authorization == "Basic c2ltdWxhdG9yOnN1cGVyX3NhZmUh";
    }


    [HttpGet("/fllws/{username}")]
    public IActionResult Follows(string username, [FromHeader] string authorization,
        [FromQuery] int? latest, [FromQuery] int? no)
    {
        if (!IsAuthorized(authorization))
        {
            return Unauthorized();
        }

        var follows = _context.Follows
            .Where(f => f.Follower.UserName == username)
            .OrderByDescending(f => f.Followed.UserName)
            .Take(no ?? 100)
            .ToList();

        if (latest.HasValue)
        {
            _latest = latest.Value;
        }

        return Ok(follows.Select(f => _context.Authors.FirstOrDefault(a => a.Id == f.FollowedId)?.UserName));
    }
    [HttpPost("/fllws/{username}")]
    public IActionResult Follow(string username, [FromHeader] string authorization,
        [FromQuery] int? latest, [FromBody] FollowRequest request)
    {
        if (!IsAuthorized(authorization))
        {
            return Unauthorized();
        }

        var follower = _service.GetAuthorByName(username);
        if (follower == null) return NotFound($"User '{username}' not found");

        if (request.Follow != null)
        {
            var followed = _service.GetAuthorByName(request.Follow);
            if (followed == null) return NotFound($"User '{request.Follow}' not found");
            _service.Follow(follower, followed);
        }
        else if (request.Unfollow != null)
        {
            var unfollowed = _service.GetAuthorByName(request.Unfollow);
            if (unfollowed == null) return NotFound($"User '{request.Unfollow}' not found");
            _service.Unfollow(follower, unfollowed);
        }
        else
        {
            return BadRequest("Request must contain either 'follow' or 'unfollow'");
        }

        if (latest.HasValue)
        {
            _latest = latest.Value;
        }

        return NoContent();
    }

    public class FollowRequest
    {
        public string? Follow { get; set; }
        public string? Unfollow { get; set; }
    }
    [HttpGet("/latest")]
    public IActionResult Latest()
    {
        return Ok(_latest);
    }
    [HttpGet("/msgs")]
    public IActionResult RecentMessages()
    {
        return Ok();
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
            return Unauthorized();
        }

        if (latest.HasValue)
        {
            _latest = latest.Value;
        }

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
        {
            return Unauthorized();
        }

        if (latest.HasValue)
        {
            _latest = latest.Value;
        }

        var author = _service.GetAuthorByName(username);
        if (author == null)
        {
            return NotFound($"User '{username}' not found");
        }

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
        public required string Content { get; set; }
    }

    public class RegisterRequest
    {
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }

    [HttpPost("/register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest credentials, [FromQuery] int? latest)
    {
        Console.WriteLine("Register!");
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (await _userManager.FindByNameAsync(credentials.Username) != null)
        {
            return BadRequest("Username already exists");
        }

        var user = Activator.CreateInstance<Author>();

        await _userStore.SetUserNameAsync(user, credentials.Username, CancellationToken.None);
        await _emailStore.SetEmailAsync(user, credentials.Email, CancellationToken.None);
        var result = await _userManager.CreateAsync(user, credentials.Password);

        //somehow update latest
        if (latest.HasValue)
        {
            _latest = latest.Value;
        }

        if (result.Succeeded)
        {
            return NoContent();
        }
        else
        {
            return BadRequest(result.Errors);
        }
    }
}
