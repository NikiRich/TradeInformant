using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;


namespace TradeInformant.Pages
{
    public class StocksModel : PageModel
    {
        public string? stockName { get; set; }
        public string? interval { get; set; }
        public int periods { get; set; }


        private readonly IWebHostEnvironment _env;

        private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7);
        
        private string CacheDirectory => Path.Combine(_env.ContentRootPath, "CachedFiles");



        public StocksModel(IWebHostEnvironment env)
        {
            _env = env;
        }



        public class CacheEntry
        {
            public Dictionary<string, dynamic>? Data { get; set; }
            public DateTime Timestamp { get; set; }
        }



        public string GetCacheFileName(string stockName, string interval)
        {
            var fileName = $"cache_{stockName}_{interval}.json";
            return Path.Combine(CacheDirectory, fileName);
        }



        public Dictionary<string, dynamic> LoadCacheFromFile(string stockName, string interval)
        {
            string filePath = GetCacheFileName(stockName, interval);

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    var cacheEntry = JsonSerializer.Deserialize<CacheEntry>(jsonString);

                    if (cacheEntry != null && DateTime.UtcNow - cacheEntry.Timestamp < CacheDuration)
                    {
                        return cacheEntry.Data ?? new Dictionary<string, dynamic>();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading or deserializing cache for {stockName} - {interval}: {e.Message}");
                }
            }
            return null;
        }





        public void SaveCacheToFile(Dictionary<string, dynamic> data, string stockName, string interval)
        {
            var cacheEntry = new CacheEntry
            {
                Data = data,
                Timestamp = DateTime.UtcNow
            };

            string filePath = GetCacheFileName(stockName, interval);

            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry));
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

            Dictionary<string, dynamic> jsonInfo = LoadCacheFromFile(stockName, interval);

            if (jsonInfo == null)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        jsonInfo = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(client.DownloadString(uri));

                        if (jsonInfo == null)
                        {
                            Console.WriteLine($"Interval: {interval}, stockName: {stockName}");
                            return new BadRequestObjectResult("Error retrieving data for the particular stock or invalid data format");
                        }

                        SaveCacheToFile(jsonInfo, stockName, interval);
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
