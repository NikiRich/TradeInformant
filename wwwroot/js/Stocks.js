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
            const timeSeries = getTimeSeries(data, interval);
            const SMA = computeSMA(timeSeries, periods);
            const EMA = computeEMA(timeSeries, periods);
            displayStock(data, interval, periods, SMA, EMA);
        })
        .catch(error => {
            console.error("Error:", error);
        });
});

function getTimeSeries(data, interval) {
    switch (interval) {
        case "Daily":
            return data["Time Series (Daily)"];
        case "Weekly":
            return data["Weekly Time Series"];
        case "Monthly":
            return data["Monthly Time Series"];
        default:
            alert("Please select a valid interval.");
            return;
    }
}

function computeSMA(timeSeries, periods) {
    if (!timeSeries) return null;
    const closingPrices = Object.values(timeSeries).slice(0, periods).map(day => parseFloat(day["4. close"]));
    const sum = closingPrices.reduce((acc, price) => acc + price, 0);
    return sum / periods;
}

function computeEMA(timeSeries, periods) {
    if (!timeSeries) return null;

    // Get the entire series of closing prices
    const closingPrices = Object.values(timeSeries).map(day => parseFloat(day["4. close"]));

    // Initial SMA for the starting point of EMA
    const initialSMA = closingPrices.slice(0, periods).reduce((acc, price) => acc + price, 0) / periods;
    let EMA = [initialSMA];

    const multiplier = 2 / (periods + 1);

    // Start computing EMA from the next day after initial SMA
    for (let i = periods; i < closingPrices.length; i++) {
        const newEMA = (closingPrices[i] * multiplier) + (EMA[EMA.length - 1] * (1 - multiplier));
        EMA.push(newEMA);
    }

    return EMA;
}




function displayStock(data, interval, periods, SMA, EMA) {
    const output = document.getElementById("StockResult");

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


    output.innerHTML = ` 
    <div>
        <p><strong>SMA:</strong> ${SMA.toFixed(2)}</p>
        <p><strong>EMA:</strong> ${EMA[EMA.length -1].toFixed(2)}</p>
    </div>
    `;

    for (const date of dates) {
        const stockInfo = timeSeries[date];
        output.innerHTML += `
        <div class="col-md-3">
            <div class="card mb-3">
                <div class="card-header">
                    Date: ${date}
                </div>
                <div class="card-body">
                    <p><strong>Open:</strong> ${stockInfo["1. open"]}</p>
                    <p><strong>High:</strong> ${stockInfo["2. high"]}</p>
                    <p><strong>Low:</strong> ${stockInfo["3. low"]}</p>
                    <p><strong>Close:</strong> ${stockInfo["4. close"]}</p>
                </div>
            </div>
        </div>
        `;
    }
}
