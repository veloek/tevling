export function convert(dateString) {
    let d = new Date(dateString);

    if (isNaN(d.valueOf())) {
        return null;
    }

    let year = pad(d.getFullYear(), 4);
    let month = pad(d.getMonth() + 1, 2);
    let date = pad(d.getDate(), 2);
    let hours = pad(d.getHours(), 2);
    let minutes = pad(d.getMinutes(), 2);
    let seconds = pad(d.getSeconds(), 2);
    let millis = pad(d.getMilliseconds(), 3);
    let tz = formatTz(d.getTimezoneOffset());

    return `${year}-${month}-${date}T${hours}:${minutes}:${seconds}.${millis}${tz}`;
}

function pad(num, n) {
    for (let i = 1; i < n; i++)
        if (num < Math.pow(10, i))
            return "0".repeat(n - i) + num;

    return "" + num;
}

function formatTz(tz) {
    let abs = Math.abs(tz);
    let hours = Math.floor(abs / 60);
    let minutes = Math.floor(abs % 60);

    // The number of minutes returned by getTimezoneOffset() is positive if the
    // local time zone is behind UTC, and negative if the local time zone is
    // ahead of UTC.
    let sign = tz < 0 ? "+" : "-";

    return sign + pad(hours, 2) + ":" + pad(minutes, 2);
}
