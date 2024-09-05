export function initialize(loadMoreEl, componentRef) {
    const options = {
        root: findClosestScrollingContainer(loadMoreEl),
        rootMargin: "0px",
        threshold: 0,
    };

    const observer = new IntersectionObserver(async (entries) => {
        for (let entry of entries) {
            if (entry.isIntersecting) {
                observer.unobserve(loadMoreEl);
                await componentRef.invokeMethodAsync("OnMarkerVisible");
            }
        }
    }, options);

    observer.observe(loadMoreEl);

    return {
        onNewItems: () => {
            observer.unobserve(loadMoreEl);
            observer.observe(loadMoreEl);
        },
        dispose: () => {
            observer.disconnect();
        }
    }
}

function findClosestScrollingContainer(el) {
    while (el != null) {
        const style = getComputedStyle(el);
        if (style.overflowY !== "visible") {
            return el;
        }
        el = el.parentElement;
    }
    return null;
}
