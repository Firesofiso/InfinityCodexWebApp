import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Subscription, catchError, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';
import { CharacterSearchResult, CharacterSearchService } from '@services/character-search.service';

@Component({
  selector: 'app-character-search',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './character-search.component.html',
  styleUrl: './character-search.component.css'
})
export class CharacterSearchComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly characterSearchService = inject(CharacterSearchService);
  private readonly subscriptions = new Subscription();

  protected readonly isLoading = signal(false);
  protected readonly query = signal('');
  protected readonly error = signal<string | null>(null);
  protected readonly results = signal<CharacterSearchResult[]>([]);
  protected readonly minimumSearchLength = 2;

  public ngOnInit(): void {
    const querySubscription = this.route.queryParamMap.pipe(
      map((params) => (params.get('search') ?? '').trim()),
      distinctUntilChanged(),
      tap((nextQuery) => {
        this.query.set(nextQuery);
        this.error.set(null);

        if (nextQuery.length < this.minimumSearchLength) {
          this.results.set([]);
          this.isLoading.set(false);
        }
      }),
      switchMap((nextQuery) => {
        if (nextQuery.length < this.minimumSearchLength) {
          return of<{ results: CharacterSearchResult[] } | null>(null);
        }

        this.isLoading.set(true);

        return this.characterSearchService.searchCharacters(nextQuery).pipe(
          map((response) => ({ results: response.results ?? [] })),
          catchError((error: { error?: { message?: string } }) => {
            const message = error.error?.message ?? 'Unable to search characters right now.';
            this.error.set(message);
            return of({ results: [] });
          })
        );
      })
    ).subscribe((response) => {
      if (response !== null) {
        this.results.set(response.results);
      }

      this.isLoading.set(false);
    });

    this.subscriptions.add(querySubscription);
  }

  public ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  protected getInitial(name: string): string {
    return name.trim().charAt(0).toUpperCase();
  }
}
