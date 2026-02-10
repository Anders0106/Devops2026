using Chirp.Repositories.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Chirp.Razor.Controllers;

[ApiController]
public class Controller : ControllerBase
{
	private readonly ChirpDBContext _context;
	private readonly IServiceProvider _provider;
	public Controller(ChirpDBContext context, IServiceProvider provider)
	{
		_context = context;
		_provider = provider;
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
    public IActionResult MessagesByUser(string username)
    {
        return Ok();
    }
    [HttpPost("/msgs/{username}")]                 
    public IActionResult PostNewMessage(string username)
    {
        return Ok();
    }  
    [HttpPost("/register")]              
    public IActionResult Register()
    {
        return Ok();
    }  
}