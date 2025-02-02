﻿//An event listener for the form submission.
document.getElementById("StockForm").addEventListener("submit", function (event) {
    // Prevent the default form submission behavior.
    event.preventDefault();

    // Getting the input values from the form.
    const StockName = document.getElementById("StockName").value;
    const Interval = document.getElementById("Interval").value;
    const Periods = parseInt(document.getElementById("Periods").value);

    // Validating the period input.
    if (isNaN(Periods) || Periods < 1 || Periods > 100) {
        alert("Please enter a valid number of Periods.");
        return;
    }

    // Preparing the query parameters for the fetch request.
    const query = new URLSearchParams({
        StockName: StockName,
        Interval: Interval,
        Periods: Periods
    });

    // Making a fetch request to the server with the query parameters.
    fetch("/Stocks?" + query.toString())
        // Returning the response as JSON if the request was successful.
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
            // Extracting the time series data based on the selected Interval.
            const timeSeries = GetTimeSeries(data, Interval);

            // Computing SMA, EMA, RSI, and MACD.
            const SMA = ComputeSMA(timeSeries, Periods);
            const EMA = ComputeEMA(timeSeries, Periods);
            const RSI = ComputeRSI(timeSeries, Periods);
            const MACD = ComputeMACD(timeSeries);
            // Displaying the stock information.
            DisplayStock(data, Interval, Periods, SMA, EMA, RSI, MACD);
            // Sending the indicators to the server for prediction.
            const indicators = {
                SMA: SMA,
                EMA: EMA && EMA.length > 0 ? EMA[EMA.length - 1] : null,
                RSI: RSI && RSI.length > 0 ? RSI[RSI.length - 1] : null,
                MACD: MACD && MACD.MACD && MACD.MACD.length > 0 ? MACD.MACD[MACD.MACD.length - 1] : null,
                signalLine: MACD && MACD.signalLine && MACD.signalLine.length > 0 ? MACD.signalLine[MACD.signalLine.length - 1] : null,
                histogram: MACD && MACD.histogram && MACD.histogram.length > 0 ? MACD.histogram[MACD.histogram.length - 1] : null
            };

            return DataForMLA(indicators);

        })

        .catch(error => {
            console.error('Error:', error);
        })
});


