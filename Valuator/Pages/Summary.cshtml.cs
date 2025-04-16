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
    private readonly ILogger<SummaryModel> _logger;
    private readonly IDatabase _mainDb;
    private readonly IDatabase _ruDb;
    private readonly IDatabase _euDb;
    private readonly IDatabase _asiaDb;


    public SummaryModel(ILogger<SummaryModel> logger,       [FromKeyedServices("MainRedis")] IConnectionMultiplexer mainRedis,
        [FromKeyedServices("RURedis")] IConnectionMultiplexer ruRedis,
        [FromKeyedServices("EURedis")] IConnectionMultiplexer euRedis,
        [FromKeyedServices("ASIARedis")] IConnectionMultiplexer asiaRedis)
    {
        _logger = logger;
       
        _mainDb = mainRedis.GetDatabase();
        _ruDb = ruRedis.GetDatabase();
        _euDb = euRedis.GetDatabase();
        _asiaDb = asiaRedis.GetDatabase();
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public async Task OnGetAsync(string id)
    {
        string region = _mainDb.StringGet($"TEXT-{id}");
        string rankKey = "RANK-" + id;
        string similarityKey = "SIMILARITY-" + id;
        string rankValue =  await getDb(region).StringGetAsync(rankKey);

        while (rankValue == null)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            rankValue = await  getDb(region).StringGetAsync(rankKey);
        }
        string similarityValue = getDb(region).StringGet(similarityKey);
        _logger.LogInformation($"id : {id} | Rank: {rankValue} | Similarity: {similarityValue}");
        Rank = double.Parse(rankValue);
        Similarity = double.Parse(similarityValue);
    }
    
    private IDatabase getDb(string region)
    {
        if (region == "RU")
        {
            return _ruDb;
        } else if (region == "EU")
        {
            return _euDb;
        } else
        {
            return _asiaDb;
        } 
    }
}
