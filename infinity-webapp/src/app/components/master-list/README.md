# MasterListComponent

Reusable data table with search, sorting, links, and per-row actions.

## Inputs

| Input | Type | Default | Description |
|---|---|---|---|
| `columns` | `ListColumn[]` | `[]` | Column definitions |
| `data` | `unknown[]` | `[]` | Row data |
| `actions` | `ListAction[]` | `[]` | Per-row action buttons |
| `searchable` | `boolean` | `false` | Show search input |
| `searchPlaceholder` | `string` | `'Search...'` | Search input placeholder |
| `searchFields` | `string[] \| null` | `null` | Fields to search (null = all columns) |
| `loading` | `boolean` | `false` | Show loading state |
| `loadingMessage` | `string` | `'Loading...'` | Loading state text |
| `emptyMessage` | `string` | `'No results found.'` | Empty state text |

## ListColumn

```ts
{
  key: string;                                              // property key on the row object
  header: string;                                           // column header text
  sortable?: boolean;                                       // enable click-to-sort
  format?: (value: unknown, row: Record<string, unknown>) => string;  // display formatter
  cellClass?: string;                                       // extra CSS classes on <td>
  routerLink?: (row: Record<string, unknown>) => string | string[];   // internal router link
  externalLink?: (row: Record<string, unknown>) => string; // external <a href> link
}
```

`routerLink` and `externalLink` are mutually exclusive. Both render the cell value as a cyan anchor.

## ListAction

```ts
{
  label: string;
  onClick: (row: Record<string, unknown>) => void;
  disabled?: (row: Record<string, unknown>) => boolean;
  hidden?: (row: Record<string, unknown>) => boolean;
  variant?: 'default' | 'danger';  // default = subtle white, danger = red
}
```

## Example

```ts
columns: ListColumn[] = [
  { key: 'name', header: 'Member', sortable: true, routerLink: (r) => ['/members', r['id']] },
  { key: 'rank', header: 'Rank', sortable: true },
  { key: 'dkp',  header: 'DKP',  sortable: true, format: (v) => Number(v).toLocaleString() },
];

actions: ListAction[] = [
  { label: 'Edit',   onClick: (r) => this.edit(r) },
  { label: 'Remove', onClick: (r) => this.remove(r), variant: 'danger',
    hidden: (r) => !this.canEdit() },
];
```

```html
<app-master-list
  [columns]="columns"
  [data]="members()"
  [actions]="actions"
  [searchable]="true"
  searchPlaceholder="Search members..."
  [loading]="isLoading()"
/>
```
