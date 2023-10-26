using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;

namespace TradeInformant.Pages
{
    public class StocksModel : PageModel
    {
        public string stockName { get; set; }
        public string interval { get; set; }
        public int periods { get; set; }

        private static readonly string stockFile = "cache.json";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(72);

        public class CacheEntry
        {
            public Dictionary<string, dynamic> Data { get; set; }
            public DateTime Timestamp { get; set; }
        }

        public Dictionary<string, dynamic> LoadCacheFromFile()
        {
            if (System.IO.File.Exists(stockFile))
            {
                var jsonString = System.IO.File.ReadAllText(stockFile);
                var cacheEntry = JsonSerializer.Deserialize<CacheEntry>(jsonString);

                if (DateTime.UtcNow - cacheEntry.Timestamp < CacheDuration)
                {
                    return cacheEntry.Data;
                }
            }
            return null;
        }

        public void SaveCacheToFile(Dictionary<string, dynamic> data)
        {
            var cacheEntry = new CacheEntry
            {
                Data = data,
                Timestamp = DateTime.UtcNow
            };

            lock (stockFile)
            {
                System.IO.File.WriteAllText(stockFile, JsonSerializer.Serialize(cacheEntry));
            }
        }


        public IActionResult OnGet(string? stockName, string? interval, int? periods)
        {
            if (stockName == null || interval == null || periods == null)
            {
                return Page();
            }

            this.stockName = stockName;
            this.interval = interval;
            this.periods = (int)periods;

            const string API_KEY = "1F6SLA57L4NZM1DR";

            string function;

            switch (interval)
            {
                case "Daily":
                    function = "TIME_SERIES_DAILY";
                    break;
                case "Weekly":
                    function = "TIME_SERIES_WEEKLY";
                    break;
                case "Monthly":
                    function = "TIME_SERIES_MONTHLY";
                    break;
                default:
                    return new BadRequestObjectResult($"Invalid interval: {interval}");
            }

            string url = $"https://www.alphavantage.co/query?function={function}&symbol={stockName}&apikey={API_KEY}";
            Uri uri = new Uri(url);

            Dictionary<string, dynamic> jsonInfo = LoadCacheFromFile();

            if (jsonInfo == null)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        jsonInfo = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(client.DownloadString(uri));

                        if (jsonInfo == null)
                        {
                            Console.WriteLine($"Interval: {interval}, StockName: {stockName}");
                            return new BadRequestObjectResult("Error retrieving data for the particular stock or invalid data format");
                        }

                        SaveCacheToFile(jsonInfo);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching stock data for {stockName} with interval {interval}: {ex.Message}");
                    return new BadRequestObjectResult("Error fetching stock data. Please try again later.");
                }
            }

            return new JsonResult(jsonInfo);
        }
    }
}
