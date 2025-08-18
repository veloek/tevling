window.DrawChallengeStats = function (canvasId, data, labels, chartTitle) {
    var ctx = document.getElementById(canvasId).getContext('2d');
    var canvas = document.getElementById(canvasId);

    // Destroy existing chart instance if it exists
    if (window[canvasId] instanceof Chart) {
        window[canvasId].destroy();
    }


    window[canvasId] = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                label: chartTitle
            }]
        }
    });
};
