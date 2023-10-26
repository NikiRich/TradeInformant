using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using static TradeInformant.Pages.StocksModel;

namespace TradeInformant.Pages
{
    public class CryptoModel : PageModel
    {
        public string stockName { get; set; }
        public string interval { get; set; }
        public string market { get; set; }
        public int periods { get; set; }

        private static readonly string cryptoFile = "cryptoCache.json";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7);

        public class CacheEntry
        {
            public Dictionary<string, dynamic> Data { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public Dictionary<string, dynamic> LoadCacheFromFile(string path)
        {
            if (System.IO.File.Exists(cryptoFile))
            {
                var jsonString = System.IO.File.ReadAllText(cryptoFile);
                var cacheEntry = JsonSerializer.Deserialize<CacheEntry>(jsonString);

                if (DateTime.UtcNow - cacheEntry.Timestamp < CacheDuration)
                {
                    return cacheEntry.Data;
                }
            }
            return null;
        }

        public void SaveCacheToFile(Dictionary<string, dynamic> data, string path)
        {
            var cacheEntry = new CacheEntry
            {
                Data = data,
                Timestamp = DateTime.UtcNow
            };

            lock (cryptoFile)
            {
                System.IO.File.WriteAllText(cryptoFile, JsonSerializer.Serialize(cacheEntry));
            }
        }


        public IActionResult OnGet(string? stockName, string? interval, string? market, int? periods)
        {
            if (stockName == null || interval == null || market == null || periods == null)
            {
                return Page();
            }

            this.stockName = stockName;
            this.interval = interval;
            this.market = market;
            this.periods = (int)periods;

            const string API_KEY = "1F6SLA57L4NZM1DR";

            string function;

            switch (interval)
            {
                case "Daily":
                    function = "DIGITAL_CURRENCY_DAILY";
                    break;
                case "Weekly":
                    function = "DIGITAL_CURRENCY_WEEKLY";
                    break;
                case "Monthly":
                    function = "DIGITAL_CURRENCY_MONTHLY";
                    break;
                default:
                    return new BadRequestObjectResult($"Invalid interval: {interval}");
            }

            string url = $"https://www.alphavantage.co/query?function={function}&symbol={stockName}&market={market}&apikey={API_KEY}";
            Uri uri = new Uri(url);

            Dictionary<string, dynamic> jsonInfo = LoadCacheFromFile(cryptoFile);

            if (jsonInfo == null)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        jsonInfo = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(client.DownloadString(uri));

                        if (jsonInfo == null)
                        {
                            Console.WriteLine($"Interval: {interval}, CryptoName: {stockName}, Market: {market}");
                            return new BadRequestObjectResult("Error retrieving data for the particular cryptocurrency or invalid data format");
                        }
                        return new JsonResult(jsonInfo);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching cryptocurrency data for {stockName} with interval {interval} and market {market}: {ex.Message}");
                    return new BadRequestObjectResult("Error fetching cryptocurrency data. Please try again later.");
                }
            }
            return new JsonResult(jsonInfo);
        }
    }
}
