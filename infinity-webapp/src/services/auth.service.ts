import { HttpClient } from "@angular/common/http";
import { inject } from "@angular/core";

export class AuthService {
    private API_URL = 'https://unrealistic-skyla-demagogically.ngrok-free.dev/auth';
    private http: HttpClient = inject(HttpClient);

    public discordLogin() {
        return this.http.get(`${this.API_URL}/discord/login`, { withCredentials: true });
    }

    public checkAuth() {
        return this.http.get(`${this.API_URL}/discord/session`, { withCredentials: true });
    }
}
