// window.drawChart = function (activityData) {
//     var ctx = document.getElementById('myChart').getContext('2d');
//     var canvas = document.getElementById('myChart');
//
//     canvas.width = 100;
//     canvas.height = 100;
//    
//     console.log(activityData);
//     // Destroy existing chart instance if it exists
//     if (window.myChart instanceof Chart) {
//         window.myChart.destroy();
//     }
//    
//     let activityTypes = Object.keys(activityData);
//     let datasets = [];
//    
//     for(const [key, value] of Object.entries(activityData)) {
//         console.log(value);
//         datasets.push({
//             label: key,
//             data: Object.values(value)
//         });
//     }
//    
//     console.log(Object.keys(Object.values(activityData)[0]));
//    
//     window.myChart = new Chart(ctx, {
//         type: 'line', // Change to 'line', 'pie', etc. if needed
//         data: {
//             labels: Object.keys(Object.values(activityData)[0]),
//             datasets: datasets
//         },
//         options: {
//             plugins: {
//                 title: {
//                     display: true,
//                     text: 'Total Distance [m]',
//                     color: 'rgba(54, 162, 235, 1)',
//                     font: {
//                         size: 20
//                     },
//                 },
//             },
//             responsive: true,
//             scales: {
//                 y: {
//                     beginAtZero: true,
//
//                 }
//             },
//
//         }
//     });
// };

window.drawChart = function (activityData, labels, chartName, chartTitle) {
    var ctx = document.getElementById(chartName).getContext('2d');
    var canvas = document.getElementById(chartName);

    // canvas.width = 100;
    // canvas.height = 100;

    // Destroy existing chart instance if it exists
    if (window.chartName instanceof Chart) {
        window.chartName.destroy();
    }
    
    let datasets = [];

    for (const [key, value] of Object.entries(activityData)) {
        datasets.push({
            label: key,
            data: value
        });
    }
    
    window.myChart = new Chart(ctx, {
        type: 'line', // Change to 'line', 'pie', etc. if needed
        data: {
            labels: labels,
            datasets: datasets
        },
        options: {
            plugins: {
                title: {
                    display: true,
                    text: chartTitle,
                    color: 'rgba(54, 162, 235, 1)',
                    font: {
                        size: 20
                    },
                },
            },
            responsive: true,
            scales: {
                y: {
                    beginAtZero: true,

                }
            },

        }
    });
};
