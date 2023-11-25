using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Net;
using System;
using System.Text.Json.Serialization.Metadata;
using System.Security.Cryptography.X509Certificates;

namespace TradeInformant.Pages
{
    public class US_Economic_IndicatorsModel : PageModel
    {
        public int RealGDPperiod { get; set; }
        public string RealGDP = "REAL_GDP";

        // Variable to store the environment
        private readonly IWebHostEnvironment _env;

        // Duration for which the cached files are valid
        private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(7);

        // Path to the directory where cached files are stored
        private string CacheDirectory => Path.Combine(_env.ContentRootPath, "CachedFiles");


        // Interface that provides information about the web hosting environment an application is running in
        public US_Economic_IndicatorsModel(IWebHostEnvironment env)
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
        public string GetCacheFileName(string RealGDP)
        {
            // Create the file name
            var fileName = $"cache_{RealGDP}.json";
            // Return the path to the cache file
            return Path.Combine(CacheDirectory, fileName);
        }


        // Function to load the cache from the file
        public Dictionary<string, dynamic>? LoadCacheFromFile(string RealGDP)
        {
            // Get the path to the cache file
            string filePath = GetCacheFileName(RealGDP);

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
                    Console.WriteLine($"Error reading or deserializing cache for {RealGDP}: {e.Message}");
                }
            }
            // Return null if the cache is not valid
            return null;
        }




        // Function to save the cache to the file
        public void SaveCacheToFile(Dictionary<string, dynamic> data, string RealGDP)
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
            string filePath = GetCacheFileName(RealGDP);

            // Write the cache to the file in a thread-safe manner
            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry));
            }
        }
        public IActionResult OnGetRGDP(int? RealGDPperiod)
        {
            if (!RealGDPperiod.HasValue || RealGDPperiod.Value <= 0)
            {
                return BadRequest("Invalid number of entries requested.");
            }

            this.RealGDPperiod = RealGDPperiod.Value;

            const string API_KEY = "1F6SLA57L4NZM1DR";
            string url = $"https://www.alphavantage.co/query?function=REAL_GDP&interval=monthly&apikey={API_KEY}";

            Uri uri = new Uri(url);
            _ = LoadCacheFromFile(RealGDP);
            try
            {
                using (WebClient client = new WebClient())
                {
                    string jsonString = client.DownloadString(uri);
                    Dictionary<string, dynamic>? json_data = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(jsonString);
                    // Assuming json_data contains a "data" key which is an array of data points
                    // Trim the data to the requested number of entries if necessary
                    if (json_data.ContainsKey("data") && json_data["data"] is List<dynamic> dataPoints)
                    {
                        json_data["data"] = dataPoints.Take(RealGDPperiod.Value).ToList();
                    }

                    SaveCacheToFile(json_data, RealGDP);
                    return new JsonResult(json_data);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

    }
}
