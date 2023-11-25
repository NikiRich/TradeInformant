
document.getElementById("RealGDPForm").addEventListener("submit", function (event) {
    // Preventing the form from being submitted
    event.preventDefault();
    // Getting the value from the form
    const RealGDPperiod = document.getElementById("RealGDPperiod").value;
    // Validating the input
    if (RealGDPperiod < 1 || RealGDPperiod > 100) {
        alert("Enter a valid number")
        return;
    }
    // Prepaing the query string for the API
    const query1 = new URLSearchParams({
        RealGDPperiod: RealGDPperiod,
    });

    fetch(`/US_Economic_Indicators?handler=RGDP&${query1.toString()}`, {
        method: "GET"
    })

        .then(response => {
            if (!response.ok) {
                throw new Error("Network response was not ok");
            };
            return response.json();
        })
        .then(result => {
            DisplayQuery1(result, RealGDPperiod);
        })
        .catch(error => {
            console.error("Error:", error);
        })
})

function DisplayQuery1(result, RealGDPperiod) {
    const RealGDPOutput = document.getElementById("RealGDPOutput");
    RealGDPOutput.innerHTML = '';
    const timeSeries = result["data"].slice(0, RealGDPperiod);

    for (const entry of timeSeries) {
        RealGDPOutput.innerHTML += `
        <div class="col-md-2">
            <div class="card md-2">
                <div class="card-header">
                    Date:${entry.date}
                </div>
                <div class="card-body">
                    <p><strong>Real GDP:</strong> ${entry.value}</p>
                </div>
            </div>
        </div>
        `;
    }
}

document.getElementById("RealGDPperCapitaForm").addEventListener("submit", function (event) {
    // Preventing the form from being submitted
    event.preventDefault();
    // Getting the value from the form
    const RealGDPperCapitaPeriod = document.getElementById("RealGDPperCapitaPeriod").value;
    // Validating the input
    if (RealGDPperCapitaPeriod < 1 || RealGDPperCapitaPeriod > 100) {
        alert("Enter a valid number")
        return;
    }
    // Prepaing the query string for the API
    const query2 = new URLSearchParams({
        RealGDPperCapitaPeriod: RealGDPperCapitaPeriod,
    });

    fetch(`/US_Economic_Indicators?handler=RGDPpCP&${query2.toString()}`, {
        method: "GET"
    })

        .then(response => {
            if (!response.ok) {
                throw new Error("Network response was not ok");
            };
            return response.json();
        })
        .then(result => {
            DisplayQuery2(result, RealGDPperCapitaPeriod);
        })
        .catch(error => {
            console.error("Error:", error);
        })
})

function DisplayQuery2(result, RealGDPperCapitaPeriod) {
    const RealGDPperCapitaOutput = document.getElementById("RealGDPperCapitaOutput");
    RealGDPperCapitaOutput.innerHTML = '';
    const timeSeries = result["data"].slice(0, RealGDPperCapitaPeriod);

    for (const entry of timeSeries) {
        RealGDPperCapitaOutput.innerHTML += `
        <div class="col-md-2">
            <div class="card md-2">
                <div class="card-header">
                    Date:${entry.date}
                </div>
                <div class="card-body">
                    <p><strong>Real GDP Per Capita:</strong> ${entry.value}</p>
                </div>
            </div>
        </div>
        `;
    }
}


document.getElementById("CPIForm").addEventListener("submit", function (event) {
    // Preventing the form from being submitted
    event.preventDefault();
    // Getting the value from the form
    const CPIperiod = document.getElementById("CPIperiod").value;
    const CPI = document.getElementById("CPI").value;
    // Validating the input
    if (CPIperiod < 1 || CPIperiod > 100) {
        alert("Enter a valid number")
        return;
    }
    // Prepaing the query string for the API
    const query3 = new URLSearchParams({
        CPIperiod: CPIperiod,
        CPI: CPI
    });

    fetch(`/US_Economic_Indicators?handler=CPI&${query3.toString()}`, {
        method: "GET"
    })

        .then(response => {
            if (!response.ok) {
                throw new Error("Network response was not ok");
            };
            return response.json();
        })
        .then(result => {
            DisplayQuery3(result, CPIperiod);
        })
        .catch(error => {
            console.error("Error:", error);
        })
})


