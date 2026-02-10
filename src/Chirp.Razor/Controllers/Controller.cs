using Microsoft.AspNetCore.Mvc;

namespace Chirp.Razor.Controllers;

[ApiController]
public class Controller : ControllerBase
{
    [HttpGet("/fllws/{username}")]                 
    public IActionResult Follows(string username)
    {
        
    } 
    [HttpPost("/fllws/{username}")]                 
    public IActionResult Follow(string username)
    {
        
    }
    [HttpGet("/latest")]                 
    public IActionResult Latest()
    {
        
    }
    [HttpGet("/msgs")]                 
    public IActionResult RecentMessages()
    {
        
    }
    [HttpGet("/msgs/{username}")]                 
    public IActionResult MessagesByUser(string username)
    {
        
    }
    [HttpPost("/msgs/{username}")]                 
    public IActionResult PostNewMessage(string username)
    {
        
    }  
    [HttpPost("/register")]              
    public IActionResult Register()
    {
        
    }  
}