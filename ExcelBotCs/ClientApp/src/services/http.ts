/** The non-throwing outcome of a request: its status, its parsed body and whether the server answered at all. */
export interface HttpOutcome<T> {
  Ok: boolean
  Status: number
  Data: T | null
  /** True when the request never reached the server, so there is no HTTP status to report. */
  Unreachable: boolean
}

function send(input: RequestInfo, init?: RequestInit): Promise<Response> {
  return fetch(input, {
    credentials: 'same-origin',
    headers: { 'Content-Type': 'application/json', ...(init?.headers || {}) },
    ...init,
  })
}

export async function http<T>(input: RequestInfo, init?: RequestInit): Promise<T> {
  const res = await send(input, init)
  if (res.status === 401)
    throw new Error('unauthorized')
  if (!res.ok)
    throw new Error(await res.text())

  // No content statuses
  if (res.status === 204 || res.status === 205) {
    return undefined as unknown as T // works for T = void
  }

  // Some servers return 200 with empty body; handle that too
  const text = await res.text()
  if (!text) {
    return undefined as unknown as T // no payload
  }

  return JSON.parse(text) as T
}

/**
 * The same request as `http`, but it never throws: a non-2xx status and a network failure both come back as a value.
 * Admin endpoints answer expected failures with a typed body (a field error, a disposition, a code), so the page must
 * read that body instead of catching an exception that already discarded it.
 */
export async function httpOutcome<T>(input: RequestInfo, init?: RequestInit): Promise<HttpOutcome<T>> {
  try {
    const res = await send(input, init)

    let data: T | null = null
    if (res.status !== 204 && res.status !== 205) {
      const text = await res.text()
      if (text) {
        try {
          data = JSON.parse(text) as T
        }
        catch {
          data = null
        }
      }
    }

    return { Ok: res.ok, Status: res.status, Data: data, Unreachable: false }
  }
  catch {
    return { Ok: false, Status: 0, Data: null, Unreachable: true }
  }
}
