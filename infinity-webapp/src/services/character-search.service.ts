import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface CharacterSearchResult {
  characterId: string;
  name: string;
  currentJob: string;
  jobString?: string | null;
  portraitUrl?: string | null;
}

export interface CharacterSearchResponse {
  query: string;
  count: number;
  results: CharacterSearchResult[];
}

@Injectable({ providedIn: 'root' })
export class CharacterSearchService {
  private readonly apiUrl = '/api/characters';
  private readonly http = inject(HttpClient);

  public searchCharacters(search: string): Observable<CharacterSearchResponse> {
    return this.http.get<CharacterSearchResponse>(`${this.apiUrl}/search`, {
      params: { search },
      withCredentials: true
    });
  }
}
