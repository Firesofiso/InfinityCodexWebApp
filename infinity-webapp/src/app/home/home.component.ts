import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-home',
    imports: [],
    templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
    private authService = inject(AuthService);
    private router = inject(Router);
    private route = inject(ActivatedRoute);
    public readonly authState = this.authService.authState;
    public readonly authError = this.authService.authError;
    public authErrorMessage: string | null = null;

    public ngOnInit(): void {
        this.route.queryParamMap.subscribe((params) => {
            this.authErrorMessage = params.get('authErrorMessage');
        });

        this.checkAuth();
    }

    public checkAuth(): void {
        if (this.authService.hasAuthenticatedSession()) {
            this.router.navigate(['/app']);
            return;
        }

        this.authService.refreshSession().subscribe({
            next: (response) => {
                const registrationStatus = response.registrationStatus ?? (response.isRegistrationComplete ? 'complete' : 'pending');
                if (response.isAuthenticated && registrationStatus === 'complete') {
                    this.router.navigate(['/app']);
                    return;
                }

                if (response.isAuthenticated && registrationStatus === 'pending') {
                    this.router.navigate(['/register']);
                }
            },
            error: () => {
                this.authErrorMessage = this.authErrorMessage ?? 'Session check failed. Please try again.';
            }
        });
    }

    public discordLogin() {
        window.location.assign(this.authService.getDiscordLoginUrl(window.location.href));
    }
}
