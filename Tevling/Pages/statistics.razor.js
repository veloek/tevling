window.drawChart = function (data, labels) {
    var ctx = document.getElementById('myChart').getContext('2d');
    var canvas = document.getElementById('myChart');
    
    canvas.width = 100;
    canvas.height = 100;

    // Destroy existing chart instance if it exists
    if (window.myChart instanceof Chart) {
        window.myChart.destroy();
    }

    window.myChart = new Chart(ctx, {
        type: 'line', // Change to 'line', 'pie', etc. if needed
        data: {
            labels: labels,
            datasets: [{
                label: 'Distance [km]',
                data: data,
                backgroundColor: 'rgba(184,223,255,0.5)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            scales: {
                y: {
                    beginAtZero: true,
                    
                }
            },
            
        }
    });
};

