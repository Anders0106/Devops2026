using Chirp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Chirp.Razor.Pages;

public class UserTimelineModel : Model
{
	public UserTimelineModel(ICheepService service, ILoggerFactory loggerFactory) : base(service, loggerFactory) { }

	public ActionResult OnGet([FromQuery] int page, string author)
	{
		if (page < 1) page = 1;
		base.PaginateCheepsByName(page, author);
		_logger.LogInformation("User timeline loaded for {Author}, page {Page}", author, page);
		return Page();
	}

	public new IActionResult OnPostDeleteCheep(int cheepId, int page = 1)
	{
		return base.OnPostDeleteCheep(cheepId, page);
	}
}