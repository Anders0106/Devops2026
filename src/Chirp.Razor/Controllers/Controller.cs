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
    

    [HttpGet("/fllws/{username}")]                 
    public IActionResult Follows(string username)
    {
        return Ok();
    } 
    [HttpPost("/fllws/{username}")]                 
    public IActionResult Follow(string username)
    {
        return Ok();    
    }
    [HttpGet("/latest")]                 
    public IActionResult Latest()
    {
        return Ok();
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
        [FromQuery] string? latest,
        [FromQuery] int? no)
    {
        
        if (authorization != "Basic c2ltdWxhdG9yOnN1cGVyX3NhZmUh")                
        {                                                                         
            return Unauthorized();                                                
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
        if (authorization != "Basic c2ltdWxhdG9yOnN1cGVyX3NhZmUh")
        {
            return Unauthorized();
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
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState.Values.SelectMany(v => v.Errors));
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