import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { FullCalendarModule } from '@fullcalendar/angular';
import { CalendarOptions } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';

@Component({
    selector: 'app-dashboard',
    imports: [FullCalendarModule],
    templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
    private authService = inject(AuthService);
    private router = inject(Router);

    public readonly authError = this.authService.authError;
    public readonly authState = this.authService.authState;
    public readonly session = this.authService.session;

    public calendarOptions: CalendarOptions = {
        initialView: 'dayGridMonth',
        plugins: [dayGridPlugin]
    };

    public ngOnInit(): void {
        if (!this.authService.hasAuthenticatedSession()) {
            this.loadSession();
        }
    }

    public refreshSession(): void {
        this.loadSession(true);
    }

    private loadSession(force: boolean = false): void {
        this.authService.refreshSession(force).subscribe({
            next: (response) => {
                const registrationStatus = response.registrationStatus ?? (response.isRegistrationComplete ? 'complete' : 'pending');
                if (!response.isAuthenticated) {
                    this.router.navigate(['/']);
                    return;
                }

                if (registrationStatus !== 'complete') {
                    this.router.navigate(['/register']);
                }
            },
            error: () => {
                this.router.navigate(['/']);
            }
        });
    }

    public logout(): void {
        this.authService.logout().subscribe({
            next: () => {
                this.router.navigate(['/']);
            },
            error: () => {
                this.router.navigate(['/']);
            }
        });
    }
}
