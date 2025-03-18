using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using static System.Net.Mime.MediaTypeNames;

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

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public async Task OnGetAsync(string id)
    {
        string rankKey = "RANK-" + id;
        string similarityKey = "SIMILARITY-" + id;

        string rankValue =  await _redisDb.StringGetAsync(rankKey);

        while (rankValue == null)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            rankValue = await _redisDb.StringGetAsync(rankKey);
        }
        _logger.LogInformation($"OnGetAsync: {id}, {rankValue}");
        string similarityValue = _redisDb.StringGet(similarityKey);
        Console.WriteLine($"OnGetAsync: {rankValue}");
        Rank = double.Parse(rankValue);
        Similarity = double.Parse(similarityValue);
    }
}
