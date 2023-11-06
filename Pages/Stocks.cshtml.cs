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

        // Variable to store the environment
        private readonly IWebHostEnvironment _env;

        // Duration for which the cached files are valid
        private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7);
        
        // Path to the directory where cached files are stored
        private string CacheDirectory => Path.Combine(_env.ContentRootPath, "CachedFiles");


        // Interface that provides information about the web hosting environment an application is running in
        public StocksModel(IWebHostEnvironment env)
        {
            _env = env;
        }


        // Class to store the cache entry
        public class CacheEntry
        {
            // Dictionary to store the data
            public Dictionary<string, dynamic>? Data { get; set; }
            // Timestamp to store the time when the data was cached
            public DateTime Timestamp { get; set; }
        }


        // Function to get the cache file name
        public string GetCacheFileName(string StockName, string Interval)
        {
            // Create the file name
            var fileName = $"cache_{StockName}_{Interval}.json";
            // Return the path to the cache file
            return Path.Combine(CacheDirectory, fileName);
        }


        // Function to load the cache from the file
        public Dictionary<string, dynamic>? LoadCacheFromFile(string StockName, string Interval)
        {
            // Get the path to the cache file
            string filePath = GetCacheFileName(StockName, Interval);

            // Check if the file exists
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    // Read the file
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    // Deserialize the file
                    var cacheEntry = JsonSerializer.Deserialize<CacheEntry>(jsonString);
                    // Check if the cache is valid
                    if (cacheEntry != null && DateTime.UtcNow - cacheEntry.Timestamp < CacheDuration)
                    {
                        // Return the data
                        return cacheEntry.Data ?? new Dictionary<string, dynamic>();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading or deserializing cache for {StockName} - {Interval}: {e.Message}");
                }
            }
            // Return null if the cache is not valid
            return null;
        }




        // Function to save the cache to the file
        public void SaveCacheToFile(Dictionary<string, dynamic> data, string StockName, string Interval)
        {
            // Create the cache entry
            var cacheEntry = new CacheEntry
            {
                // Store the data
                Data = data,
                // Store the current time
                Timestamp = DateTime.UtcNow
            };
            // Get the path to the cache file
            string filePath = GetCacheFileName(StockName, Interval);

            // Write the cache to the file in a thread-safe manner
            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry));
            }
        }


        // Function to get the stock data
        public IActionResult OnGet(string? StockName, string? Interval, int? Periods)
        {
            // Check if the parameters are null
            if (StockName == null || Interval == null || Periods == null)
            {
                // Return the page
                return Page();
            }

            // Store the parameters
            this.StockName = StockName;
            this.Interval = Interval;
            this.Periods = (int)Periods;

            // API key for Alpha Vantage
            const string API_KEY = "1F6SLA57L4NZM1DR";

            string function;

            // Check the interval
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
                    // Return an error if the interval is invalid
                    return new BadRequestObjectResult($"Invalid Interval: {Interval}");
            }

            // Create the URL to fetch the data
            string url = $"https://www.alphavantage.co/query?function={function}&symbol={StockName}&apikey={API_KEY}";
            // Create the URI to have the URL in a proper format
            Uri uri = new Uri(url);

            // Load the cache from the file
            Dictionary<string, dynamic>? jsonInfo = LoadCacheFromFile(StockName, Interval);

            // Check if the cache is null
            if (jsonInfo == null)
            {
                try
                {
                    // Fetch the data from the API
                    using (WebClient client = new())
                    {
                        // Download the data
                        jsonInfo = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(client.DownloadString(uri));

                        // Check if the data is null
                        if (jsonInfo == null)
                        {
                            Console.WriteLine($"Interval: {Interval}, StockName: {StockName}");
                            // Return an error if the data is null
                            return new BadRequestObjectResult("Error retrieving data for the particular stock or invalid data format");
                        }

                        // Save the data to the cache
                        SaveCacheToFile(jsonInfo, StockName, Interval);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching stock data for {StockName} with Interval {Interval}: {ex.Message}");
                    // Return an error if the data cannot be fetched
                    return new BadRequestObjectResult("Error fetching stock data. Please try again later.");
                }
            }
            // Return the data
            return new JsonResult(jsonInfo);
        }
    }
}