// Getting the time series data based on the selected Interval.
function GetTimeSeries(data, Interval) {
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


function ComputeSMA(timeSeries, Periods) {
    // If the timeSeries data is not available, return null.
    if (!timeSeries) return null;
    // Extract closing prices and calculate the average.
    const closingPrices = Object.values(timeSeries).slice(0, Periods).map(day => parseFloat(day["4. close"]));
    const sum = closingPrices.reduce((acc, price) => acc + price, 0);
    return sum / Periods;
}


function ComputeEMA(input, Periods) {
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


function ComputeRSI(timeSeries, Periods) {
    let gains = [];
    let losses = [];

    // Extracting the closing prices from the time series data.
    const closingPrices = Object.values(timeSeries).map(day => parseFloat(day["4. close"]));

    // Calculating the gains and losses for each day.
    for (let i = 1; i < closingPrices.length; i++) {
        let change = closingPrices[i] - closingPrices[i - 1];
        gains.push(Math.max(change, 0)); // If change is negative, push 0
        losses.push(Math.max(-change, 0)); // If change is positive or 0, push 0
    }

    // Calculating the average gain and average loss for the first N days.
    let avgGain = gains.slice(0, Periods).reduce((acc, gain) => acc + gain, 0) / Periods;
    let avgLoss = losses.slice(0, Periods).reduce((acc, loss) => acc + loss, 0) / Periods;

    // Calculating the initial RSI value.
    let RS = avgGain / avgLoss;
    let initialRSI = 100 - (100 / (1 + RS));

    // Initializing RSI as an array with the initial RSI value.
    let RSI = [initialRSI];

    // Calculating the RSI for the remaining days.
    for (let i = Periods; i < gains.length; i++) {
        avgGain = ((avgGain * (Periods - 1)) + gains[i]) / Periods;
        avgLoss = ((avgLoss * (Periods - 1)) + losses[i]) / Periods;

        RS = avgGain / avgLoss;
        let currentRSI = 100 - (100 / (1 + RS));
        RSI.push(currentRSI);
    }

    return RSI;
}

function ComputeMACD(timeSeries) {
    const closingPrices = Object.values(timeSeries).map(day => parseFloat(day["4. close"]));

    //Compute the 12-period EMA and 26-period EMA
    let shortEMA = ComputeEMA(timeSeries, 12);
    let longEMA = ComputeEMA(timeSeries, 26);

    //Computing MACD
    let MACD = [];
    for (let i = 0; i < closingPrices.length; i++) {
        // Ensure that we have values for both EMA12 and EMA26 before subtracting.
        if (shortEMA[i] !== undefined && longEMA[i] !== undefined) {
            MACD[i] = shortEMA[i] - longEMA[i];
        }
    }

    //Computing Signal Line
    let signalLine = ComputeEMA(MACD, 9);

    //Computing MACD Histogram
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

function DisplayStock(data, Interval, Periods, SMA, EMA, RSI, MACD, prediction = null) {
    const output = document.getElementById("StockResult");

    if (prediction !== null) {
        PredictionDisplay({ Prediction: prediction })
    }

    let timeSeriesDate;

    // Determining the time series data based on the selected Interval.
    switch (Interval) {
        case "Daily":
            // For daily data, using the "Time Series (Daily)" key.
            timeSeriesDate = "Time Series (Daily)";
            break;
        case "Weekly":
            // For weekly data, using the "Weekly Time Series" key.
            timeSeriesDate = "Weekly Time Series";
            break;
        case "Monthly":
            // For monthly data, using the "Monthly Time Series" key.
            timeSeriesDate = "Monthly Time Series";
            break;
        default:
            alert("Please select a valid Interval.");
            return;
    }

    // Extracting the time series data for the selected Interval.
    const timeSeries = data[timeSeriesDate];

    if (!timeSeries) {
        alert("No data available for the selected Interval.");
        return;
    }

    // Extracting the first N dates from the time series data.
    const dates = Object.keys(timeSeries).slice(0, Periods);

    // Extracting the stock information for the latest date.
    let lastMACDValue = (MACD.MACD && MACD.MACD.length > 0) ? MACD.MACD[MACD.MACD.length - 1].toFixed(2) : "N/A";
    let signalLineValue = (MACD.signalLine && MACD.signalLine.length > 0) ? MACD.signalLine[MACD.signalLine.length - 1].toFixed(2) : "N/A";
    let histogramValue = (MACD.histogram && MACD.histogram.length > 0) ? MACD.histogram[MACD.histogram.length - 1].toFixed(2) : "N/A";

    // Updating the output with the stock information.
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

    // Displaying each stock data entry in a card format.
    for (const date of dates) {
        // Extracting the stock information for the current date.
        const stockInfo = timeSeries[date];
        output.innerHTML += `
        <div class="col-md-3">
            <div class="card md-3">
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


function DataForMLA(indicators) {
    // Constructing query string from indicators object
    const query2 = new URLSearchParams(indicators).toString();
    // Sending the indicators to the server for prediction.
    return fetch(`/Stocks?handler=PredictionCalculation&${query2}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok ' + response.statusText);
            }
            return response.json();
        })
        .then(predictionData => {
            PredictionDisplay(predictionData);
        })
        .catch(error => {
            console.error('Error sending indicators to server:', error);
        });
}


function PredictionDisplay(predictionData) {
    const predictionElement = document.getElementById('PredictionResult');
    predictionElement.innerHTML = `
    <div>
        <p><strong>Prediction:</strong> ${predictionData.prediction}</p>
    </div>
    `;
}

document.getElementById("TrainModel").addEventListener("click", function (event) {
    TrainModel();
});


function TrainModel() {
    const trainingData = {
        Features: [
            { "SMA": 151.41, "EMA": 133.73, "RSI": 52.23, "MACD": 4.60, "signalLine": 4.67, "histogram": -0.22 },
            { "SMA": 143.92, "EMA": 129.33, "RSI": 34.54, "MACD": -0.19, "signalLine": 0.76, "histogram": 1.07 },
            { "SMA": 136.05, "EMA": 82.53, "RSI": 40.06, "MACD": -10.02, "signalLine": -9.89, "histogram": -0.24 },
            { "SMA": 234.37, "EMA": 267.61, "RSI": 24.41, "MACD": -7.90, "signalLine": -11.00, "histogram": -4.83 },
            { "SMA": 236.93, "EMA": 19.95, "RSI": 32.73, "MACD": 3.66, "signalLine": 4.23, "histogram": -0.89 },
            { "SMA": 361.84, "EMA": 337.48, "RSI": 44.49, "MACD": 6.60, "signalLine": -0.79, "histogram": -6.72 },
            { "SMA": 479.51, "EMA": 426.96, "RSI": 28.96, "MACD": 13.08, "signalLine": 6.09, "histogram": -13.95 },
            { "SMA": 108.75, "EMA": 115.84, "RSI": 51.49, "MACD": 0.04, "signalLine": -0.42, "histogram": -1.43 }
        ],
        Labels: [
            "Sell",
            "Sell",
            "Sell",
            "Buy",
            "Sell",
            "Sell",
            "Sell",
            "Buy",
        ]
    };
    const query2 = new URLSearchParams();
    for (let i = 0; i < trainingData.Features.length; i++) {
        query2.append(`Features[${i}][SMA]`, trainingData.Features[i].SMA);
        query2.append(`Features[${i}][EMA]`, trainingData.Features[i].EMA);
        query2.append(`Features[${i}][RSI]`, trainingData.Features[i].RSI);
        query2.append(`Features[${i}][MACD]`, trainingData.Features[i].MACD);
        query2.append(`Features[${i}][signalLine]`, trainingData.Features[i].signalLine);
        query2.append(`Features[${i}][histogram]`, trainingData.Features[i].histogram);
        query2.append(`Labels[${i}]`, trainingData.Labels[i]);
    }

    return fetch(`/Stocks?handler=TrainModel&${query2.toString()}`, {
        method: "GET"
    })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok ' + response.statusText);
            }
            return response.json();
        })
        .then(result => {
            console.log('Training completed:', result);
        })
        .catch(error => {
            console.error('Error during model training:', error);
        });
}
