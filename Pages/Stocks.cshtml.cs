using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text.Json;


namespace TradeInformant.Pages
{
    public class StocksModel : PageModel
    {
        public string? StockName { get; set; }
        public string? Interval { get; set; }
        public int Periods { get; set; }


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



        public string GetCacheFileName(string StockName, string Interval)
        {
            var fileName = $"cache_{StockName}_{Interval}.json";
            return Path.Combine(CacheDirectory, fileName);
        }



        public Dictionary<string, dynamic>? LoadCacheFromFile(string StockName, string Interval)
        {
            string filePath = GetCacheFileName(StockName, Interval);

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
                    Console.WriteLine($"Error reading or deserializing cache for {StockName} - {Interval}: {e.Message}");
                }
            }
            return null;
        }





        public void SaveCacheToFile(Dictionary<string, dynamic> data, string StockName, string Interval)
        {
            var cacheEntry = new CacheEntry
            {
                Data = data,
                Timestamp = DateTime.UtcNow
            };

            string filePath = GetCacheFileName(StockName, Interval);

            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry));
            }
        }



        public IActionResult OnGet(string? StockName, string? Interval, int? Periods)
        {
            if (StockName == null || Interval == null || Periods == null)
            {
                return Page();
            }

            this.StockName = StockName;
            this.Interval = Interval;
            this.Periods = (int)Periods;

            const string API_KEY = "1F6SLA57L4NZM1DR";

            string function;

            switch (Interval)
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
                    return new BadRequestObjectResult($"Invalid Interval: {Interval}");
            }


            string url = $"https://www.alphavantage.co/query?function={function}&symbol={StockName}&apikey={API_KEY}";
            Uri uri = new Uri(url);

            Dictionary<string, dynamic>? jsonInfo = LoadCacheFromFile(StockName, Interval);

            if (jsonInfo == null)
            {
                try
                {
                    using (WebClient client = new())
                    {
                        jsonInfo = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(client.DownloadString(uri));

                        if (jsonInfo == null)
                        {
                            Console.WriteLine($"Interval: {Interval}, StockName: {StockName}");
                            return new BadRequestObjectResult("Error retrieving data for the particular stock or invalid data format");
                        }

                        SaveCacheToFile(jsonInfo, StockName, Interval);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching stock data for {StockName} with Interval {Interval}: {ex.Message}");
                    return new BadRequestObjectResult("Error fetching stock data. Please try again later.");
                }
            }

            return new JsonResult(jsonInfo);
        }
    }
}
