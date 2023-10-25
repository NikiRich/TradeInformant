document.addEventListener("DOMContentLoaded", function () {
    fetch('/Gainers_Losers')
        .then(response => response.json())
        .then(data => {
            const gainersList = document.getElementById('topGainersList');
            data.forEach(stock => {
                const li = document.createElement('li');
                li.innerHTML = `
                    <p><strong>Name:</strong> ${stock.name}</p>
                    <p><strong>Price:</strong> $${stock.price}</p>
                `;
                gainersList.appendChild(li);
            });
        });

    fetch('/api/GetTopLosers')
        .then(response => response.json())
        .then(data => {
            const losersList = document.getElementById('topLosersList');
            data.forEach(stock => {
                const li = document.createElement('li');
                li.innerHTML = `
                    <p><strong>Name:</strong> ${stock.name}</p>
                    <p><strong>Price:</strong> $${stock.price}</p>
                `;
                losersList.appendChild(li);
            });
        });
});
