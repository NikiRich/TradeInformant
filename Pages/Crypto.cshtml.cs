using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

namespace TradeInformant.Pages
{
    public class CryptoModel : PageModel 
    {
        public string stockName { get; set; }
        public string interval { get; set; }
        public string market { get; set; }
        public int periods { get; set; }

        private static readonly string cryptoFile = "cryptoCache.json";


        public IActionResult OnGet(string? stockName, string? interval, string? market, int? periods)
        {
            if (stockName == null || interval == null || market == null || periods == null)
            {
                return Page();
            }

            // Assign the properties
            this.stockName = stockName;
            this.interval = interval;
            this.market = market;
            this.periods = (int)periods;

            const string API_KEY = "1F6SLA57L4NZM1DR";

            string function;

            switch (interval)
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
                    return new BadRequestObjectResult($"Invalid interval: {interval}");
            }

            string url = $"https://www.alphavantage.co/query?function={function}&symbol={stockName}&market={market}&apikey={API_KEY}";
            Uri uri = new Uri(url);

            try
            {
                using (WebClient client = new WebClient())
                {
                    var jsonInfo = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(client.DownloadString(uri));

                    if (jsonInfo == null)
                    {
                        Console.WriteLine($"Interval: {interval}, CryptoName: {stockName}, Market: {market}");
                        return new BadRequestObjectResult("Error retrieving data for the particular cryptocurrency or invalid data format");
                    }
                    return new JsonResult(jsonInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching cryptocurrency data for {stockName} with interval {interval} and market {market}: {ex.Message}");
                return new BadRequestObjectResult("Error fetching cryptocurrency data. Please try again later.");
            }
        }
    }
}
