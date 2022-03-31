

const params = new Proxy(new URLSearchParams(window.location.search), {
    get: (searchParams, prop) => searchParams.get(prop),
});

let value = params.cif;

const eventSource = new EventSource("/sse?cif="+value, { withCredentials: true });

eventSource.onmessage = (event) => {
    console.log(event.data)
}


eventSource.addEventListener("close", (event) => {
    eventSource.close()
});