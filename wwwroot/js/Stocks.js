// Add an event listener for the form submission.
document.getElementById("StockForm").addEventListener("submit", function (event) {
    // Prevent the default form submission behavior.
    event.preventDefault();

    // Get the input values from the form.
    const stockName = document.getElementById("stockName").value;
    const interval = document.getElementById("interval").value;
    const periods = parseInt(document.getElementById("periods").value);

    // Validate the period input.
    if (isNaN(periods) || periods < 1 || periods > 100) {
        alert("Please enter a valid number of periods.");
        return;
    }

    // Prepare the query parameters for the fetch request.
    const query = new URLSearchParams({
        stockName: stockName,
        interval: interval,
        periods: periods
    });

    // Make a fetch request to the server with the query parameters.
    fetch("/Stocks?" + query.toString())
        .then(response => {
            if (!response.ok) {
                // If there's an error, reject the promise.
                return response.text().then(text => {
                    throw new Error(text);
                });
            }
            // Parse the JSON response.
            return response.json();
        })
        .then(data => {
            // Extract the time series data based on the selected interval.
            const timeSeries = getTimeSeries(data, interval);
            // Compute SMA and EMA values.
            const SMA = computeSMA(timeSeries, periods);
            const EMA = computeEMA(timeSeries, periods);
            // Display the stock information.
            displayStock(data, interval, periods, SMA, EMA);
        })
        .catch(error => {
            // Handle any errors that occurred during the fetch.
            console.error("Error:", error);
        });
});

// Get the time series data based on the selected interval.
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

// Compute the Simple Moving Average (SMA) for the given period.
function computeSMA(timeSeries, periods) {
    // If the timeSeries data is not available, return null.
    if (!timeSeries) return null;
    // Extract closing prices and calculate the average.
    const closingPrices = Object.values(timeSeries).slice(0, periods).map(day => parseFloat(day["4. close"]));
    const sum = closingPrices.reduce((acc, price) => acc + price, 0);
    return sum / periods;
}

// Compute the Exponential Moving Average (EMA) for the given period.
function computeEMA(timeSeries, periods) {
    // If the timeSeries data is not available, return null.
    if (!timeSeries) return null;

    // Extract and map the closing prices from the time series data.
    const closingPrices = Object.values(timeSeries).map(day => parseFloat(day["4. close"]));

    // Calculate the initial Simple Moving Average (SMA) for the given periods.
    // This will serve as our starting point for the EMA calculation.
    const initialSMA = closingPrices.slice(0, periods).reduce((acc, price) => acc + price, 0) / periods;

    // Initialize the EMA array with the initial SMA value.
    let EMA = [initialSMA];

    // Calculate the multiplier using the formula: 2 / (periods + 1).
    // This factor decides the weightage of recent prices vs previous EMA in the EMA formula.
    const multiplier = 2 / (periods + 1);

    // Starting from the next day after initial SMA, calculate the EMA.
    // EMA formula: (Close - Previous EMA) * multiplier + Previous EMA.
    for (let i = periods; i < closingPrices.length; i++) {
        const newEMA = (closingPrices[i] * multiplier) + (EMA[EMA.length - 1] * (1 - multiplier));
        EMA.push(newEMA);
    }

    // Return the calculated EMA values.
    return EMA;
}


// Display stock data, SMA, and EMA on the web page.
function displayStock(data, interval, periods, SMA, EMA) {
    const output = document.getElementById("StockResult");

    let timeSeriesDate;

    // Determine the time series data based on the selected interval.
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

    // Extract the time series data for the selected interval.
    const timeSeries = data[timeSeriesDate];

    if (!timeSeries) {
        alert("No data available for the selected interval.");
        return;
    }

    // Extract the first N dates from the time series data.
    const dates = Object.keys(timeSeries).slice(0, periods);

    // Update the output with the SMA and EMA values.
    output.innerHTML = ` 
    <div>
        <p><strong>SMA:</strong> ${SMA.toFixed(2)}</p>
        <p><strong>EMA:</strong> ${EMA[EMA.length - 1].toFixed(2)}</p>
    </div>
    `;

    // Display each stock data entry in a card format.
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
