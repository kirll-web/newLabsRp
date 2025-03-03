using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
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

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public async Task OnGetAsync(string id)
    {
        string rankKey = "RANK-" + id;
        string similarityKey = "SIMILARITY-" + id;

        string rankValue = _redisDb.StringGet(rankKey);
        string similarityValue = _redisDb.StringGet(similarityKey);

        Rank = double.Parse(rankValue);
        Similarity = double.Parse(similarityValue);
    }
}
