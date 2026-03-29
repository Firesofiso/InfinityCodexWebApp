import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, RegistrationContextResponse } from '../../services/auth.service';

@Component({
    selector: 'app-register',
    imports: [CommonModule, FormsModule],
    templateUrl: './register.component.html'
})
export class RegisterComponent implements OnInit {
    private authService = inject(AuthService);
    private router = inject(Router);

    public displayName = '';
    public preferredJobsRaw = '';
    public loading = signal(true);
    public submitting = signal(false);
    public errorMessage = signal<string | null>(null);

    public ngOnInit(): void {
        this.authService.getRegistrationContext().subscribe({
            next: (response: RegistrationContextResponse) => {
                if (response.isRegistrationComplete) {
                    this.router.navigate(['/app']);
                    return;
                }

                this.displayName = response.displayName ?? '';
                this.preferredJobsRaw = response.preferredJobs.join(', ');
                this.loading.set(false);
            },
            error: () => {
                this.errorMessage.set('Could not load your registration context. Please retry from the home page.');
                this.loading.set(false);
            }
        });
    }

    public submit(): void {
        if (this.submitting()) {
            return;
        }

        const normalizedDisplayName = this.displayName.trim();
        if (!normalizedDisplayName) {
            this.errorMessage.set('Display name is required.');
            return;
        }

        this.errorMessage.set(null);
        this.submitting.set(true);

        this.authService.completeRegistration({
            displayName: normalizedDisplayName,
            preferredJobs: this.parsePreferredJobs(this.preferredJobsRaw)
        }).subscribe({
            next: () => {
                this.router.navigate(['/app']);
            },
            error: () => {
                this.errorMessage.set('Registration could not be completed. Please try again.');
                this.submitting.set(false);
            }
        });
    }

    private parsePreferredJobs(raw: string): string[] {
        if (!raw.trim()) {
            return [];
        }

        return raw
            .split(',')
            .map((part) => part.trim())
            .filter((part, index, arr) => !!part && arr.findIndex((entry) => entry.toLowerCase() === part.toLowerCase()) === index)
            .slice(0, 20);
    }
}
