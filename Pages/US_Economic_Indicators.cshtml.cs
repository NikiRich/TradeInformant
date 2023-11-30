using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Net;

namespace TradeInformant.Pages
{
    public class US_Economic_IndicatorsModel : PageModel
    {
        // Variables to store the number of entries requested
        public int RealGDPperiod { get; set; }
        public string? RealGDP { get; set; }
        public int RealGDPperCapitaPeriod { get; set; }
        public int CPIperiod { get; set; }
        public string? CPI { get; set; }
        public int InflationPeriod { get; set; }
        public int UnemploymentRatePeriod { get; set; }

        // Variables to store the name of the API
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
        public string GetCache(string RealGDP_name)
        {
            // Creating the file name
            var fileName = $"cache_{RealGDP_name}.json";
            // Returing the path to the cache file
            return Path.Combine(CacheDirectory, fileName);
        }


        // Function to load the cache from the file
        public Dictionary<string, dynamic>? LoadCache(string RealGDP_name)
        {
            // Getting the path to the cache file
            string filePath = GetCache(RealGDP_name);

            // Checking if the file exists
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    // Reading the file
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    // Deserializing the file
                    var cacheEntry = JsonSerializer.Deserialize<CacheEntry>(jsonString);
                    // Checking if the cache is valid
                    if (cacheEntry != null && DateTime.UtcNow - cacheEntry.Timestamp < CacheDuration)
                    {
                        // Returning the data
                        return cacheEntry.Data ?? new Dictionary<string, dynamic>();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading or deserializing cache for {RealGDP_name}: {e.Message}");
                }
            }
            // Returning null if the cache is not valid
            return null;
        }




