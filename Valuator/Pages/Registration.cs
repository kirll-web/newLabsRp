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
            // Логика регистрации
            if (_redisDb.KeyExists($"USER-{username}"))
            {
                ErrorMessage = "Пользователь с таким логином уже существует.";
                return Page();
            }

            var hashedPassword = HashPassword(password);
            _redisDb.StringSet($"USER-{username}", hashedPassword);

            // Перенаправляем на главную страницу после успешной регистрации
            return RedirectToPage("Index");
        }
        else if (action == "login")
        {
            // Логика авторизации
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

            // Создаём claims для пользователя
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username)
            };

            // Создаём claimsIdentity
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Устанавливаем куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = true, // Куки сохраняются при закрытии браузера
                    ExpiresUtc = DateTime.UtcNow.AddDays(7) // Время действия куки
                });

            // Авторизация успешна, перенаправляем на главную страницу
            return RedirectToPage("Index");
        }

        // Если действие не распознано
        ErrorMessage = "Произошла ошибка обработки запроса.";
        return Page();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        // Удаляем куки и удаляем аутентификацию
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Перенаправляем обратно на страницу авторизации
        return RedirectToPage("Registration");
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }
}
