window.drawChart = function (activityData, labels, chartName, chartTitle) {
    var ctx = document.getElementById(chartName).getContext('2d');
    var canvas = document.getElementById(chartName);

    // Destroy existing chart instance if it exists
    if (window[chartName] instanceof Chart) {
        window[chartName].destroy();
    }

    let datasets = [];

    for (const [key, value] of Object.entries(activityData)) {
        datasets.push({
            label: key, data: value
        });
    }

    window[chartName] = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels, datasets: datasets
        }, options: {
            plugins: {
                title: {
                    display: true, text: chartTitle, color: 'rgba(54, 162, 235, 1)', font: {
                        size: 20
                    },
                },
            }, responsive: true, scales: {
                y: {
                    beginAtZero: true,

                }
            },

        }
    });
};
