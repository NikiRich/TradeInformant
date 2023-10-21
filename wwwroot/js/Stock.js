document.getElementById("StockForm").addEventListener("submit", function (event) {
    event.preventDefault();

    const stockName = document.getElementById("stockName").value;
    const interval = document.getElementById("interval").value;
    const periods = parseInt(document.getElementById("periods").value);
});

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
        displayStock(data);
    })
    .catch(error => {
        console.error("Error:", error);
    });

function displayStock(data) {
    const output = document.getElementById("StockResult");
    output.innerHTML = "";

    data.forEach(stock => {
       
    });
}
