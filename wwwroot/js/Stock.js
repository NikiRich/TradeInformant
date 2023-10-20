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