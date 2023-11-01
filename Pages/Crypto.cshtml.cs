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
    public class CryptoModel : PageModel
    {
        public string? cryptoName { get; set; }
        public string? Interval { get; set; }
        public string? market { get; set; }
        public int Periods { get; set; }

        private readonly IWebHostEnvironment _env;

        private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7);

        private string CacheDirectory => Path.Combine(_env.ContentRootPath, "CachedFiles");



        public CryptoModel(IWebHostEnvironment env)
        {
            _env = env;
        }



        public class CacheEntry
        {
            public Dictionary<string, dynamic>? Data { get; set; }
            public DateTime Timestamp { get; set; }
        }



        public string GetCacheFileName(string cryptoName, string Interval)
        {
            var fileName = $"cache_{cryptoName}_{Interval}.json";
            return Path.Combine(CacheDirectory, fileName);
        }



        public Dictionary<string, dynamic> LoadCacheFromFile(string cryptoName, string Interval)
        {
            string filePath = GetCacheFileName(cryptoName, Interval);

            if (System.IO.File.Exists(filePath))
            {
                var jsonString = System.IO.File.ReadAllText(filePath);
                var cacheEntry = JsonSerializer.Deserialize<CacheEntry>(jsonString);

                if (cacheEntry != null && DateTime.UtcNow - cacheEntry.Timestamp < CacheDuration)
                {
                    return cacheEntry.Data ?? new Dictionary<string, dynamic>();
                }
            }
            return new Dictionary<string, dynamic>();
        }



        public void SaveCacheToFile(Dictionary<string, dynamic> data, string cryptoName, string Interval)
        {
            var cacheEntry = new CacheEntry
            {
                Data = data,
                Timestamp = DateTime.UtcNow
            };

            string filePath = GetCacheFileName(cryptoName, Interval);

            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry));
            }
        }


        public IActionResult OnGet(string? cryptoName, string? Interval, string? market, int? Periods)
        {
            if (cryptoName == null || Interval == null || Periods == null)
            {
                Console.WriteLine($"Parameters are missing");
                return Page();
            }

            this.cryptoName = cryptoName;
            this.Interval = Interval;
            this.market = market;
            this.Periods = (int)Periods;

            const string API_KEY = "1F6SLA57L4NZM1DR";

            string function;

            switch (Interval)
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
                    return new BadRequestObjectResult($"Invalid Interval: {Interval}");
            }


            string url = $"https://www.alphavantage.co/query?function={function}&symbol={cryptoName}&market={market}&apikey={API_KEY}";
            Uri uri = new Uri(url);

            Dictionary<string, dynamic> jsonInfo = LoadCacheFromFile(cryptoName, Interval);

            if (jsonInfo == null)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        jsonInfo = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(client.DownloadString(uri));

                        if (jsonInfo == null)
                        {
                            Console.WriteLine($"Interval: {Interval}, cryptoName: {cryptoName}, Market: {market}");
                            return new BadRequestObjectResult("Error retrieving data for the particular cryptocurrency or invalid data format");
                        }

                        SaveCacheToFile(jsonInfo, cryptoName, Interval);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching cryptocurrency data for {cryptoName} with Interval {Interval} and market {market}: {ex.Message}");
                    return new BadRequestObjectResult("Error fetching cryptocurrency data. Please try again later.");
                }
            }

            return new JsonResult(jsonInfo);
        }
    }
}
