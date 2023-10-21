document.getElementById("StockForm").addEventListener("submit", function (event) {
    event.preventDefault();

    const stockName = document.getElementById("stockName").value;
    const interval = document.getElementById("interval").value;
    const periods = parseInt(document.getElementById("periods").value);

    if (isNaN(periods) || periods < 1 || periods > 100) {
        alert("Please enter a valid number of periods.");
        return;
    }

    const stockForm = new FormData();
    stockForm.append("stockName", stockName);
    stockForm.append("interval", interval);
    stockForm.append("periods", periods);

    fetch("/Stock", {
        method: "POST",
        body: stockForm
    })
        .then(response => {
            if (!response.ok) {
                throw new Error(response.statusText);
            }
            return response.json();
        })
        .then(data => {
            displayStock(data, interval);
        })
        .catch(error => {
            console.error("Error:", error);
        });
});

function displayStock(data, interval) {
    const output = document.getElementById("StockResult");
    output.innerHTML = "";

    let timeSeriesDate;

    switch (interval) {
        case "daily":
            timeSeriesDate = "Time Series (Daily)";
            break;
        case "weekly":
            timeSeriesDate = "Weekly Time Series";
            break;
        case "monthly":
            timeSeriesDate = "Monthly Time Series";
            break;
        default:
            alert("Please select a valid interval.");
            return;
    }

    const timeSeries = data[timeSeriesDate];

    const dates = Object.keys(timeSeries).slice(0, number);

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
        `
    }
}
