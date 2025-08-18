export function drawChart(activityData, labels, chartName, chartTitle, unit) {
  var ctx = document.getElementById(chartName).getContext("2d");

  // Destroy existing chart instance if it exists
  if (window[chartName] instanceof Chart) {
    window[chartName].destroy();
  }

  let datasets = [];

  for (const [key, value] of Object.entries(activityData)) {
    datasets.push({
      label: key,
      data: value,
    });
  }

  window[chartName] = new Chart(ctx, {
    type: "bar",
    data: {
      labels: labels,
      datasets: datasets,
    },
    options: {
      plugins: {
        title: {
          display: true,
          text: chartTitle,
          color: "rgba(54, 162, 235, 1)",
          font: {
            size: 20,
          },
        },
        tooltip: {
          callbacks: {
            label: (context) => {
              let label = context.dataset.label || "";

              if (label) {
                label += ": ";
              }

              if (context.parsed.y !== null) {
                label += Number.isInteger(context.parsed.y)
                  ? context.parsed.y
                  : context.parsed.y.toFixed(1);
              }

              if (unit) {
                label += " " + unit;
              }

              return label;
            },
          },
        },
      },
      responsive: true,
      scales: {
        x: {
          stacked: true,
        },
        y: {
          stacked: true,
          beginAtZero: true,
        },
      },
    },
  });
}
