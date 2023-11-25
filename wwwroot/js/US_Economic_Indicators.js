
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










/*
if ((RealGDPperiod < 1 || RealGDPperiod > 100) || (RealGDPperCapitaPeriod < 1 || RealGDPperCapitaPeriod > 100) ||
    (CPIperiod < 1 || CPIperiod > 100) || (InflationPeriod < 1 || InflationPeriod > 100) || (UnemploymentRatePeriod < 1 || UnemploymentRatePeriod > 100)) {
    alert("Enter a valid number")
    return;
}
const CPI = document.getElementById("CPI").value;
const CPIperiod = document.getElementById("CPIperiod").value;
const InflationPeriod = document.getElementById("InflationPeriod").value;
const UnemploymentRatePeriod = document.getElementById("UnemploymentRatePeriod").value;
*/