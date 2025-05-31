using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class RegistrationModel : PageModel
{
    private readonly ILogger<RegistrationModel> _logger;
    private readonly IDatabase _redisDb;
    public string? ErrorMessage { get; set; }

    public RegistrationModel(ILogger<RegistrationModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redisDb = redis.GetDatabase();
    }

    public async Task<IActionResult> OnPostAsync(string username, string password, string action)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Логин и пароль обязателены.";
            return Page();
        }

        if (action == "register")
        {
            if (_redisDb.KeyExists($"USER-{username}"))
            {
                ErrorMessage = "Пользователь с таким логином уже существует.";
                return Page();
            }

            var hashedPassword = HashPassword(password);
            _redisDb.StringSet($"USER-{username}", hashedPassword);

            
            return RedirectToPage("Index");
        }
        else if (action == "login")
        {
            var storedPassword = _redisDb.StringGet($"USER-{username}");
            if (string.IsNullOrEmpty(storedPassword))
            {
                ErrorMessage = "Пользователь не найден.";
                return Page();
            }

            var hashedPassword = HashPassword(password);
            if (storedPassword != hashedPassword)
            {
                ErrorMessage = "Неверный логин или пароль.";
                return Page();
            }

            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username)
            };

            
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = true, 
                    ExpiresUtc = DateTime.UtcNow.AddDays(7) 
                });

            
            return RedirectToPage("Index");
        }

        
        ErrorMessage = "Произошла ошибка обработки запроса.";
        return Page();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        
        return RedirectToPage("Registration");
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
