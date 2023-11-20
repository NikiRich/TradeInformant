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

        // DTO for indicators
        public class Indicators
        {
            public decimal SMA { get; set; }
            public decimal EMA { get; set; }
            public decimal RSI { get; set; }
            public decimal MACD { get; set; }
            public decimal signalLine { get; set; }
            public decimal histogram { get; set; }
        }
        // Method to save the trained CART model to a file
        private void SaveModelToFile(CART cart)
        {
            // Path to the model file
            var modelPath = Path.Combine(_env.ContentRootPath, "Model", "cart_model.json");

            // Serialize the model to JSON
            var modelJson = JsonSerializer.Serialize(cart);

            // Write the JSON to the file, overwriting any existing file
            System.IO.File.WriteAllText(modelPath, modelJson);
        }

        // Method to load the trained CART model from a file
        private CART LoadModelFromFile()
        {
            // Path to the model file
            var modelPath = Path.Combine(_env.ContentRootPath, "Model", "cart_model.json");

            // Check if the model file exists
            if (System.IO.File.Exists(modelPath))
            {
                // Read the JSON from the file
                var modelJson = System.IO.File.ReadAllText(modelPath);

                // Deserialize the JSON to a CART object
                return JsonSerializer.Deserialize<CART>(modelJson);
            }

            // If the file does not exist, return null indicating no model is loaded
            return null;
        }

        // This method is for training the model with provided training data.
        public IActionResult OnGetTrainModel([FromQuery] TrainingData trainingData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Convert the training data into the expected format for the CART algorithm
            List<Dictionary<string, double>> features = trainingData.Features;
            List<string> labels = trainingData.Labels;

            // Create an instance of the CART algorithm and train it
            var cart = new CART();
            cart.Train(features, labels);

            // Save the trained model to a file for future predictions
            SaveModelToFile(cart);

            return new JsonResult(new { Message = "Model trained successfully" });
        }

        public IActionResult OnGetPredictionCalculation([FromQuery] Indicators indicators)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Load the trained model from a file
            var cart = LoadModelFromFile();

            // Ensure your model is trained before making predictions
            if (cart == null)
            {
                return new JsonResult(new { Error = "The model is not trained or the model file is not found." });
            }

            // Convert the indicators to the expected format
            var inputFeatures = new Dictionary<string, decimal>
            {
                { "SMA", indicators.SMA },
                { "EMA", indicators.EMA },
                { "RSI", indicators.RSI },
                { "MACD", indicators.MACD },
                { "signalLine", indicators.signalLine },
                { "histogram", indicators.histogram }
            };

            // Make a prediction
            var prediction = cart.Predict(inputFeatures);

            // Return the prediction result
            return new JsonResult(new { Prediction = prediction });
        }



        // DTO for training data
        public class TrainingData
        {
            public List<Dictionary<string, double>> Features { get; set; }
            public List<string> Labels { get; set; }
        }


        // Implementation of the CART algorithm
        public class CART
        {
            private DecisionTreeNode _root;

            public CART()
            {
                _root = null;
            }

            public void Train(List<Dictionary<string, double>> features, List<string> labels)
            {
                _root = BuildTree(features, labels);
            }

            private DecisionTreeNode BuildTree(List<Dictionary<string, double>> features, List<string> labels)
            {
                // Check for stopping conditions
                if (ShouldStopSplitting(features, labels))
                {
                    return new DecisionTreeNode
                    {
                        IsLeaf = true,
                        Prediction = labels.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key
                    };
                }

                // Find the best feature and value to split on
                var (bestFeature, bestValue) = FindBestSplit(features, labels);

                // Partition the data into two subsets based on the best split
                var (leftFeatures, leftLabels, rightFeatures, rightLabels) = PartitionData(features, labels, bestFeature, bestValue);

                // Recursively build the tree
                var leftChild = BuildTree(leftFeatures, leftLabels);
                var rightChild = BuildTree(rightFeatures, rightLabels);

                // Return the current node
                return new DecisionTreeNode
                {
                    FeatureToSplit = bestFeature,
                    SplitValue = (decimal)bestValue,
                    LeftChild = leftChild,
                    RightChild = rightChild
                };
            }
            private bool ShouldStopSplitting(List<Dictionary<string, double>> features, List<string> labels)
            {
                // Stop if all labels are the same
                if (labels.Distinct().Count() == 1)
                {
                    return true;
                }

                // Stop if there are no features left to split on
                if (features.Count == 0 || features.All(f => f.Count == 0))
                {
                    return true;
                }

                // Additional stopping conditions like maximum tree depth or minimum node size can be added here

                // If none of the stopping conditions are met, do not stop splitting
                return false;
            }

            private (string bestFeature, double bestValue) FindBestSplit(List<Dictionary<string, double>> features, List<string> labels)
            {
                double bestGain = double.MinValue;
                string bestFeature = null;
                double bestValue = double.NaN;

                // Iterate over every feature
                foreach (var feature in features.SelectMany(f => f.Keys).Distinct())
                {
                    // Get unique values for the feature
                    var featureValues = features.Select(f => f[feature]).Distinct().OrderBy(x => x);

                    // Test all possible split values
                    foreach (var value in featureValues)
                    {
                        var gain = CalculateInformationGain(features, labels, feature, value);
                        if (gain > bestGain)
                        {
                            bestGain = gain;
                            bestFeature = feature;
                            bestValue = value;
                        }
                    }
                }

                return (bestFeature, bestValue);
            }

            private (List<Dictionary<string, double>> leftFeatures, List<string> leftLabels,
         List<Dictionary<string, double>> rightFeatures, List<string> rightLabels)
    PartitionData(List<Dictionary<string, double>> features, List<string> labels, string feature, double value)
            {
                var leftFeatures = new List<Dictionary<string, double>>();
                var leftLabels = new List<string>();
                var rightFeatures = new List<Dictionary<string, double>>();
                var rightLabels = new List<string>();

                for (int i = 0; i < features.Count; i++)
                {
                    if (features[i][feature] <= value)
                    {
                        leftFeatures.Add(features[i]);
                        leftLabels.Add(labels[i]);
                    }
                    else
                    {
                        rightFeatures.Add(features[i]);
                        rightLabels.Add(labels[i]);
                    }
                }

                return (leftFeatures, leftLabels, rightFeatures, rightLabels);
            }
            private double CalculateInformationGain(List<Dictionary<string, double>> features, List<string> labels, string feature, double value)
            {
                // Split the data
                var (leftFeatures, leftLabels, rightFeatures, rightLabels) = PartitionData(features, labels, feature, value);

                // Calculate the entropy before the split
                double originalEntropy = Entropy(labels);

                // Calculate the entropy after the split
                double leftEntropy = Entropy(leftLabels);
                double rightEntropy = Entropy(rightLabels);

                // Calculate the weighted average of the entropy after the split
                double weightedEntropy = ((double)leftLabels.Count / labels.Count) * leftEntropy
                                         + ((double)rightLabels.Count / labels.Count) * rightEntropy;

                // Information gain is the entropy reduction by splitting the data
                double informationGain = originalEntropy - weightedEntropy;

                return informationGain;
            }

            private double Entropy(List<string> labels)
            {
                // Group the labels and count occurrences
                var labelCounts = labels.GroupBy(l => l).ToDictionary(g => g.Key, g => g.Count());

                // Calculate the entropy
                double entropy = 0.0;
                foreach (var labelCount in labelCounts)
                {
                    double probability = (double)labelCount.Value / labels.Count;
                    entropy -= probability * Math.Log(probability, 2);
                }

                return entropy;
            }

            public string Predict(Dictionary<string, decimal> inputFeatures)
            {
                if (_root == null)
                {
                    throw new InvalidOperationException("The model has not been trained.");
                }
                return PredictFromNode(_root, inputFeatures);
            }

            private string PredictFromNode(DecisionTreeNode node, Dictionary<string, decimal> features)
            {
                // Base case: if the node is a leaf, return the prediction
                if (node.IsLeaf)
                {
                    return node.Prediction;
                }

                // Recursive case: traverse the tree based on the feature's value
                if (features[node.FeatureToSplit] <= node.SplitValue)
                {
                    return PredictFromNode(node.LeftChild, features);
                }
                else
                {
                    return PredictFromNode(node.RightChild, features);
                }
            }
        }


        // Decision tree node class
        public class DecisionTreeNode
        {
            public bool IsLeaf { get; set; }
            public string FeatureToSplit { get; set; }
            public decimal SplitValue { get; set; }
            public string Prediction { get; set; }
            public DecisionTreeNode LeftChild { get; set; }
            public DecisionTreeNode RightChild { get; set; }

            public string Predict(Dictionary<string, decimal> features)
            {
                if (this.IsLeaf)
                {
                    return this.Prediction;
                }
                else
                {      
                    if (features[this.FeatureToSplit] <= this.SplitValue)
                    {
                        return this.LeftChild.Predict(features);
                    }
                    else
                    {
                        return this.RightChild.Predict(features);
                    }
                }
            }
        }
        
    }
}

