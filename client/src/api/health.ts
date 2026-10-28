export async function wakeServer(): Promise<void> {
  while (true) {
    try {
      const response = await fetch("/api/health");
      if (response.ok) {
        return;
      }
    } catch {
      // server is not ready yet
    }

    await new Promise((resolve) => setTimeout(resolve, 3000));
  }
}