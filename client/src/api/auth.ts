interface LoginResponse {
  token: string;
}

const tokenKey = "document-processing-token";

export async function login(email: string, password: string): Promise<void> {
  const response = await fetch("/api/auth/login", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) {
    throw new Error("Email or password is incorrect.");
  }

  const data: LoginResponse = await response.json();
  sessionStorage.setItem(tokenKey, data.token);
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