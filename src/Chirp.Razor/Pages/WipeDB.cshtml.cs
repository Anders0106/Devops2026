using Chirp.Repositories.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Chirp.Razor.Pages;

public class WipeDBModel : PageModel
{
	private readonly ChirpDBContext _context;
	private readonly IServiceProvider _provider;
	private readonly ILogger<WipeDBModel> _logger;

	public WipeDBModel(ChirpDBContext context, IServiceProvider provider, ILogger<WipeDBModel> logger)
	{
		_context = context;
		_provider = provider;
		_logger = logger;
	}

	public IActionResult OnPostWipeDB()
	{
		_logger.LogWarning("Database wipe triggered by user {User}", User.Identity?.Name);
		_context.Database.EnsureDeleted();
		_context.Database.Migrate();
		_logger.LogWarning("Database wipe completed");
		return RedirectToPage("/");
	}



}