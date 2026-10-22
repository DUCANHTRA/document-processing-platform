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