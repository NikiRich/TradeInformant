using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

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
            // Creating the file name
            var fileName = $"cache_{StockName}_{Interval}.json";
            // Returning the path to the cache file
            return Path.Combine(CacheDirectory, fileName);
        }


        // Function to load the cache from the file
        public Dictionary<string, dynamic>? LoadCacheFromFile(string StockName, string Interval)
        {
            // Getting the path to the cache file
            string filePath = GetCacheFileName(StockName, Interval);

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
                    Console.WriteLine($"Error reading or deserializing cache for {StockName} - {Interval}: {e.Message}");
                }
            }
            // Returning null if the cache is not valid
            return null;
        }


        // Function to save the cache to the file
        public void SaveCache(Dictionary<string, dynamic> data, string StockName, string Interval)
        {
            // Creating the cache entry
            var cacheEntry = new CacheEntry
            {
                // Storing the data
                Data = data,
                // Storing the current time
                Timestamp = DateTime.UtcNow
            };
            // Getting the path to the cache file
            string filePath = GetCacheFileName(StockName, Interval);

            // Writing the cache to the file in a thread-safe manner
            lock (filePath)
            {
                System.IO.File.WriteAllText(filePath, JsonSerializer.Serialize(cacheEntry));
            }
        }


        // Function to get the stock data
        public IActionResult OnGet(string? StockName, string? Interval, int? Periods)
        {
            // Checking if the parameters are null
            if (StockName == null || Interval == null || Periods == null)
            {
                // Returning the page
                return Page();
            }

            // Storing the parameters
            this.StockName = StockName;
            this.Interval = Interval;
            this.Periods = (int)Periods;

            // API key for Alpha Vantage
            const string API_KEY = "1F6SLA57L4NZM1DR";

            string function;

            // Checking the interval
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
                    // Returning an error if the interval is invalid
                    return new BadRequestObjectResult($"Invalid Interval: {Interval}");
            }

            // Creating the URL to fetch the data
            string url = $"https://www.alphavantage.co/query?function={function}&symbol={StockName}&apikey={API_KEY}";
            // Creating the URI to have the URL in a proper format
            Uri uri = new Uri(url);

            // Loading the cache from the file
            Dictionary<string, dynamic>? jsonInfo = LoadCacheFromFile(StockName, Interval);

            // Checking if the cache is null
            if (jsonInfo == null)
            {
                try
                {
                    // Fetching the data from the API
                    using (WebClient client = new())
                    {
                        // Downloading the data
                        jsonInfo = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(client.DownloadString(uri));

                        // Checking if the data is null
                        if (jsonInfo == null)
                        {
                            Console.WriteLine($"Interval: {Interval}, StockName: {StockName}");
                            // Returning an error if the data is null
                            return new BadRequestObjectResult("Error retrieving data for the particular stock or invalid data format");
                        }

                        // Saving the data to the cache
                        SaveCache(jsonInfo, StockName, Interval);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching stock data for {StockName} with Interval {Interval}: {ex.Message}");
                    // Returning an error if the data cannot be fetched
                    return new BadRequestObjectResult("Error fetching stock data. Please try aGain later.");
                }
            }
            // Returning the data
            return new JsonResult(jsonInfo);
        }


        public class Indicators
        {
            public decimal SMA { get; set; }
            public decimal EMA { get; set; }
            public decimal RSI { get; set; }
            public decimal MACD { get; set; }
            public decimal signalLine { get; set; }
            public decimal histogram { get; set; }
        }

        // Method to save the TrainModeled CART model to a file
        private void SaveModel(CART cart)
        {
            try
            {
                // Path to the model file
                var modelPath = Path.Combine(_env.ContentRootPath, "Model", "cart_model.json");

                // Serializing the model to JSON
                var options = new JsonSerializerOptions { WriteIndented = true };
                var modelJson = JsonSerializer.Serialize(cart, options);

                // Ensuring the directory exists
                var directory = Path.GetDirectoryName(modelPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Writing the JSON to the file, overwriting any existing file
                System.IO.File.WriteAllText(modelPath, modelJson);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save model to file.", ex);
            }
        }

        // Method to load the TrainModeled CART model from a file
        private CART LoadModel()
        {
            try
            {
                // Path to the model file
                var modelPath = Path.Combine(_env.ContentRootPath, "Model", "cart_model.json");

                // Checking if the model file exists
                if (System.IO.File.Exists(modelPath))
                {
                    // Reading the JSON from the file
                    var modelJson = System.IO.File.ReadAllText(modelPath);

                    // Deserializing the JSON to a CART object
                    return JsonSerializer.Deserialize<CART>(modelJson);
                }
                else
                {
                    // If the file does not exist, return null indicating no model is loaded
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load model from file.", ex);
            }
        }

        // This method is for TrainModeling the model with provided TrainModeling data.
        public IActionResult OnGetTrainModelModel([FromQuery] TrainModelingData TrainModelingData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Converting the TrainModeling data into the expected format for the CART algorithm
            List<Dictionary<string, decimal>> Features = TrainModelingData.Features;
            List<string> Labels = TrainModelingData.Labels;

            // Creating an instance of the CART algorithm and TrainModel it
            var cart = new CART();
            cart.TrainModel(Features, Labels);

            // Saving the TrainModeled model to a file for future predictions
            SaveModel(cart);
            // Returning a success message
            return new JsonResult(new { Message = "Model TrainModeled successfully" });
        }
        // This method is for making predictions with the TrainModeled model.
        public IActionResult OnGetPredictionCalculation([FromQuery] Indicators indicators)
        {
            // Checking if the model is trained
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Loading the TrainModeled model from a file
            var cart = LoadModel();

            // Ensuring the model is loaded
            if (cart == null)
            {
                return new JsonResult(new { Error = "The model could not load" });
            }

            // Converting the indicators to the expected format
            var Input = new Dictionary<string, decimal>
            {
                { "SMA", indicators.SMA },
                { "EMA", indicators.EMA },
                { "RSI", indicators.RSI },
                { "MACD", indicators.MACD },
                { "signalLine", indicators.signalLine },
                { "histogram", indicators.histogram }
            };

            // Making a prediction
            var prediction = cart.Predict(Input);

            // Returning the prediction result
            return new JsonResult(new { prediction });
        }



        // This class is for storing the TrainModeling data
        public class TrainModelingData
        {
            // The TrainModeling data is stored as a list of Features and a list of Labels
            public List<Dictionary<string, decimal>> Features { get; set; }
            public List<string> Labels { get; set; }
        }


        // Decision tree node class that stores the information about a node in the decision tree
        public class DecisionTreeNode
        {
            public bool IsLeaf { get; set; }
            public string FeatureToSplit { get; set; }
            public decimal SplitValue { get; set; }
            public string Prediction { get; set; }
            public DecisionTreeNode LeftChild { get; set; }
            public DecisionTreeNode RightChild { get; set; }

            // Method to make predictions with the decision tree
            public string Predict(Dictionary<string, decimal> Features)
            {
                if (this.IsLeaf)
                {
                    // If the node is a leaf, return the prediction
                    return this.Prediction;
                }
                else
                {
                    // If the feature value is less than or equal to the split value, traverse the left subtree
                    if (Features[this.FeatureToSplit] <= this.SplitValue)
                    {
                        // Traversing the left subtree
                        return this.LeftChild.Predict(Features);
                    }
                    else
                    {
                        // Traversing the right subtree
                        return this.RightChild.Predict(Features);
                    }
                }
            }
        }

        // Implementation of the CART algorithm
        public class CART
        {
            // The root node of the decision tree
            public DecisionTreeNode Root { get; set; }

            // Constructor
            public CART()
            {
                Root = null;
            }

            // Traing the model with the provided TrainModeling data
            public void TrainModel(List<Dictionary<string, decimal>> Features, List<string> Labels)
            {
                // Building the decision tree
                Root = BuildTree(Features, Labels);
                if (Root == null)
                {
                    // Logging error or throw an exception if the root is still null after TrainModeling
                    throw new InvalidOperationException("Failed to build the decision tree.");
                }

            }

            // Method to build the decision tree recursively
            private DecisionTreeNode BuildTree(List<Dictionary<string, decimal>> Features, List<string> Labels)
            {
                // Checking for stopping conditions
                if (StopSplit(Features, Labels))
                {
                    // Returning a leaf node with the most common label
                    return new DecisionTreeNode
                    {
                        // Setting the IsLeaf property to true
                        IsLeaf = true,
                        // Finding the most common label
                        Prediction = Labels.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key
                    };
                }

                // Finding the best Feature and Value to split on
                var (BestFeature, BestValue) = FindBestSplit(Features, Labels);

                // Partitioning the data into two subsets based on the best split
                var (LeftFeatures, LeftLabels, RightFeatures, RightLabels) = PartitionData(Features, Labels, BestFeature, BestValue);

                // Recursively build the tree
                var LeftChild = BuildTree(LeftFeatures, LeftLabels);
                var RightChild = BuildTree(RightFeatures, RightLabels);

                // Returning the current node
                return new DecisionTreeNode
                {
                    FeatureToSplit = BestFeature,
                    SplitValue = (decimal)BestValue,
                    LeftChild = LeftChild,
                    RightChild = RightChild
                };
            }

            // Method to check for stopping conditions
            private bool StopSplit(List<Dictionary<string, decimal>> Features, List<string> Labels)
            {
                // Stopping if all Labels are the same
                if (Labels.Distinct().Count() == 1)
                {
                    return true;
                }

                // Stopping if there are no Features left to split on
                if (Features.Count == 0 || Features.All(f => f.Count == 0))
                {
                    return true;
                }

                // Additional stopping conditions like maximum tree depth or minimum node size can be added here

                // If none of the stopping conditions are met, do not stop splitting
                return false;
            }

            // Splitting the data into two subsets based on a split
            private (List<Dictionary<string, decimal>> LeftFeatures, List<string> LeftLabels,
            List<Dictionary<string, decimal>> RightFeatures, List<string> RightLabels)
            // Method to partition the data into two subsets based on a split
            PartitionData(List<Dictionary<string, decimal>> Features, List<string> Labels, string Feature, decimal Value)
            {
                var LeftFeatures = new List<Dictionary<string, decimal>>();
                var LeftLabels = new List<string>();
                var RightFeatures = new List<Dictionary<string, decimal>>();
                var RightLabels = new List<string>();

                for (int i = 0; i < Features.Count; i++)
                {
                    if (Features[i][Feature] <= Value)
                    {
                        LeftFeatures.Add(Features[i]);
                        LeftLabels.Add(Labels[i]);
                    }
                    else
                    {
                        RightFeatures.Add(Features[i]);
                        RightLabels.Add(Labels[i]);
                    }
                }

                return (LeftFeatures, LeftLabels, RightFeatures, RightLabels);
            }

            // Method to calculate the information gain
            private decimal InformationGain(List<Dictionary<string, decimal>> Features, List<string> Labels, string Feature, decimal Value)
            {
                // Splitting the data
                var (LeftFeatures, LeftLabels, RightFeatures, RightLabels) = PartitionData(Features, Labels, Feature, Value);

                // Calculating the entropy before the split
                decimal originalEntropy = Entropy(Labels);

                // Calculating the entropy after the split
                decimal leftEntropy = Entropy(LeftLabels);
                decimal rightEntropy = Entropy(RightLabels);

                // Calculating the weighted average of the entropy after the split
                decimal weightedEntropy = ((decimal)LeftLabels.Count / Labels.Count) * leftEntropy
                                         + ((decimal)RightLabels.Count / Labels.Count) * rightEntropy;

                // Information Gain is the entropy reduction by splitting the data
                decimal informationGain = originalEntropy - weightedEntropy;

                return informationGain;
            }

            // Method to find the best Feature and Value to split on
            private (string BestFeature, decimal BestValue) FindBestSplit(List<Dictionary<string, decimal>> Features, List<string> Labels)
            {
                decimal BestGain = decimal.MinValue;
                string BestFeature = null;
                decimal? BestValue = null;


                // Iterating over every Feature
                foreach (var Feature in Features.SelectMany(f => f.Keys).Distinct())
                {
                    // Getting unique Values for the Feature
                    var FeatureValues = Features.Select(f => f[Feature]).Distinct().OrderBy(x => x);

                    // Testing all possible split Values
                    foreach (var Value in FeatureValues)
                    {
                        var Gain = InformationGain(Features, Labels, Feature, Value);
                        if (Gain > BestGain)
                        {
                            BestGain = Gain;
                            BestFeature = Feature;
                            BestValue = Value;
                        }
                    }
                }

                return (BestFeature, BestValue ?? default);
            }

            // Method to calculate the entropy which is the measure of impurity in a set of Labels
            private decimal Entropy(List<string> Labels)
            {
                // Grouping the Labels and count occurrences
                var labelCounts = Labels.GroupBy(l => l).ToDictionary(g => g.Key, g => g.Count());

                // Calculating the entropy
                decimal entropy = 0;
                foreach (var labelCount in labelCounts)
                {
                    double probability = (double)labelCount.Value / Labels.Count;
                    entropy -= (decimal)(probability * Math.Log(probability, 2));
                }

                return entropy;
            }

            // Predicting the label for a single data point
            public string Predict(Dictionary<string, decimal> Input)
            {
                if (Root == null)
                {
                    throw new InvalidOperationException("The model has not been TrainModeled.");
                }
                return PredictFromNode(Root, Input);
            }

            // Predicting the label for a single data point from a given node
            private string PredictFromNode(DecisionTreeNode node, Dictionary<string, decimal> Features)
            {
                // Base case: if the node is a leaf, return the prediction
                if (node.IsLeaf)
                {
                    return node.Prediction;
                }

                // Recursive case: traverse the tree based on the Feature's Value
                if (Features[node.FeatureToSplit] <= node.SplitValue)
                {
                    return PredictFromNode(node.LeftChild, Features);
                }
                else
                {
                    return PredictFromNode(node.RightChild, Features);
                }
            }
        }
    }
}

