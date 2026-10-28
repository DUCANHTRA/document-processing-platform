interface LoginResponse {
  token: string;
}

const tokenKey = "document-processing-token";

const MAX_RETRIES = 10;
const RETRY_INTERVAL_MS = 5000;
const RETRIABLE_STATUSES = [502, 503, 504];

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export async function login(
  email: string,
  password: string,
  onRetry?: (attempt: number, total: number) => void
): Promise<void> {
  for (let attempt = 1; attempt <= MAX_RETRIES; attempt++) {
    const response = await fetch("/api/auth/login", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ email, password }),
    });

    if (response.ok) {
      const data: LoginResponse = await response.json();
      sessionStorage.setItem(tokenKey, data.token);
      return;
    }

    // Retriable gateway errors — server is cold-starting
    if (RETRIABLE_STATUSES.includes(response.status)) {
      if (attempt < MAX_RETRIES) {
        onRetry?.(attempt, MAX_RETRIES);
        await delay(RETRY_INTERVAL_MS);
        continue;
      }
      // All retries exhausted
      throw new Error("Could not reach the server. Please try again later.");
    }

    // Hard failures — do not retry
    if (response.status === 401) {
      throw new Error("Email or password is incorrect.");
    }
    throw new Error("Email or password is incorrect.");
  }
}

export function getToken(): string | null {
  return sessionStorage.getItem(tokenKey);
}


export async function register(email: string, password: string): Promise<void> {
  const response = await fetch("/api/auth/register", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ email, password }),
  });

  if (response.status === 409) {
    throw new Error("An account with this email already exists.");
  }

  if (!response.ok) {
    throw new Error("Registration failed.");
  }
}

// Read saved token from sessionStorage and adds to request header Authorization - Bearer
export async function authenticatedFetch(
  input: RequestInfo | URL,
  init: RequestInit = {}
): Promise<Response> {
  const headers = new Headers(init.headers);
  const token = getToken();

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  return fetch(input, { ...init, headers });
}