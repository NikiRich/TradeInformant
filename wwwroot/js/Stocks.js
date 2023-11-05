//An event listener for the form submission.
document.getElementById("StockForm").addEventListener("submit", function (event) {
    // Prevent the default form submission behavior.
    event.preventDefault();

    // Get the input values from the form.
    const StockName = document.getElementById("StockName").value;
    const Interval = document.getElementById("Interval").value;
    const Periods = parseInt(document.getElementById("Periods").value);

    // Validate the period input.
    if (isNaN(Periods) || Periods < 1 || Periods > 100) {
        alert("Please enter a valid number of Periods.");
        return;
    }

    // Prepare the query parameters for the fetch request.
    const query = new URLSearchParams({
        StockName: StockName,
        Interval: Interval,
        Periods: Periods
    });

    // Make a fetch request to the server with the query parameters.
    fetch("/Stocks?" + query.toString())
        // Return the response as JSON if the request was successful.
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
        // If the response was successful, display the stock information.
        .then(data => {
            // Extract the time series data based on the selected Interval.
            const timeSeries = getTimeSeries(data, Interval);

            // Compute SMA, EMA, RSI, and MACD.
            const SMA = computeSMA(timeSeries, Periods);
            const EMA = computeEMA(timeSeries, Periods);
            const RSI = computeRSI(timeSeries, Periods);
            const MACD = computeMACD(timeSeries);
            // Display the stock information.
            displayStock(data, Interval, Periods, SMA, EMA, RSI, MACD);
        })
        .catch(error => {
            // Handle any errors that occurred during the fetch.
            console.error("Error:", error);
        });
});


// Get the time series data based on the selected Interval.
function getTimeSeries(data, Interval) {
    switch (Interval) {
        case "Daily":
            // For daily data, use the "Time Series (Daily)" key.
            return data["Time Series (Daily)"];
        case "Weekly":
            // For weekly data, use the "Weekly Time Series" key.
            return data["Weekly Time Series"];
        case "Monthly":
            // For monthly data, use the "Monthly Time Series" key.
            return data["Monthly Time Series"];
        default:
            alert("Please select a valid Interval.");
            return;
    }
}


// Compute the Simple Moving Average (SMA) for the given period.
function computeSMA(timeSeries, Periods) {
    // If the timeSeries data is not available, return null.
    if (!timeSeries) return null;
    // Extract closing prices and calculate the average.
    const closingPrices = Object.values(timeSeries).slice(0, Periods).map(day => parseFloat(day["4. close"]));
    const sum = closingPrices.reduce((acc, price) => acc + price, 0);
    return sum / Periods;
}


// Compute the Exponential Moving Average (EMA) for the given period.
function computeEMA(input, Periods) {
    let closingPrices;

    // Check if input is an array, then use it directly as the closing prices.
    if (Array.isArray(input)) {
        closingPrices = input;
    } else if (input && typeof input === 'object') {
        // If it's an object, extract the closing prices using the '4. close' key.
        closingPrices = Object.values(input).map(day => parseFloat(day["4. close"]));
    } else {
        // If the input is neither an array nor a valid object, return null.
        return null;
    }

    // Calculate the initial Simple Moving Average (SMA) for the given Periods.
    const initialSMA = closingPrices.slice(0, Periods).reduce((acc, price) => acc + price, 0) / Periods;

    // Initialize the EMA array with the initial SMA value.
    let EMA = [initialSMA];

    // Calculate the multiplier for the EMA calculation.
    const multiplier = 2 / (Periods + 1);

    // Calculate the EMA for each subsequent closing price.
    for (let i = Periods; i < closingPrices.length; i++) {
        const newEMA = (closingPrices[i] - EMA[EMA.length - 1]) * multiplier + EMA[EMA.length - 1];
        EMA.push(newEMA);
    }

    // Return the EMA array.
    return EMA;
}



