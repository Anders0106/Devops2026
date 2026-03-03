using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;

public class DownloadDbModel : PageModel
{
    public IActionResult OnGet()
    {
        var filePath = "/app/Assets/chirp.db";

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }

        var bytes = System.IO.File.ReadAllBytes(filePath);
        var fileName = "chirp.db";

        return File(bytes, "application/octet-stream", fileName);
    }
}