import { Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { BookOpenIcon, HouseIcon, LogOutIcon, LucideAngularModule, OrbitIcon, ShieldIcon, UserIcon, UsersIcon } from 'lucide-angular';
import { AuthService } from '@services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [LucideAngularModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent {
    private readonly authService = inject(AuthService);
    private readonly router = inject(Router);

    readonly Home = HouseIcon;
    readonly Orbit = OrbitIcon;
    readonly User = UserIcon;
    readonly Shield = ShieldIcon;
    readonly BookOpen = BookOpenIcon;
    readonly Users = UsersIcon;
    readonly LogOut = LogOutIcon;

    readonly canManageRoles = computed(() => this.authService.canManageRoles());
    readonly loggingOut = signal(false);

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
