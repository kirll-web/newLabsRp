using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _redisDb;

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redisDb = redis.GetDatabase();
    }

    public async Task<IActionResult> OnPostAsync(string text)
    {
        string id = Guid.NewGuid().ToString();

        // Сохранение текста
       
        // Расчет Rank
        double rank = CalculateRank(text);
        _redisDb.StringSet($"RANK-{id}", rank.ToString());

        double similarity = CalculateSimilarityAsync(text);
        _redisDb.StringSet($"SIMILARITY-{id}", similarity.ToString());
        _redisDb.StringSet($"TEXT-{id}", text != null ? text : "");

        return Redirect($"summary?id={id}");
    }

    private double CalculateSimilarityAsync(string currentText)
    {
        var keys = _redisDb.Multiplexer.GetServer(_redisDb.Multiplexer.GetEndPoints().First()).Keys(pattern: "TEXT-*");

        foreach (var key in keys)
        {
           try
            {
                var storedText = _redisDb.StringGet(key);
                if (storedText == currentText)
                {
                    return 1;
                }
            } catch(Exception ex)
            {
                continue;
            }
        }

        return 0;
    }

    private double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        int nonAlphabeticCount = 0;
        foreach (char c in text)
        {
            if (!IsAlphabetic(c))
                nonAlphabeticCount++;
        }
        return (double)nonAlphabeticCount / text.Length;
    }

    private bool IsAlphabetic(char c)
    {
        // Проверка на русские и латинские буквы
        return char.IsLetter(c) &&
               (c <= 0x007F ||  // ASCII
                c >= 0x0410 && c <= 0x044F); // Русские буквы
    }

}