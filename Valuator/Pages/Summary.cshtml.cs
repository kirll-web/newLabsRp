using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private readonly IDatabase _redisDb;
    private readonly ILogger<SummaryModel> _logger;
    
    public SummaryModel(ILogger<SummaryModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redisDb = redis.GetDatabase();
    }

    public string Rank { get; set; }
    public double Similarity { get; set; }
    public string Id { get; set; }


    public async Task<IActionResult> 
        OnGetAsync(string id)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return RedirectToPage("/Registration");
        }
        string? author = _redisDb.StringGet($"AUTHOR-{id}");
     
        if (string.IsNullOrEmpty(author) || author != User.Identity.Name)
        {
            // Автор не совпадает с текущим пользователем, доступ запрещён
            return RedirectToPage("/Registration");// Перенаправляем пользователя на страницу с сообщением об ошибке
        }

        Id = id;
        string rankKey = "RANK-" + id;
        string similarityKey = "SIMILARITY-" + id;
        _logger.LogInformation($"OnGetAsync: {"RANK-" + id}");
        string? rankValue = await _redisDb.StringGetAsync(rankKey);
        

        if (rankValue == null)
        {
            rankValue = "считается...";
        }
     
        Rank = rankValue;

        _logger.LogInformation($"OnGetAsync: {id}, {rankValue}");
        string similarityValue = _redisDb.StringGet(similarityKey);
        _logger.LogInformation($"id : {id}SIMILARITY: {similarityValue}");
        Console.WriteLine($"OnGetAsync: {rankValue}");
        Similarity = double.Parse(similarityValue);
        return Page(); // Возвращаем страницу в случае успеха

    }
}