function DisplayQuery3(result, CPIperiod) {
    const CPIOutput = document.getElementById("CPIOutput");
    CPIOutput.innerHTML = '';
    const timeSeries = result["data"].slice(0, CPIperiod);

    for (const entry of timeSeries) {
        RealGDPperCapitaOutput.innerHTML += `
        <div class="col-md-2">
            <div class="card md-2">
                <div class="card-header">
                    Date:${entry.date}
                </div>
                <div class="card-body">
                    <p><strong>CPI:</strong> ${entry.value}</p>
                </div>
            </div>
        </div>
        `;
    }
}


document.getElementById("InflationForm").addEventListener("submit", function (event) {
    // Preventing the form from being submitted
    event.preventDefault();
    // Getting the value from the form
    const InflationPeriod = document.getElementById("InflationPeriod").value;
    // Validating the input
    if (InflationPeriod < 1 || InflationPeriod > 100) {
        alert("Enter a valid number")
        return;
    }
    // Prepaing the query string for the API
    const query4 = new URLSearchParams({
        InflationPeriod: InflationPeriod,
    });

    fetch(`/US_Economic_Indicators?handler=Inflation&${query4.toString()}`, {
        method: "GET"
    })

        .then(response => {
            if (!response.ok) {
                throw new Error("Network response was not ok");
            };
            return response.json();
        })
        .then(result => {
            DisplayQuery4(result, InflationPeriod);
        })
        .catch(error => {
            console.error("Error:", error);
        })
})

function DisplayQuery4(result, InflationPeriod) {
    const InflationOutput = document.getElementById("InflationOutput");
    InflationOutput.innerHTML = '';
    const timeSeries = result["data"].slice(0, InflationPeriod);

    for (const entry of timeSeries) {
        InflationOutput.innerHTML += `
        <div class="col-md-2">
            <div class="card md-2">
                <div class="card-header">
                    Date:${entry.date}
                </div>
                <div class="card-body">
                    <p><strong>Inflation:</strong> ${entry.value}</p>
                </div>
            </div>
        </div>
        `;
    }
}

document.getElementById("InflationForm").addEventListener("submit", function (event) {
    // Preventing the form from being submitted
    event.preventDefault();
    // Getting the value from the form
    const UnemploymentRatePeriod = document.getElementById("UnemploymentRatePeriod").value;
    // Validating the input
    if (UnemploymentRatePeriod < 1 || UnemploymentRatePeriod > 100) {
        alert("Enter a valid number")
        return;
    }
    // Prepaing the query string for the API
    const query5 = new URLSearchParams({
        UnemploymentRatePeriod: UnemploymentRatePeriod,
    });

    fetch(`/US_Economic_Indicators?handler=Unemployment&${query5.toString()}`, {
        method: "GET"
    })

        .then(response => {
            if (!response.ok) {
                throw new Error("Network response was not ok");
            };
            return response.json();
        })
        .then(result => {
            DisplayQuery5(result, UnemploymentRatePeriod);
        })
        .catch(error => {
            console.error("Error:", error);
        })
})

function DisplayQuery5(result, UnemploymentRatePeriod) {
    const UnemploymentRateOutput = document.getElementById("UnemploymentRateOutput");
    UnemploymentRateOutput.innerHTML = '';
    const timeSeries = result["data"].slice(0, UnemploymentRatePeriod);

    for (const entry of timeSeries) {
        UnemploymentRateOutput.innerHTML += `
        <div class="col-md-2">
            <div class="card md-2">
                <div class="card-header">
                    Date:${entry.date}
                </div>
                <div class="card-body">
                    <p><strong>Inflation:</strong> ${entry.value}</p>
                </div>
            </div>
        </div>
        `;
    }
}