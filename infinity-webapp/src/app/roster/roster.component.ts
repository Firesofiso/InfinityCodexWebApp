import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ListAction, ListColumn, MasterListComponent } from '../components/master-list/master-list.component';
import { RosterMember, RosterRow, UserService } from '../../services/user.service';

@Component({
  selector: 'app-roster',
  standalone: true,
  imports: [CommonModule, MasterListComponent],
  templateUrl: './roster.component.html',
  styles: [':host { display: block; }']
})
export class RosterComponent implements OnInit {
  private readonly userService = inject(UserService);

  protected readonly isLoading = signal(true);
  protected readonly pageError = signal<string | null>(null);
  protected readonly rows = signal<RosterRow[]>([]);

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
