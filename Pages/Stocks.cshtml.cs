using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

namespace TradeInformant.Pages
{
    public class StocksModel : PageModel
    {
        public string stockName { get; set; }
        public string interval { get; set; }
        public int periods { get; set; }

        public IActionResult OnGet(string? stockName, string? interval, int? periods)
        {
            if (stockName == null || interval == null || periods == null)
            {
                return Page();
            }
            // Assign the properties
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

            try
            {
                using (WebClient client = new WebClient())
                {
                    var jsonInfo = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(client.DownloadString(uri));

                    if (jsonInfo == null)
                    {
                        Console.WriteLine($"Interval: {interval}, StockName: {stockName}");
                        return new BadRequestObjectResult("Error retrieving data for the particular stock or invalid data format");
                    }
                    return new JsonResult(jsonInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching stock data for {stockName} with interval {interval}: {ex.Message}");
                return new BadRequestObjectResult("Error fetching stock data. Please try again later.");
            }
        }
    }
}
