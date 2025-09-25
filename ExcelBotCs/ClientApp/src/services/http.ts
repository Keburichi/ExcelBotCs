export async function http<T>(input: RequestInfo, init?: RequestInit): Promise<T> {
    const res = await fetch(input, {
        credentials: 'same-origin',
        headers: {'Content-Type': 'application/json', ...(init?.headers || {})},
        ...init
    })
    if (res.status === 401) throw new Error('unauthorized')
    if (!res.ok) throw new Error(await res.text())

    // No content statuses
    if (res.status === 204 || res.status === 205) {
        return undefined as unknown as T; // works for T = void
    }

    // Some servers return 200 with empty body; handle that too
    const text = await res.text();
    if (!text) {
        return undefined as unknown as T; // no payload
    }

    return JSON.parse(text) as T;
}