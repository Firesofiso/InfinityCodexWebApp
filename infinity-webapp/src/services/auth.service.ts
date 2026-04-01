import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError, finalize, shareReplay, tap } from 'rxjs/operators';

export interface SessionResponse {
    isAuthenticated: boolean;
    name?: string;
    registrationStatus?: 'pending' | 'complete' | null;
    isRegistrationComplete?: boolean;
    canAccessApp?: boolean;
    role?: string | null;
    claims?: Array<{ type: string; value: string }>;
}

export interface RegistrationContextResponse {
    isRegistrationComplete: boolean;
    displayName: string;
    preferredJobs: string[];
    characterNames: string[];
    discordName?: string;
}

export type AuthState = 'checking' | 'authenticated' | 'pending-registration' | 'unauthenticated';

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
        const session = this.sessionSignal();
        return this.authStateSignal() === 'authenticated'
            && session !== null
            && (session.registrationStatus ?? (session.isRegistrationComplete ? 'complete' : 'pending')) === 'complete';
    }

    public hasPendingRegistrationSession(): boolean {
        return this.authStateSignal() === 'pending-registration' && this.sessionSignal() !== null;
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
                    const registrationStatus = response.registrationStatus ?? (response.isRegistrationComplete ? 'complete' : 'pending');
                    this.authStateSignal.set(registrationStatus === 'complete' ? 'authenticated' : 'pending-registration');
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

    public getRegistrationContext(): Observable<RegistrationContextResponse> {
        return this.http.get<RegistrationContextResponse>(`${this.API_URL}/registration/context`, { withCredentials: true });
    }

    public completeRegistration(payload: { displayName: string; preferredJobs: string[]; characterNames: string[] }): Observable<{ message: string; isRegistrationComplete: boolean }> {
        return this.http.post<{ message: string; isRegistrationComplete: boolean }>(
            `${this.API_URL}/registration/complete`,
            payload,
            { withCredentials: true }
        ).pipe(
            tap(() => {
                const existingSession = this.sessionSignal();
                if (existingSession) {
                    this.sessionSignal.set({
                        ...existingSession,
                        registrationStatus: 'complete',
                        isRegistrationComplete: true,
                        canAccessApp: true,
                        name: payload.displayName
                    });
                }
                this.authStateSignal.set('authenticated');
            })
        );
    }

    public clearSession(errorMessage: string | null = null): void {
        this.sessionSignal.set(null);
        this.authStateSignal.set('unauthenticated');
        this.authErrorSignal.set(errorMessage);
    }
}
