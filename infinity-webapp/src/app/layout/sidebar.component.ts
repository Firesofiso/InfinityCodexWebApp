import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { BookOpenIcon, HouseIcon, LogOutIcon, LucideAngularModule, OrbitIcon, ShieldIcon, UserIcon, UsersIcon } from 'lucide-angular';
import { AuthService } from '@services/auth.service';
import { UserService } from '@services/user.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [LucideAngularModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent {
    private readonly authService = inject(AuthService);
    private readonly userService = inject(UserService);
    private readonly router = inject(Router);

    readonly Home = HouseIcon;
    readonly Orbit = OrbitIcon;
    readonly User = UserIcon;
    readonly Shield = ShieldIcon;
    readonly BookOpen = BookOpenIcon;
    readonly Users = UsersIcon;
    readonly LogOut = LogOutIcon;

    readonly canManageRoles = computed(() => this.authService.canManageRoles());
    readonly isImpersonating = computed(() => this.authService.isImpersonating());
    readonly stoppingImpersonation = signal(false);
    readonly loggingOut = signal(false);

    public stopImpersonation(): void {
        if (!this.isImpersonating() || this.stoppingImpersonation()) {
            return;
        }

        this.stoppingImpersonation.set(true);

        this.userService.stopImpersonation().subscribe({
            next: () => {
                this.authService.refreshSession(true).subscribe({
                    next: () => {
                        this.stoppingImpersonation.set(false);
                        this.router.navigate(['/app/characters']);
                    },
                    error: () => {
                        this.stoppingImpersonation.set(false);
                    }
                });
            },
            error: () => {
                this.stoppingImpersonation.set(false);
            }
        });
    }

    public logout(): void {
        if (this.loggingOut()) {
            return;
        }

        this.loggingOut.set(true);

        this.authService.logout().subscribe({
            next: () => {
                this.router.navigate(['/']);
            },
            error: () => {
                this.loggingOut.set(false);
                this.router.navigate(['/']);
            }
        });
    }
}
