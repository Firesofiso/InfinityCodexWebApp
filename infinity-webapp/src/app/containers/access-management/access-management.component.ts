import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '@services/auth.service';
import { AccessOverviewResponse, AccessRoleDefinition, AccessUserSummary, GenerateFakePlayersResponse, UserService } from '@services/user.service';

@Component({
  selector: 'app-access-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './access-management.component.html'
})
export class AccessManagementComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly isLoading = signal(true);
  protected readonly pageError = signal<string | null>(null);
  protected readonly permissions = signal<string[]>([]);
  protected readonly roles = signal<AccessRoleDefinition[]>([]);
  protected readonly users = signal<AccessUserSummary[]>([]);
  protected readonly roleUpdateError = signal<string | null>(null);
  protected readonly roleUpdateNotice = signal<string | null>(null);
  protected readonly updatingUserId = signal<number | null>(null);
  protected readonly fakeUserCount = signal(12);
  protected readonly fakeMinCharacters = signal(1);
  protected readonly fakeMaxCharacters = signal(3);
  protected readonly ensureAllRolesRepresented = signal(true);
  protected readonly fakeGenerationError = signal<string | null>(null);
  protected readonly fakeGenerationNotice = signal<string | null>(null);
  protected readonly isGeneratingFakePlayers = signal(false);
  protected readonly impersonationError = signal<string | null>(null);
  protected readonly impersonationNotice = signal<string | null>(null);
  protected readonly impersonatingUserId = signal<number | null>(null);

  protected readonly canManageRoles = computed(() => this.authService.canManageRoles());
  protected readonly session = this.authService.session;
  protected readonly isImpersonating = computed(() => this.session()?.isImpersonating === true);
  protected readonly effectiveUserId = computed(() => this.session()?.effectiveUserId ?? null);
  protected readonly impersonatorName = computed(() => this.session()?.impersonatorName ?? null);

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
    if (!this.canManageRoles()) {
      return;
    }

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

  protected generateFakePlayers(): void {
    if (!this.canManageRoles()) {
      return;
    }

    this.fakeGenerationError.set(null);
    this.fakeGenerationNotice.set(null);
    this.isGeneratingFakePlayers.set(true);

    this.userService.generateFakePlayers({
      count: this.fakeUserCount(),
      minCharactersPerUser: this.fakeMinCharacters(),
      maxCharactersPerUser: this.fakeMaxCharacters(),
      ensureAllRolesRepresented: this.ensureAllRolesRepresented()
    }).subscribe({
      next: (response: GenerateFakePlayersResponse) => {
        this.fakeGenerationNotice.set(`Created ${response.usersCreated} fake players and ${response.charactersCreated} characters.`);
        this.isGeneratingFakePlayers.set(false);
        this.loadAccessOverview();
      },
      error: (error: { error?: { message?: string } }) => {
        this.fakeGenerationError.set(error.error?.message ?? 'Could not generate fake players.');
        this.isGeneratingFakePlayers.set(false);
      }
    });
  }

  protected impersonateUser(user: AccessUserSummary): void {
    if (!this.canManageRoles()) {
      return;
    }

    this.impersonationError.set(null);
    this.impersonationNotice.set(null);
    this.impersonatingUserId.set(user.id);

    this.userService.startImpersonation(user.id).subscribe({
      next: () => {
        this.impersonatingUserId.set(null);
        this.authService.refreshSession(true).subscribe({
          next: () => this.router.navigate(['/app/characters'])
        });
      },
      error: (error: { error?: { message?: string } }) => {
        this.impersonationError.set(error.error?.message ?? 'Could not start impersonation.');
        this.impersonatingUserId.set(null);
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

  protected readonly roleBreakdownText = computed(() => {
    const users = this.users();
    if (users.length === 0) {
      return 'No users loaded yet.';
    }

    const breakdown = new Map<string, number>();
    for (const user of users) {
      breakdown.set(user.role, (breakdown.get(user.role) ?? 0) + 1);
    }

    return Array.from(breakdown.entries())
      .sort(([leftRole], [rightRole]) => leftRole.localeCompare(rightRole))
      .map(([role, count]) => `${role}: ${count}`)
      .join(' · ');
  });
}
