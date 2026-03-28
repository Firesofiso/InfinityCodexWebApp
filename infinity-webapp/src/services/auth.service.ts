import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, finalize, shareReplay, tap } from 'rxjs/operators';

export interface SessionResponse {
    isAuthenticated: boolean;
    name?: string;
    claims?: Array<{ type: string; value: string }>;
}

export type AuthState = 'checking' | 'authenticated' | 'unauthenticated';

@Injectable({ providedIn: 'root' })
export class AuthService {
    // Use a relative API path so Angular dev can proxy to the local ASP.NET app and production can stay same-origin.
    private readonly API_URL = '/auth';
    private http: HttpClient = inject(HttpClient);
    private readonly authStateSignal = signal<AuthState>('checking');
    private readonly sessionSignal = signal<SessionResponse | null>(null);
    private readonly authErrorSignal = signal<string | null>(null);
    private sessionRequest$: Observable<SessionResponse> | null = null;

    public readonly authState = this.authStateSignal.asReadonly();
    public readonly session = this.sessionSignal.asReadonly();
    public readonly authError = this.authErrorSignal.asReadonly();

    public getDiscordLoginUrl(returnUrl: string = window.location.href): string {
        const loginUrl = new URL(`${this.API_URL}/discord/login`, window.location.origin);
        loginUrl.searchParams.set('returnUrl', returnUrl);
        return loginUrl.toString();
    }

    public hasAuthenticatedSession(): boolean {
        return this.authStateSignal() === 'authenticated' && this.sessionSignal() !== null;
    }

    public refreshSession(force: boolean = false): Observable<SessionResponse> {
        if (!force && this.sessionRequest$) {
            return this.sessionRequest$;
        }

        this.authStateSignal.set('checking');
        this.authErrorSignal.set(null);

        const request$ = this.http.get<SessionResponse>(`${this.API_URL}/discord/session`, { withCredentials: true }).pipe(
            tap((response) => {
                if (response.isAuthenticated) {
                    this.sessionSignal.set(response);
                    this.authStateSignal.set('authenticated');
                    return;
                }

                this.clearSession();
            }),
            catchError((error: unknown) => {
                this.clearSession('Unable to verify the current Discord session.');
                return throwError(() => error);
            }),
            finalize(() => {
                this.sessionRequest$ = null;
            }),
            shareReplay(1)
        );

        this.sessionRequest$ = request$;
        return request$;
    }

    public logout(): Observable<{ message: string; revoked: boolean; sessionCleared: boolean }> {
        this.authErrorSignal.set(null);

        return this.http.post<{ message: string; revoked: boolean; sessionCleared: boolean }>(
            `${this.API_URL}/logout`,
            {},
            { withCredentials: true }
        ).pipe(
            tap(() => {
                this.clearSession();
            }),
            catchError((error: unknown) => {
                this.clearSession('Logout did not complete cleanly, but the local session was cleared.');
                return throwError(() => error);
            })
        );
    }

    public clearSession(errorMessage: string | null = null): void {
        this.sessionSignal.set(null);
        this.authStateSignal.set('unauthenticated');
        this.authErrorSignal.set(errorMessage);
    }
}
