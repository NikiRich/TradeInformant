document.getElementById("CryptoForm").addEventListener("submit", function (event) {
    event.preventDefault();

    const cryptoName = document.getElementById("cryptoName").value;
    const interval = document.getElementById("interval").value;
    const market = "CNY";
    const periods = parseInt(document.getElementById("periods").value);

    if (isNaN(periods) || periods < 1 || periods > 100) {
        alert("Please enter a valid number of periods.");
        return;
    }

    const query = new URLSearchParams({
        cryptoName: cryptoName, 
        interval: interval,
        market: market,
        periods: periods
    });

    fetch("/Cryptos?" + query.toString())
        .then(response => {
            if (!response.ok) {
                return response.text().then(text => {
                    throw new Error(text);
                });
            }
            return response.json();
        })
        .then(data => {
            displayCrypto(data, interval, periods); 
        })
        .catch(error => {
            console.error("Error:", error);
        });
});

function displayCrypto(data, interval, periods) {
    console.log(data);
    const output = document.getElementById("CryptoResult");
    output.innerHTML = "";

    let timeSeriesDate;
    switch (interval) {
        case "Daily":
            timeSeriesDate = "Time Series (Digital Currency Daily)";
            break;
        case "Weekly":
            timeSeriesDate = "Time Series (Digital Currency Weekly)";
            break;
        case "Monthly":
            timeSeriesDate = "Time Series (Digital Currency Monthly)";
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
        const cryptoInfo = timeSeries[date];
        output.innerHTML += `
            <div id="CryptoOutput">
                <p>Date: ${date}</p>
                <p>Open: ${cryptoInfo["1b. open (USD)"]}</p>
                <p>High: ${cryptoInfo["2b. high (USD)"]}</p>
                <p>Low: ${cryptoInfo["3b. low (USD)"]}</p>
                <p>Close: ${cryptoInfo["4b. close (USD)"]}</p>
                <p>Volume: ${cryptoInfo["5. volume"]}</p>
                <p>Market Cap: ${cryptoInfo["6. market cap (USD)"]}</p>
            </div>
        `;
    }
}

