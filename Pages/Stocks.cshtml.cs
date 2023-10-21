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
        [BindProperty]
        public string stockName { get; set; }

        [BindProperty]
        public string interval { get; set; }

        [BindProperty]
        public int periods { get; set; }

        public IActionResult OnPost()
        {
            const string API_KEY = "9UIS088TUP5KK1QB";

            string function;

            switch(interval)
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
                    return new BadRequestObjectResult("Invalid interval");
            }

            string url = $"https://www.alphavantage.co/query?function={function}&symbol={stockName}&apikey={API_KEY}";
            Uri uri = new Uri(url);

            Dictionary<string, dynamic> jsonInfo;

            using (WebClient client = new WebClient())
            {
                jsonInfo = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(client.DownloadString(uri));
            }

            if (jsonInfo == null || !jsonInfo.ContainsKey("Meta Data"))
            {
                return new BadRequestObjectResult("Error retrieving data for the particular stock or invalid data format");
            }
            return new JsonResult(jsonInfo);
        }

    }
}
