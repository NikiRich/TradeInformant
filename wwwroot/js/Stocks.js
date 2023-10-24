document.getElementById("StockForm").addEventListener("submit", function (event) {
    event.preventDefault();

    const stockName = document.getElementById("stockName").value;
    const interval = document.getElementById("interval").value;
    const periods = parseInt(document.getElementById("periods").value);

    if (isNaN(periods) || periods < 1 || periods > 100) {
        alert("Please enter a valid number of periods.");
        return;
    }

    const query = new URLSearchParams({
        stockName: stockName,
        interval: interval,
        periods: periods
    });

    fetch("/Stocks?" + query.toString())
        .then(response => {
            if (!response.ok) {
                return response.text().then(text => {
                    throw new Error(text);
                });
            }
            return response.json();
        })
        .then(data => {
            displayStock(data, interval, periods);
        })
        .catch(error => {
            console.error("Error:", error);
        });
});



function displayStock(data, interval, periods) {
    console.log(data);
    const output = document.getElementById("StockResult");
    output.innerHTML = "";

    let timeSeriesDate;

    switch (interval) {
        case "Daily":
            timeSeriesDate = "Time Series (Daily)";
            break;
        case "Weekly":
            timeSeriesDate = "Weekly Time Series";
            break;
        case "Monthly":
            timeSeriesDate = "Monthly Time Series";
            break;
        default:
            alert("Please select a valid interval.");
            return;
    }

    const timeSeries = data[timeSeriesDate];

if (!timeSeries) {
    alert("No data available for the selected interval.");
    return;
}

const dates = Object.keys(timeSeries).slice(0, periods);
for (const date of dates) {
    const stockInfo = timeSeries[date];
    output.innerHTML += `
        <div id="StockOutput">
            <p>Date: ${date}</p>
            <p>Open: ${stockInfo["1. open"]}</p>
            <p>High: ${stockInfo["2. high"]}</p>
            <p>Low: ${stockInfo["3. low"]}</p>
            <p>Close: ${stockInfo["4. close"]}</p>
        </div>
    `;
}

}