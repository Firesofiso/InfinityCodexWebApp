import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '@services/auth.service';
import { AccessOverviewResponse, AccessRoleDefinition, AccessUserSummary, UserService } from '@services/user.service';

@Component({
  selector: 'app-access-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './access-management.component.html'
})
export class AccessManagementComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly authService = inject(AuthService);

  protected readonly isLoading = signal(true);
  protected readonly pageError = signal<string | null>(null);
  protected readonly permissions = signal<string[]>([]);
  protected readonly roles = signal<AccessRoleDefinition[]>([]);
  protected readonly users = signal<AccessUserSummary[]>([]);
  protected readonly roleUpdateError = signal<string | null>(null);
  protected readonly roleUpdateNotice = signal<string | null>(null);
  protected readonly updatingUserId = signal<number | null>(null);

  protected readonly canManageRoles = computed(() => this.authService.canManageRoles());

  public ngOnInit(): void {
    this.loadAccessOverview();
  }

  protected getRolePermissions(role: string): string {
    const roleDefinition = this.roles().find((entry) => entry.role === role);
    if (!roleDefinition || roleDefinition.permissions.length === 0) {
      return 'No elevated permissions';
    }

    return roleDefinition.permissions.join(' · ');
  }

  protected updateUserRole(userId: number, role: string): void {
    this.roleUpdateError.set(null);
    this.roleUpdateNotice.set(null);
    this.updatingUserId.set(userId);

    this.userService.updateUserRole(userId, role).subscribe({
      next: (response) => {
        this.users.update((current) => current.map((user) => {
          if (user.id !== userId) {
            return user;
          }

          return {
            ...user,
            role: response.role
          };
        }));

        this.roleUpdateNotice.set(`Updated ${response.displayName} to ${response.role}.`);
        this.authService.refreshSession(true).subscribe();
        this.updatingUserId.set(null);
      },
      error: (error: { error?: { message?: string } }) => {
        this.roleUpdateError.set(error.error?.message ?? 'Could not update user role.');
        this.updatingUserId.set(null);
      }
    });
  }

  private loadAccessOverview(): void {
    this.isLoading.set(true);
    this.pageError.set(null);

    this.userService.getAccessOverview().subscribe({
      next: (response: AccessOverviewResponse) => {
        this.permissions.set(response.permissions ?? []);
        this.roles.set(response.roles ?? []);
        this.users.set(response.users ?? []);
        this.isLoading.set(false);
      },
      error: (error: { error?: { message?: string } }) => {
        this.pageError.set(error.error?.message ?? 'Unable to load roles and permissions.');
        this.isLoading.set(false);
      }
    });
  }
}
