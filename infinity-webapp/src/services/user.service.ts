import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface ContentAccess {
  sky: boolean;
  sea: boolean;
  limbus: boolean;
  dynamisCities: boolean;
  dynamisIcelands: boolean;
  dynamisDreamlands: boolean;
  dynamisTavnazia: boolean;
}

export interface RosterMember {
  memberId: number;
  characterId: number;
  characterName: string;
  discordAlias: string;
  preferredJobs: string[];
  level75Jobs: string[];
  dkpTotal: number | null;
  contentAccess: ContentAccess | null;
}

export interface RosterResponse {
  members: RosterMember[];
}

export interface RosterRow {
  characterId: number;
  characterName: string;
  discordAlias: string;
  preferredJobs: string;
  sky: boolean;
  sea: boolean;
  limbus: boolean;
  dkpTotal: number | null;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  getRoster(): Observable<RosterResponse> {
    return this.http.get<RosterResponse>('/api/users/roster');
  }
}
