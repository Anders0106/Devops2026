using Chirp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Chirp.Razor.Pages;

public class PublicModel : Model
{
	public PublicModel(ICheepService service, ILoggerFactory loggerFactory) : base(service, loggerFactory) { }
	private int infinitePage = 1;

	public ActionResult OnGet([FromQuery] int page)
	{
		if (page < 1) page = 1;
		base.PaginateCheeps(page);
		_logger.LogInformation("Public timeline loaded, page {Page}", page);
		return Page();
	}

	public PartialViewResult OnGetLoadMoreCheeps()
	{
		infinitePage++;
		base.PaginateCheeps(infinitePage);
		_logger.LogInformation("Public timeline load more, page {Page}", infinitePage);
		return Partial("_CheepListPartial", (Cheeps, CheepRange, PageNumber, TotalPages, UserAuthor, FollowedAuthors));
	}

	public new IActionResult OnPostDeleteCheep(int cheepId, int page = 1)
	{
		return base.OnPostDeleteCheep(cheepId, page);
	}
}