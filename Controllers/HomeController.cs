using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;

namespace RecruitmentTracker.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public IActionResult Index() => View();

    public async Task<IActionResult> Opportunities()
    {
        var vacancies = await _db.Vacancies
            .Where(v => v.Status == "Open" && v.ApplicationDeadline >= DateTime.Today)
            .OrderBy(v => v.ApplicationDeadline)
            .ToListAsync();

        return View(vacancies);
    }

    public IActionResult About() => View();

    [HttpGet]
    public IActionResult Contact() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Contact(string name, string email, string subject, string message)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(message))
        {
            ViewBag.ContactError = "Please complete your name, email and message.";
            ViewBag.Name = name;
            ViewBag.Email = email;
            ViewBag.Subject = subject;
            ViewBag.Message = message;
            return View();
        }

        TempData["ContactSuccess"] = "Thanks for getting in touch. Your message has been received by the recruitment team.";
        return RedirectToAction(nameof(Contact));
    }

    public IActionResult Error() => View();
}
