import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ListAction, ListColumn, MasterListComponent } from '../../components/master-list/master-list.component';
import { RosterMember, RosterRow, UserService } from '@services/user.service';
import { CharacterWorkspaceService } from '@services/character-workspace.service';
import { AuthService } from '@services/auth.service';

@Component({
  selector: 'app-roster',
  standalone: true,
  imports: [CommonModule, FormsModule, MasterListComponent],
  templateUrl: './roster.component.html',
  styles: [':host { display: block; }']
})
export class RosterComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly characterWorkspaceService = inject(CharacterWorkspaceService);
  private readonly authService = inject(AuthService);

  protected readonly isLoading = signal(true);
  protected readonly pageError = signal<string | null>(null);
  protected readonly rows = signal<RosterRow[]>([]);
  protected readonly showBulkEarnModal = signal(false);
  protected readonly bulkEarnLabel = signal('');
  protected readonly bulkEarnAmount = signal<number | null>(null);
  protected readonly bulkEarnSearch = signal('');
  protected readonly bulkEarnSelectedCharacterIds = signal<number[]>([]);
  protected readonly bulkEarnSaving = signal(false);
  protected readonly bulkEarnError = signal<string | null>(null);
  protected readonly bulkEarnNotice = signal<string | null>(null);

  protected readonly canManageDkp = computed(() => {
    return this.authService.canManageDkp();
  });

  protected readonly filteredBulkEarnRows = computed(() => {
    const query = this.bulkEarnSearch().trim().toLowerCase();
    const allRows = this.rows();

    if (!query) {
      return allRows;
    }

    return allRows.filter((row) => {
      const searchableValues = [
        row.characterName,
        row.discordAlias,
        row.preferredJobs
      ];

      return searchableValues.some((value) => value.toLowerCase().includes(query));
    });
  });

  protected readonly columns: ListColumn<RosterRow>[] = [
    {
      key: 'characterName',
      header: 'Character',
      sortable: true,
      routerLink: (row) => ['/app/roster', String(row.characterId)]
    },
    {
      key: 'dkpTotal',
      header: 'DKP',
      sortable: true,
      format: (value) => value != null ? String(value) : '—'
    },
    {
      key: 'discordAlias',
      header: 'Discord'
    },
    {
      key: 'preferredJobs',
      header: 'Preferred Jobs'
    },
    {
      key: 'sky',
      header: 'Sky',
      format: (value) => value ? '✓' : '—',
      headerClass: 'text-center',
      cellClass: 'text-center'
    },
    {
      key: 'sea',
      header: 'Sea',
      format: (value) => value ? '✓' : '—',
      headerClass: 'text-center',
      cellClass: 'text-center'
    },
    {
      key: 'limbus',
      header: 'Limbus',
      format: (value) => value ? '✓' : '—',
      headerClass: 'text-center',
      cellClass: 'text-center'
    }
  ];

  protected readonly actions: ListAction<RosterRow>[] = [];

  public ngOnInit(): void {
    this.loadRoster();
  }

  protected openBulkEarnModal(): void {
    this.bulkEarnError.set(null);
    this.bulkEarnNotice.set(null);
    this.bulkEarnSearch.set('');
    this.bulkEarnSelectedCharacterIds.set([]);
    this.showBulkEarnModal.set(true);
  }

  protected closeBulkEarnModal(): void {
    this.showBulkEarnModal.set(false);
  }

  protected isBulkEarnCharacterSelected(characterId: number): boolean {
    return this.bulkEarnSelectedCharacterIds().includes(characterId);
  }

  protected toggleBulkEarnCharacter(characterId: number): void {
    this.bulkEarnSelectedCharacterIds.update((current) => {
      if (current.includes(characterId)) {
        return current.filter((value) => value !== characterId);
      }

      return [...current, characterId].sort((left, right) => left - right);
    });
  }

  protected submitBulkEarn(): void {
    const label = this.bulkEarnLabel().trim();
    const amount = this.bulkEarnAmount();
    const characterIds = this.bulkEarnSelectedCharacterIds();

    if (!label) {
      this.bulkEarnError.set('An event label is required.');
      return;
    }

    if (amount === null || amount <= 0) {
      this.bulkEarnError.set('Earn amount must be greater than zero.');
      return;
    }

    if (characterIds.length === 0) {
      this.bulkEarnError.set('Select at least one player.');
      return;
    }

    this.bulkEarnError.set(null);
    this.bulkEarnNotice.set(null);
    this.bulkEarnSaving.set(true);

    this.characterWorkspaceService.createBulkEarnEvent({
      label,
      amount,
      characterIds
    }).subscribe({
      next: (response) => {
        this.bulkEarnNotice.set(`Applied ${response.amount} DKP to ${response.affectedMemberCount} player(s).`);
        this.bulkEarnSaving.set(false);
        this.bulkEarnLabel.set('');
        this.bulkEarnAmount.set(null);
        this.bulkEarnSelectedCharacterIds.set([]);
        this.loadRoster();
      },
      error: (error: { error?: { message?: string } }) => {
        this.bulkEarnError.set(error.error?.message ?? 'Bulk earn event could not be saved.');
        this.bulkEarnSaving.set(false);
      }
    });
  }

  private loadRoster(): void {
    this.isLoading.set(true);
    this.pageError.set(null);

    this.userService.getRoster().subscribe({
      next: (response) => {
        this.rows.set(response.members.map((member) => this.toRow(member)));
        this.isLoading.set(false);
      },
      error: () => {
        this.pageError.set('Unable to load the member roster right now.');
        this.isLoading.set(false);
      }
    });
  }

  private toRow(member: RosterMember): RosterRow {
    const access = member.contentAccess;
    return {
      characterId: member.characterId,
      characterName: member.characterName,
      discordAlias: member.discordAlias,
      preferredJobs: member.preferredJobs.join(' · ') || '—',
      sky: access?.sky ?? false,
      sea: access?.sea ?? false,
      limbus: access?.limbus ?? false,
      dkpTotal: member.dkpTotal
    };
  }
}
