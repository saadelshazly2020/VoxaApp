export function getHubUrl(): string {
    // Always use relative URLs for SignalR
    // This works for both localhost and ngrok because ngrok acts as a reverse proxy
    return '/videocallhub';
}