        // Function to save the cache to the file
        public void SaveCache(Dictionary<string, dynamic> data, string RealGDP_name)
        {
            // Creating the cache entry
            var cacheEntry = new CacheEntry
            {
                // Storing the data
                Data = data,
                // Store the current time
                Timestamp = DateTime.UtcNow
            };
            // Getting the path to the cache file
            string filePath = GetCache(RealGDP_name);

            // Writing the cache to the file in a thread-safe manner
            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry));
            }
        }

        // Function to get the data from the API
        public IActionResult OnGetRGDP(int? RealGDPperiod, string? RealGDP)
        {
            this.RealGDPperiod = RealGDPperiod.Value;
            this.RealGDP = RealGDP;
            // Checking if the number of entries requested is valid
            if (!RealGDPperiod.HasValue || RealGDPperiod.Value <= 0 || RealGDPperiod.Value > 100)
            {
                return BadRequest("Invalid number of entries requested.");
            }

            // Switch statement to convert the string to the required format for the API
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
            // Creating the URL to get the data from the API
            string url = $"https://www.alphavantage.co/query?function={RealGDP_name}&interval={RealGDP}&apikey={API_KEY}";
            // Creating the URI to handle the URL
            Uri uri = new Uri(url);
            // Loading the cache
            _ = LoadCache(RealGDP_name);

            try
            {
                // Creating a new WebClient
                using (WebClient client = new WebClient())
                {
                    // Downloading the data from the API
                    string jsonString = client.DownloadString(uri);
                    Dictionary<string, dynamic>? json_data = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(jsonString);
                    // Checking if the dictionary contains a "data" key which is an array of data points
                    if (json_data.ContainsKey("data") && json_data["data"] is List<dynamic> DataPoints)
                    {
                        // Trimming the data to the requested number of entries if necessary
                        json_data["data"] = DataPoints.Take(RealGDPperiod.Value).ToList();
                    }

                    // Saving the cache
                    SaveCache(json_data, RealGDP_name);
                    // Returning the data as a JSON object
                    return new JsonResult(json_data);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // Second class to store the cache entry
        public class CacheEntry2
        {
            // Dictionary to store the data
            public Dictionary<string, dynamic>? Data2 { get; set; }
            // Timestamp to store the time when the data was cached
            public DateTime Timestamp2 { get; set; }
        }

        public string GetCache2(string RealGDPperCapita)
        {
            // Creating the file name
            var fileName = $"cache_{RealGDPperCapita}.json";
            // Returning the path to the cache file
            return Path.Combine(CacheDirectory, fileName);
        }


        // Function to load the cache from the file
        public Dictionary<string, dynamic>? LoadCache2(string RealGDPperCapita)
        {
            // Getting the path to the cache file
            string filePath = GetCache2(RealGDPperCapita);

            // Checking if the file exists
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    // Reading the file
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    // Deserializing the file
                    var cacheEntry2 = JsonSerializer.Deserialize<CacheEntry2>(jsonString);
                    // Checking if the cache is valid
                    if (cacheEntry2 != null && DateTime.UtcNow - cacheEntry2.Timestamp2 < CacheDuration)
                    {
                        // Returning the data
                        return cacheEntry2.Data2 ?? new Dictionary<string, dynamic>();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading or deserializing cache for {RealGDPperCapita}: {e.Message}");
                }
            }
            // Returning null if the cache is not valid
            return null;
        }


        // Function to save the cache to the file
        public void SaveCache2(Dictionary<string, dynamic> data, string RealGDPperCapita)
        {
            // Creating the cache entry
            var cacheEntry2 = new CacheEntry2
            {
                // Storing the data
                Data2 = data,
                // Storing the current time
                Timestamp2 = DateTime.UtcNow
            };
            // Getting the path to the cache file
            string filePath = GetCache2(RealGDPperCapita);

            // Writing the cache to the file in a thread-safe manner
            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry2));
            }
        }

        // Function to get the data from the API
        public IActionResult OnGetRGDPpC(int? RealGDPperCapitaPeriod)
        {
            this.RealGDPperCapitaPeriod = RealGDPperCapitaPeriod.Value;

            // Checking if the number of entries requested is valid
            if (!RealGDPperCapitaPeriod.HasValue || RealGDPperCapitaPeriod.Value <= 0 || RealGDPperCapitaPeriod.Value > 100)
            {
                return BadRequest("Invalid number of entries requested.");
            }

            const string API_KEY = "1F6SLA57L4NZM1DR";
            // Creating the URL to get the data from the API
            string url = $"https://www.alphavantage.co/query?function={RealGDPperCapita}&apikey={API_KEY}";
            // Creating the URI to handle the URL
            Uri uri = new Uri(url);
            // Loading the cache
            _ = LoadCache2(RealGDPperCapita);
            try
            {
                // Creating a new WebClient
                using (WebClient client = new WebClient())
                {
                    // Downloading the data from the API
                    string jsonString = client.DownloadString(uri);
                    // Deserializing the file to a dictionary
                    Dictionary<string, dynamic>? json_data = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(jsonString);
                    // Checking if the dictionary contains a "data" key which is an array of data points
                    if (json_data.ContainsKey("data") && json_data["data"] is List<dynamic> DataPoints)
                    {
                        // Trimming the data to the requested number of entries if necessary
                        json_data["data"] = DataPoints.Take(RealGDPperCapitaPeriod.Value).ToList();
                    }

                    // Saving the cache
                    SaveCache2(json_data, RealGDPperCapita);
                    // Returning the data as a JSON object
                    return new JsonResult(json_data);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // Third class to store the cache entry
        public class CacheEntry3
        {
            // Dictionary to store the data
            public Dictionary<string, dynamic>? Data3 { get; set; }
            // Timestamp to store the time when the data was cached
            public DateTime Timestamp3 { get; set; }
        }

        public string GetCache3(string CPI_name)
        {
            // Creating the file name
            var fileName = $"cache_{CPI_name}.json";
            // Returning the path to the cache file
            return Path.Combine(CacheDirectory, fileName);
        }


        public Dictionary<string, dynamic>? LoadCache3(string CPI_name)
        {
            // Getting the path to the cache file
            string filePath = GetCache3(CPI_name);

            // Checking if the file exists
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    // Reading the file
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    // Deserializing the file
                    var cacheEntry3 = JsonSerializer.Deserialize<CacheEntry3>(jsonString);
                    // Checking if the cache is valid
                    if (cacheEntry3 != null && DateTime.UtcNow - cacheEntry3.Timestamp3 < CacheDuration)
                    {
                        // Returning the data
                        return cacheEntry3.Data3 ?? new Dictionary<string, dynamic>();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading or deserializing cache for {CPI_name}: {e.Message}");
                }
            }
            // Returning null if the cache is not valid
            return null;
        }


        public void SaveCache3(Dictionary<string, dynamic> data, string CPI_name)
        {
            // Createing the cache entry
            var cacheEntry3 = new CacheEntry3
            {
                // Storing the data
                Data3 = data,
                // Storing the current time
                Timestamp3 = DateTime.UtcNow
            };
            // Gettting the path to the cache file
            string filePath = GetCache3(CPI_name);

            // Writing the cache to the file in a thread-safe manner
            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry3));
            }
        }

        // Function to get the data from the API
        public IActionResult OnGetCPI(int? CPIperiod, string? CPI)
        {
            this.CPIperiod = CPIperiod.Value;
            this.CPI = CPI;
            // Checking if the number of entries requested is valid
            if (!CPIperiod.HasValue || CPIperiod.Value <= 0 || CPIperiod.Value > 100)
            {
                return BadRequest("Invalid number of entries requested.");
            }
            // Switch statement to convert the string to the required format for the API
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
            // Creating the URL to get the data from the API
            string url = $"https://www.alphavantage.co/query?function={CPI_name}&interval={CPI}&apikey={API_KEY}";
            // Creating the URI to handle the URL
            Uri uri = new Uri(url);
            // Loading the cache
            _ = LoadCache3(CPI_name);

            try
            {
                // Creating a new WebClient
                using (WebClient client = new WebClient())
                {
                    // Downloading the data from the API
                    string jsonString = client.DownloadString(uri);
                    Dictionary<string, dynamic>? json_data = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(jsonString);
                    // Checking if the dictionary contains a "data" key which is an array of data points
                    if (json_data.ContainsKey("data") && json_data["data"] is List<dynamic> DataPoints)
                    {
                        // Trimming the data to the requested number of entries if necessary
                        json_data["data"] = DataPoints.Take(CPIperiod.Value).ToList();
                    }

                    // Saving the cache
                    SaveCache3(json_data, CPI_name);
                    // Returning the data as a JSON object
                    return new JsonResult(json_data);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // Fourth class to store the cache entry
        public class CacheEntry4
        {
            // Dictionary to store the data
            public Dictionary<string, dynamic>? Data4 { get; set; }
            // Timestamp to store the time when the data was cached
            public DateTime Timestamp4 { get; set; }
        }

        public string GetCache4(string Inflation)
        {
            // Creating the file name
            var fileName = $"cache_{Inflation}.json";
            // Returning the path to the cache file
            return Path.Combine(CacheDirectory, fileName);
        }


        // Function to load the cache from the file
        public Dictionary<string, dynamic>? LoadCache4(string Inflation)
        {
            // Getting the path to the cache file
            string filePath = GetCache4(Inflation);

            // Checking if the file exists
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    // Reading the file
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    // Deserializing the file
                    var cacheEntry4 = JsonSerializer.Deserialize<CacheEntry4>(jsonString);
                    // Checking if the cache is valid
                    if (cacheEntry4 != null && DateTime.UtcNow - cacheEntry4.Timestamp4 < CacheDuration)
                    {
                        // Returning the data
                        return cacheEntry4.Data4 ?? new Dictionary<string, dynamic>();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading or deserializing cache for {Inflation}: {e.Message}");
                }
            }
            // Returning null if the cache is not valid
            return null;
        }




        // Function to save the cache to the file
        public void SaveCache4(Dictionary<string, dynamic> data, string Inflation)
        {
            // Creating the cache entry
            var cacheEntry4 = new CacheEntry4
            {
                // Storing the data
                Data4 = data,
                // Storing the current time
                Timestamp4 = DateTime.UtcNow
            };
            // Getting the path to the cache file
            string filePath = GetCache4(Inflation);

            // Writing the cache to the file in a thread-safe manner
            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry4));
            }
        }

        // Function to get the data from the API
        public IActionResult OnGetInflation(int? InflationPeriod)
        {
            // Setting the value of the variable
            this.InflationPeriod = InflationPeriod.Value;
            // Checking if the number of entries requested is valid
            if (!InflationPeriod.HasValue || InflationPeriod.Value <= 0 || InflationPeriod.Value > 100)
            {
                return BadRequest("Invalid number of entries requested.");
            }

            const string API_KEY = "1F6SLA57L4NZM1DR";
            // Creating the URL to get the data from the API
            string url = $"https://www.alphavantage.co/query?function={Inflation}&apikey={API_KEY}";
            // Creating the URI to handle the URL
            Uri uri = new Uri(url);

            _ = LoadCache4(Inflation);

            try
            {
                // Creating a new WebClient
                using (WebClient client = new WebClient())
                {
                    // Downloading the data from the API
                    string jsonString = client.DownloadString(uri);
                    // Deserializing the file
                    Dictionary<string, dynamic>? json_data = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(jsonString);
                    // Checking if the dictionary contains a "data" key which is an array of data points
                    if (json_data.ContainsKey("data") && json_data["data"] is List<dynamic> DataPoints)
                    {
                        // Trimming the data to the requested number of entries if necessary
                        json_data["data"] = DataPoints.Take(InflationPeriod.Value).ToList();
                    }
                    // Saving the cache
                    SaveCache4(json_data, Inflation);
                    // Returning the data as a JSON object
                    return new JsonResult(json_data);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        // Fifth class to store the cache entry
        public class CacheEntry5
        {
            // Dictionary to store the data
            public Dictionary<string, dynamic>? Data5 { get; set; }
            // Timestamp to store the time when the data was cached
            public DateTime Timestamp5 { get; set; }
        }

        // Function to get the cache file name
        public string GetCache5(string UnemploymentRate)
        {
            // Creating the file name
            var fileName = $"cache_{UnemploymentRate}.json";
            // Returning the path to the cache file
            return Path.Combine(CacheDirectory, fileName);
        }


        // Function to load the cache from the file
        public Dictionary<string, dynamic>? LoadCache5(string UnemploymentRate)
        {
            // Getting the path to the cache file
            string filePath = GetCache5(UnemploymentRate);

            // Checking if the file exists
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    // Reading the file
                    var jsonString = System.IO.File.ReadAllText(filePath);
                    // Deserializing the file
                    var cacheEntry5 = JsonSerializer.Deserialize<CacheEntry5>(jsonString);
                    // Checking if the cache is valid
                    if (cacheEntry5 != null && DateTime.UtcNow - cacheEntry5.Timestamp5 < CacheDuration)
                    {
                        // Returning the data
                        return cacheEntry5.Data5 ?? new Dictionary<string, dynamic>();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading or deserializing cache for {UnemploymentRate}: {e.Message}");
                }
            }
            // Returning null if the cache is not valid
            return null;
        }


        // Function to save the cache to the file
        public void SaveCache5(Dictionary<string, dynamic> data, string UnemploymentRate)
        {
            // Creating the cache entry
            var cacheEntry4 = new CacheEntry5
            {
                // Storing the data
                Data5 = data,
                // Storing the current time
                Timestamp5 = DateTime.UtcNow
            };
            // Getting the path to the cache file
            string filePath = GetCache5(UnemploymentRate);

            // Writing the cache to the file in a thread-safe manner
            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry4));
            }
        }

        // Function to get the data from the API
        public IActionResult OnGetUnemployment(int? UnemploymentRatePeriod)
        {

            this.UnemploymentRatePeriod = UnemploymentRatePeriod.Value;
            // Checking if the number of entries requested is valid
            if (!UnemploymentRatePeriod.HasValue || UnemploymentRatePeriod.Value <= 0 || UnemploymentRatePeriod.Value > 100)
            {
                return BadRequest("Invalid number of entries requested.");
            }

            const string API_KEY = "1F6SLA57L4NZM1DR";
            // Creating the URL to get the data from the API
            string url = $"https://www.alphavantage.co/query?function={UnemploymentRate}&apikey={API_KEY}";
            // Creating the URI to handle the URL
            Uri uri = new Uri(url);
            // Loading the cache
            _ = LoadCache5(UnemploymentRate);

            try
            {
                // Creating a new WebClient
                using (WebClient client = new WebClient())
                {
                    // Downloading the data from the API
                    string jsonString = client.DownloadString(uri);
                    Dictionary<string, dynamic>? json_data = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(jsonString);
                    // Checking if the dictionary contains a "data" key which is an array of data points
                    if (json_data.ContainsKey("data") && json_data["data"] is List<dynamic> DataPoints)
                    {
                        // Trimming the data to the requested number of entries if necessary
                        json_data["data"] = DataPoints.Take(UnemploymentRatePeriod.Value).ToList();
                    }
                    // Saving the cache
                    SaveCache5(json_data, UnemploymentRate);
                    // Return the data as a JSON object
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
