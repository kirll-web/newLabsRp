using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class AccessDeniedModel : PageModel
{
    private readonly ILogger<AccessDeniedModel> _logger;
    private readonly IDatabase _redisDb;
    public string? ErrorMessage { get; set; }

    public AccessDeniedModel(ILogger<AccessDeniedModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
    }
}
