using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ExcelSheetsApp.Models;

namespace ExcelSheetsApp.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(
                model.Username, 
                model.Password, 
                model.RememberMe, 
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in.");
                return RedirectToLocal(returnUrl);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
                return View(model);
            }
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        // Register işlemi kapatıldı, login sayfasına yönlendir
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        // Register işlemi kapatıldı, login sayfasına yönlendir
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction(nameof(HomeController.Index), "Home");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}