function computeRSI(timeSeries, Periods) {
    let gains = [];
    let losses = [];

    const closingPrices = Object.values(timeSeries).map(day => parseFloat(day["4. close"]));

    // Calculate the gains and losses for each day.
    for (let i = 1; i < closingPrices.length; i++) {
        let change = closingPrices[i] - closingPrices[i - 1];
        gains.push(Math.max(change, 0)); // If change is negative, push 0
        losses.push(Math.max(-change, 0)); // If change is positive or 0, push 0
    }

    // Calculate the average gain and average loss for the first N days.
    let avgGain = gains.slice(0, Periods).reduce((acc, gain) => acc + gain, 0) / Periods;
    let avgLoss = losses.slice(0, Periods).reduce((acc, loss) => acc + loss, 0) / Periods;

    // Calculate the initial RSI value.
    let RS = avgGain / avgLoss;
    let initialRSI = 100 - (100 / (1 + RS));

    // Initialize RSI as an array with the initial RSI value.
    let RSI = [initialRSI];

    // Calculate the RSI for the remaining days.
    for (let i = Periods; i < gains.length; i++) {
        avgGain = ((avgGain * (Periods - 1)) + gains[i]) / Periods;
        avgLoss = ((avgLoss * (Periods - 1)) + losses[i]) / Periods;

        RS = avgGain / avgLoss;
        let currentRSI = 100 - (100 / (1 + RS));
        RSI.push(currentRSI);
    }

    return RSI;
}

// Compute the Moving Average Convergence Divergence (MACD) for the given period.
function computeMACD(timeSeries) {
    const closingPrices = Object.values(timeSeries).map(day => parseFloat(day["4. close"]));

    //Compute the 12-period EMA and 26-period EMA
    let shortEMA = computeEMA(timeSeries, 12);
    let longEMA = computeEMA(timeSeries, 26);

    //Compute MACD Line
    let MACD = [];
    for (let i = 0; i < closingPrices.length; i++) {
        // Ensure that we have values for both EMA12 and EMA26 before subtracting.
        if (shortEMA[i] !== undefined && longEMA[i] !== undefined) {
            MACD[i] = shortEMA[i] - longEMA[i];
        }
    }

    //Compute Signal Line
    let signalLine = computeEMA(MACD, 9);

    //Compute MACD Histogram
    let histogram = [];
    for (let i = 0; i < MACD.length; i++) {
        if (MACD[i] !== undefined && signalLine[i] !== undefined) {
            histogram[i] = MACD[i] - signalLine[i];
        }
    }

    return {
        MACD: MACD,
        signalLine: signalLine,
        histogram: histogram
    };
}

// Display stock data, SMA, and EMA on the web page.
function displayStock(data, Interval, Periods, SMA, EMA, RSI, MACD) {
    const output = document.getElementById("StockResult");

    let timeSeriesDate;

    // Determine the time series data based on the selected Interval.
    switch (Interval) {
        case "Daily":
            // For daily data, use the "Time Series (Daily)" key.
            timeSeriesDate = "Time Series (Daily)";
            break;
        case "Weekly":
            // For weekly data, use the "Weekly Time Series" key.
            timeSeriesDate = "Weekly Time Series";
            break;
        case "Monthly":
            // For monthly data, use the "Monthly Time Series" key.
            timeSeriesDate = "Monthly Time Series";
            break;
        default:
            alert("Please select a valid Interval.");
            return;
    }

    // Extract the time series data for the selected Interval.
    const timeSeries = data[timeSeriesDate];

    if (!timeSeries) {
        alert("No data available for the selected Interval.");
        return;
    }

    // Extract the first N dates from the time series data.
    const dates = Object.keys(timeSeries).slice(0, Periods);

    let lastMACDValue = (MACD.MACD && MACD.MACD.length > 0) ? MACD.MACD[MACD.MACD.length - 1].toFixed(2) : "N/A";
    let signalLineValue = (MACD.signalLine && MACD.signalLine.length > 0) ? MACD.signalLine[MACD.signalLine.length - 1].toFixed(2) : "N/A";
    let histogramValue = (MACD.histogram && MACD.histogram.length > 0) ? MACD.histogram[MACD.histogram.length - 1].toFixed(2) : "N/A";

    // Update the output with the SMA and EMA values.
    output.innerHTML = ` 
    <div>
        <p><strong>SMA:</strong> ${SMA.toFixed(2)}</p>
        <p><strong>EMA:</strong> ${EMA[EMA.length - 1].toFixed(2)}</p>
        <p><strong>RSI:</strong> ${RSI[RSI.length - 1].toFixed(2)}</p>
        <p><strong>MACD:</strong> ${lastMACDValue}</p>
        <p><strong>Signal Line:</strong> ${signalLineValue}</p>
        <p><strong>Histogram:</strong> ${histogramValue}</p>
    </div>
    `;

    // Display each stock data entry in a card format.
    for (const date of dates) {
        // Extract the stock information for the current date.
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
