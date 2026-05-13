import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface CharacterWorkspaceListItem {
  characterId: number;
  name: string;
  isActive: boolean;
  portraitUrl?: string | null;
  lastSyncedAt?: string | null;
  // Not yet returned by the workspace endpoint — populated from roster when available
  dkpTotal?: number | null;
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
  sandOriaMission?: string | null;
  bastokMission?: string | null;
  windurstMission?: string | null;
  riseOfTheZilartMission?: string | null;
  chainsOfPromathiaMission?: string | null;
  epilogueMission?: string | null;
  updatedAt?: string | null;
}

export interface CharacterDynamisClears {
  dynamisSandOria: boolean;
  dynamisBastok: boolean;
  dynamisWindurst: boolean;
  dynamisJeuno: boolean;
  dynamisBeaucedine: boolean;
  dynamisXarcabard: boolean;
  dynamisValkurm: boolean;
  dynamisBuburimu: boolean;
  dynamisQufim: boolean;
  dynamisTavnazia: boolean;
}

export interface CharacterWishlistItem {
  itemId: number;
  name: string;
  requiredLevel: number;
  slot?: string | null;
  notes?: string | null;
  allowedJobs: string[];
  sources: CharacterWishlistSource[];
}

export interface CharacterWishlistContentGroup {
  id: number;
  name: string;
  tag: string;
}

export interface CharacterWishlistSource {
  id: number;
  name: string;
  tag: string;
  group: CharacterWishlistContentGroup;
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
  dynamis: CharacterDynamisClears;
  wishlist: CharacterWishlist;
}

export interface DkpTransactionEntry {
  id: number;
  sourceType: string;
  reason: string;
  amount: number;
  balanceAfter: number;
  createdAt: string;
  characterId?: number | null;
  earnEventId?: number | null;
}

export interface DkpTransactionListResponse {
  characterId: number;
  userId: number;
  entries: DkpTransactionEntry[];
}

export interface DkpAdjustmentRequest {
  amount: number;
  reason: string;
}

export interface DkpAdjustmentResponse {
  characterId: number;
  userId: number;
  newBalance: number;
  transaction: DkpTransactionEntry;
}

export interface DkpBulkEarnRequest {
  label: string;
  amount: number;
  characterIds: number[];
  occurredAt?: string | null;
}

export interface DkpBulkEarnResponse {
  eventId: number;
  label: string;
  amount: number;
  occurredAt: string;
  affectedMemberCount: number;
  transactions: DkpTransactionEntry[];
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

  public updateDynamisClears(characterId: number, clears: CharacterDynamisClears): Observable<CharacterDynamisClears> {
    return this.http.put<CharacterDynamisClears>(
      `${this.apiUrl}/${characterId}/dynamis`,
      clears,
      { withCredentials: true }
    );
  }

  public markItemObtained(characterId: number, itemId: number): Observable<void> {
    return this.http.post<void>(
      `${this.apiUrl}/${characterId}/wishlist/${itemId}/obtained`,
      {},
      { withCredentials: true }
    );
  }

  public setMainCharacter(characterId: number): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${characterId}/set-main`,
      {},
      { withCredentials: true }
    );
  }

  public adjustDkp(characterId: number, payload: DkpAdjustmentRequest): Observable<DkpAdjustmentResponse> {
    return this.http.post<DkpAdjustmentResponse>(
      `/api/dkp/characters/${characterId}/adjust`,
      payload,
      { withCredentials: true }
    );
  }

  public getDkpTransactions(characterId: number, limit: number = 50): Observable<DkpTransactionListResponse> {
    return this.http.get<DkpTransactionListResponse>(
      `/api/dkp/characters/${characterId}/transactions`,
      {
        params: { limit },
        withCredentials: true
      }
    );
  }

  public createBulkEarnEvent(payload: DkpBulkEarnRequest): Observable<DkpBulkEarnResponse> {
    return this.http.post<DkpBulkEarnResponse>(
      '/api/dkp/events/earn',
      payload,
      { withCredentials: true }
    );
  }
}