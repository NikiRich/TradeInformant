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
        public string? RealGDP { get; set; }
        public int RealGDPperCapitaPeriod { get; set; }
        public int CPIperiod { get; set; }
        public string? CPI { get; set; }
        public int InflationPeriod { get; set; }
        public int UnemploymentRatePeriod { get; set; }
        public string RealGDP_name = "REAL_GDP";
        public string CPI_name = "CPI";
        public string RealGDPperCapita = "REAL_GDP_PER_CAPITA";
        public string Inflation = "INFLATION";
        public string UnemploymentRate = "UNEMPLOYMENT";

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
        public string GetCacheFileName(string RealGDP_name)
        {
            // Create the file name
            var fileName = $"cache_{RealGDP_name}.json";
            // Return the path to the cache file
            return Path.Combine(CacheDirectory, fileName);
        }


        // Function to load the cache from the file
        public Dictionary<string, dynamic>? LoadCacheFromFile(string RealGDP_name)
        {
            // Get the path to the cache file
            string filePath = GetCacheFileName(RealGDP_name);

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
                    Console.WriteLine($"Error reading or deserializing cache for {RealGDP_name}: {e.Message}");
                }
            }
            // Return null if the cache is not valid
            return null;
        }




        // Function to save the cache to the file
        public void SaveCacheToFile(Dictionary<string, dynamic> data, string RealGDP_name)
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
            string filePath = GetCacheFileName(RealGDP_name);

            // Write the cache to the file in a thread-safe manner
            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry));
            }
        }


        public IActionResult OnGetRGDP(int? RealGDPperiod, string? RealGDP)
        {
            if (!RealGDPperiod.HasValue || RealGDPperiod.Value <= 0)
            {
                return BadRequest("Invalid number of entries requested.");
            }

            this.RealGDPperiod = RealGDPperiod.Value;
            this.RealGDP = RealGDP;
            switch (RealGDP)
            {
                case "Annualy":
                    RealGDP = "annual";
                    break;
                case "Quarterly":
                    RealGDP = "quarterly";
                    break;
            }
            const string API_KEY = "1F6SLA57L4NZM1DR";
            string url = $"https://www.alphavantage.co/query?function={RealGDP_name}&interval={RealGDP}&apikey={API_KEY}";

            Uri uri = new Uri(url);
            _ = LoadCacheFromFile(RealGDP_name);
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

                    SaveCacheToFile(json_data, RealGDP_name);
                    return new JsonResult(json_data);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        public class CacheEntry2
        {
            // Dictionary to store the data
            public Dictionary<string, dynamic>? Data2 { get; set; }
            // Timestamp to store the time when the data was cached
            public DateTime Timestamp2 { get; set; }
        }

        public string GetCacheFileName2(string RealGDPperCapita)
        {
            // Create the file name
            var fileName = $"cache_{RealGDPperCapita}.json";
            // Return the path to the cache file
            return Path.Combine(CacheDirectory, fileName);
        }


        // Function to load the cache from the file
        public Dictionary<string, dynamic>? LoadCacheFromFile2(string RealGDPperCapita)
        {
            // Get the path to the cache file
            string filePath = GetCacheFileName2(RealGDPperCapita);

            // Check if the file exists
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    // Read the file
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    // Deserialize the file
                    var cacheEntry2 = JsonSerializer.Deserialize<CacheEntry2>(jsonString);
                    // Check if the cache is valid
                    if (cacheEntry2 != null && DateTime.UtcNow - cacheEntry2.Timestamp2 < CacheDuration)
                    {
                        // Return the data
                        return cacheEntry2.Data2 ?? new Dictionary<string, dynamic>();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading or deserializing cache for {RealGDPperCapita}: {e.Message}");
                }
            }
            // Return null if the cache is not valid
            return null;
        }




        // Function to save the cache to the file
        public void SaveCacheToFile2(Dictionary<string, dynamic> data, string RealGDPperCapita)
        {
            // Create the cache entry
            var cacheEntry2 = new CacheEntry2
            {
                // Store the data
                Data2 = data,
                // Store the current time
                Timestamp2 = DateTime.UtcNow
            };
            // Get the path to the cache file
            string filePath = GetCacheFileName2(RealGDPperCapita);

            // Write the cache to the file in a thread-safe manner
            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry2));
            }
        }


        public IActionResult OnGetRGDPpC(int? RealGDPperCapitaPeriod)
        {
            if (!RealGDPperCapitaPeriod.HasValue || RealGDPperCapitaPeriod.Value <= 0)
            {
                return BadRequest("Invalid number of entries requested.");
            }

            this.RealGDPperCapitaPeriod = RealGDPperCapitaPeriod.Value;

            const string API_KEY = "1F6SLA57L4NZM1DR";
            string url = $"https://www.alphavantage.co/query?function={RealGDPperCapita}&apikey={API_KEY}";

            Uri uri = new Uri(url);
            _ = LoadCacheFromFile2(RealGDPperCapita);
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
                        json_data["data"] = dataPoints.Take(RealGDPperCapitaPeriod.Value).ToList();
                    }

                    SaveCacheToFile2(json_data, RealGDPperCapita);
                    return new JsonResult(json_data);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        public class CacheEntry3
        {
            // Dictionary to store the data
            public Dictionary<string, dynamic>? Data3 { get; set; }
            // Timestamp to store the time when the data was cached
            public DateTime Timestamp3 { get; set; }
        }

        public string GetCacheFileName3(string CPI_name)
        {
            // Create the file name
            var fileName = $"cache_{CPI_name}.json";
            // Return the path to the cache file
            return Path.Combine(CacheDirectory, fileName);
        }


        // Function to load the cache from the file
        public Dictionary<string, dynamic>? LoadCacheFromFile3(string CPI_name)
        {
            // Get the path to the cache file
            string filePath = GetCacheFileName3(CPI_name);

            // Check if the file exists
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    // Read the file
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    // Deserialize the file
                    var cacheEntry3 = JsonSerializer.Deserialize<CacheEntry3>(jsonString);
                    // Check if the cache is valid
                    if (cacheEntry3 != null && DateTime.UtcNow - cacheEntry3.Timestamp3 < CacheDuration)
                    {
                        // Return the data
                        return cacheEntry3.Data3 ?? new Dictionary<string, dynamic>();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading or deserializing cache for {CPI_name}: {e.Message}");
                }
            }
            // Return null if the cache is not valid
            return null;
        }




        // Function to save the cache to the file
        public void SaveCacheToFile3(Dictionary<string, dynamic> data, string CPI_name)
        {
            // Create the cache entry
            var cacheEntry3 = new CacheEntry3
            {
                // Store the data
                Data3 = data,
                // Store the current time
                Timestamp3 = DateTime.UtcNow
            };
            // Get the path to the cache file
            string filePath = GetCacheFileName3(CPI_name);

            // Write the cache to the file in a thread-safe manner
            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry3));
            }
        }

        public IActionResult OnGetCPI(int? CPIperiod, string? CPI)
        {
            if (!CPIperiod.HasValue || CPIperiod.Value <= 0)
            {
                return BadRequest("Invalid number of entries requested.");
            }
            this.CPIperiod = CPIperiod.Value;
            this.CPI = CPI;

            switch (CPI)
            {
                case "Monthly":
                    CPI = "monthly";
                    break;
                case "SemiAnnualy":
                    CPI = "semiannual";
                    break;
            }

            const string API_KEY = "1F6SLA57L4NZM1DR";
            string url = $"https://www.alphavantage.co/query?function={CPI_name}&interval={CPI}&apikey={API_KEY}";
            Uri uri = new Uri(url);
            _ = LoadCacheFromFile3(CPI_name);
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
                        json_data["data"] = dataPoints.Take(CPIperiod.Value).ToList();
                    }

                    SaveCacheToFile3(json_data, CPI_name);
                    return new JsonResult(json_data);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        public class CacheEntry4
        {
            // Dictionary to store the data
            public Dictionary<string, dynamic>? Data4 { get; set; }
            // Timestamp to store the time when the data was cached
            public DateTime Timestamp4 { get; set; }
        }

        public string GetCacheFileName4(string Inflation)
        {
            // Create the file name
            var fileName = $"cache_{Inflation}.json";
            // Return the path to the cache file
            return Path.Combine(CacheDirectory, fileName);
        }


        // Function to load the cache from the file
        public Dictionary<string, dynamic>? LoadCacheFromFile4(string Inflation)
        {
            // Get the path to the cache file
            string filePath = GetCacheFileName4(Inflation);

            // Check if the file exists
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    // Read the file
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    // Deserialize the file
                    var cacheEntry4 = JsonSerializer.Deserialize<CacheEntry4>(jsonString);
                    // Check if the cache is valid
                    if (cacheEntry4 != null && DateTime.UtcNow - cacheEntry4.Timestamp4 < CacheDuration)
                    {
                        // Return the data
                        return cacheEntry4.Data4 ?? new Dictionary<string, dynamic>();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading or deserializing cache for {Inflation}: {e.Message}");
                }
            }
            // Return null if the cache is not valid
            return null;
        }




        // Function to save the cache to the file
        public void SaveCacheToFile4(Dictionary<string, dynamic> data, string Inflation)
        {
            // Create the cache entry
            var cacheEntry4 = new CacheEntry4
            {
                // Store the data
                Data4 = data,
                // Store the current time
                Timestamp4 = DateTime.UtcNow
            };
            // Get the path to the cache file
            string filePath = GetCacheFileName4(Inflation);

            // Write the cache to the file in a thread-safe manner
            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry4));
            }
        }


        public IActionResult OnGetInflation(int? InflationPeriod)
        {
            if (!InflationPeriod.HasValue || InflationPeriod.Value <= 0)
            {
                return BadRequest("Invalid number of entries requested.");
            }

            this.InflationPeriod = InflationPeriod.Value;

            const string API_KEY = "1F6SLA57L4NZM1DR";
            string url = $"https://www.alphavantage.co/query?function={Inflation}&apikey={API_KEY}";

            Uri uri = new Uri(url);
            _ = LoadCacheFromFile4(Inflation);
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
                        json_data["data"] = dataPoints.Take(InflationPeriod.Value).ToList();
                    }

                    SaveCacheToFile4(json_data, Inflation);
                    return new JsonResult(json_data);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        public class CacheEntry5
        {
            // Dictionary to store the data
            public Dictionary<string, dynamic>? Data5 { get; set; }
            // Timestamp to store the time when the data was cached
            public DateTime Timestamp5 { get; set; }
        }

        public string GetCacheFileName5(string UnemploymentRate)
        {
            // Create the file name
            var fileName = $"cache_{UnemploymentRate}.json";
            // Return the path to the cache file
            return Path.Combine(CacheDirectory, fileName);
        }


        // Function to load the cache from the file
        public Dictionary<string, dynamic>? LoadCacheFromFile5(string UnemploymentRate)
        {
            // Get the path to the cache file
            string filePath = GetCacheFileName5(UnemploymentRate);

            // Check if the file exists
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    // Read the file
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    // Deserialize the file
                    var cacheEntry5 = JsonSerializer.Deserialize<CacheEntry5>(jsonString);
                    // Check if the cache is valid
                    if (cacheEntry5 != null && DateTime.UtcNow - cacheEntry5.Timestamp5 < CacheDuration)
                    {
                        // Return the data
                        return cacheEntry5.Data5 ?? new Dictionary<string, dynamic>();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading or deserializing cache for {UnemploymentRate}: {e.Message}");
                }
            }
            // Return null if the cache is not valid
            return null;
        }




        // Function to save the cache to the file
        public void SaveCacheToFile5(Dictionary<string, dynamic> data, string UnemploymentRate)
        {
            // Create the cache entry
            var cacheEntry4 = new CacheEntry5
            {
                // Store the data
                Data5 = data,
                // Store the current time
                Timestamp5 = DateTime.UtcNow
            };
            // Get the path to the cache file
            string filePath = GetCacheFileName5(UnemploymentRate);

            // Write the cache to the file in a thread-safe manner
            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry4));
            }
        }


        public IActionResult OnGetUnemployment(int? UnemploymentRatePeriod)
        {
            if (!UnemploymentRatePeriod.HasValue || UnemploymentRatePeriod.Value <= 0)
            {
                return BadRequest("Invalid number of entries requested.");
            }

            this.UnemploymentRatePeriod = UnemploymentRatePeriod.Value;

            const string API_KEY = "1F6SLA57L4NZM1DR";
            string url = $"https://www.alphavantage.co/query?function={UnemploymentRate}&apikey={API_KEY}";

            Uri uri = new Uri(url);
            _ = LoadCacheFromFile5(UnemploymentRate);
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
                        json_data["data"] = dataPoints.Take(UnemploymentRatePeriod.Value).ToList();
                    }

                    SaveCacheToFile5(json_data, UnemploymentRate);
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
