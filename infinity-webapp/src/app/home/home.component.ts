import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'app-home',
    imports: [],
    templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
    private authService = inject(AuthService);
    private router = inject(Router);
    public readonly authState = this.authService.authState;
    public readonly authError = this.authService.authError;

    public ngOnInit(): void {
        this.checkAuth();
    }

    public checkAuth(): void {
        if (this.authService.hasAuthenticatedSession()) {
            this.router.navigate(['/app']);
            return;
        }

        this.authService.refreshSession().subscribe({
            next: (response) => {
                if (response.isAuthenticated) {
                    this.router.navigate(['/app']);
                }
                console.log('Session check completed:', response);
            },
            error: (error) => {
                console.error('Session check failed:', error);
            }
        });
    }

    public discordLogin() {
        window.location.assign(this.authService.getDiscordLoginUrl(window.location.href));
    }
}
