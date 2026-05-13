import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

export interface ListColumn<T> {
  key: keyof T & string;
  header: string;
  sortable?: boolean;
  routerLink?: (row: T) => string | string[];
  externalLink?: (row: T) => string;
  format?: (value: unknown, row: T) => string;
  headerClass?: string;
  cellClass?: string;
}

export interface ListAction<T> {
  label: string;
  onClick: (row: T) => void;
  disabled?: (row: T) => boolean;
  hidden?: (row: T) => boolean;
  variant?: 'default' | 'danger';
}

@Component({
  selector: 'app-master-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './master-list.component.html',
  styles: [':host { display: block; }']
})
export class MasterListComponent<T extends object = Record<string, unknown>> implements OnChanges {
  @Input() columns: ListColumn<T>[] = [];
  @Input() data: T[] = [];
  @Input() actions: ListAction<T>[] = [];
  @Input() searchable = false;
  @Input() searchPlaceholder = 'Search...';
  @Input() searchFields: string[] | null = null;
  @Input() loading = false;
  @Input() loadingMessage = 'Loading...';
  @Input() emptyMessage = 'No results found.';

  protected readonly searchQuery = signal('');
  protected readonly sortKey = signal<string | null>(null);
  protected readonly sortDirection = signal<'asc' | 'desc'>('asc');
  private readonly _data = signal<T[]>([]);

  protected readonly displayData = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    const key = this.sortKey();
    const dir = this.sortDirection();
    let rows = this._data();

    if (query) {
      rows = rows.filter((row) => this.rowMatchesQuery(row, query));
    }

    if (key) {
      rows = [...rows].sort((a, b) => {
        const aRaw = a[key as keyof T];
        const bRaw = b[key as keyof T];
        const aNum = Number(aRaw);
        const bNum = Number(bRaw);
        if (!Number.isNaN(aNum) && !Number.isNaN(bNum)) {
          return dir === 'asc' ? aNum - bNum : bNum - aNum;
        }
        const cmp = String(aRaw ?? '').toLowerCase().localeCompare(String(bRaw ?? '').toLowerCase());
        return dir === 'asc' ? cmp : -cmp;
      });
    }

    return rows;
  });

  public ngOnChanges(): void {
    this._data.set(this.data);
  }

  protected setSearch(value: string): void {
    this.searchQuery.set(value);
  }

  protected toggleSort(column: ListColumn<T>): void {
    if (!column.sortable) {
      return;
    }

    if (this.sortKey() === column.key) {
      this.sortDirection.update((dir) => (dir === 'asc' ? 'desc' : 'asc'));
    } else {
      this.sortKey.set(column.key);
      this.sortDirection.set('asc');
    }
  }

  protected getCellValue(row: T, column: ListColumn<T>): string {
    const raw = row[column.key as keyof T];
    if (column.format) {
      return column.format(raw, row);
    }
    return raw != null ? String(raw) : '';
  }

  protected getRouterLink(row: T, column: ListColumn<T>): string | string[] | null {
    return column.routerLink ? column.routerLink(row) : null;
  }

  protected getExternalLink(row: T, column: ListColumn<T>): string | null {
    return column.externalLink ? column.externalLink(row) : null;
  }

  protected getVisibleActions(row: T): ListAction<T>[] {
    return this.actions.filter((action) => !action.hidden?.(row));
  }

  protected isActionDisabled(action: ListAction<T>, row: T): boolean {
    return action.disabled?.(row) ?? false;
  }

  protected trackByIndex(index: number): number {
    return index;
  }

  private rowMatchesQuery(row: T, query: string): boolean {
    const fields = this.searchFields ?? this.columns.map((col) => col.key);
    return fields.some((field) => {
      const val = row[field as keyof T];
      return val != null && String(val).toLowerCase().includes(query);
    });
  }
}
