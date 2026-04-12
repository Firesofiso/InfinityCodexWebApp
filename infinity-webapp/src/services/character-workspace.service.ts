import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface CharacterWorkspaceListItem {
  characterId: number;
  name: string;
  isActive: boolean;
  portraitUrl?: string | null;
  lastSyncedAt?: string | null;
}

export interface CharacterWorkspaceListResponse {
  characters: CharacterWorkspaceListItem[];
  mainCharacterId?: number | null;
}

export interface HorizonJobLevel {
  jobCode: string;
  level: number;
}

export interface HorizonCharacterDetail {
  name: string;
  nation?: number | null;
  rank?: string | null;
  mentor: boolean;
  settings?: number | null;
  jobString?: string | null;
  avatar?: string | null;
  portraitUrl?: string | null;
  seacomType?: string | null;
  seacomMessage?: string | null;
  online: boolean;
  jobs: HorizonJobLevel[];
}

export interface CharacterMissionProgress {
  sanDOriaMission?: string | null;
  bastokMission?: string | null;
  windurstMission?: string | null;
  riseOfTheZilartMission?: string | null;
  chainsOfPromathiaMission?: string | null;
  updatedAt?: string | null;
}

export interface CharacterWishlistItem {
  itemId: number;
  name: string;
  requiredLevel: number;
  slot?: string | null;
  notes?: string | null;
  allowedJobs: string[];
  sources: string[];
}

export interface CharacterWishlistAssignment {
  itemId: number;
  characterIds: number[];
}

export interface CharacterWishlist {
  selectedItemIds: number[];
  availableItems: CharacterWishlistItem[];
  assignments: CharacterWishlistAssignment[];
}

export interface CharacterWorkspaceDetailResponse {
  character: CharacterWorkspaceListItem;
  horizon?: HorizonCharacterDetail | null;
  horizonError?: string | null;
  missions: CharacterMissionProgress;
  wishlist: CharacterWishlist;
}

@Injectable({ providedIn: 'root' })
export class CharacterWorkspaceService {
  private readonly apiUrl = '/api/characters/workspace';
  private readonly http = inject(HttpClient);

  public getCharacters(): Observable<CharacterWorkspaceListResponse> {
    return this.http.get<CharacterWorkspaceListResponse>(this.apiUrl, {
      withCredentials: true
    });
  }

  public getCharacter(characterId: number): Observable<CharacterWorkspaceDetailResponse> {
    return this.http.get<CharacterWorkspaceDetailResponse>(`${this.apiUrl}/${characterId}`, {
      withCredentials: true
    });
  }

  public updateMissions(characterId: number, payload: CharacterMissionProgress): Observable<CharacterMissionProgress> {
    return this.http.put<CharacterMissionProgress>(`${this.apiUrl}/${characterId}/missions`, payload, {
      withCredentials: true
    });
  }

  public updateWishlist(
    characterId: number,
    assignments: CharacterWishlistAssignment[]
  ): Observable<{ selectedItemIds: number[]; assignments: CharacterWishlistAssignment[] }> {
    return this.http.put<{ selectedItemIds: number[]; assignments: CharacterWishlistAssignment[] }>(
      `${this.apiUrl}/${characterId}/wishlist`,
      { assignments },
      { withCredentials: true }
    );
  }
}