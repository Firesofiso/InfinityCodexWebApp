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
  memberId: number;
  characterId: number;
  characterName: string;
  discordAlias: string;
  preferredJobs: string;
  sky: boolean;
  sea: boolean;
  limbus: boolean;
  dkpTotal: number | null;
}

export interface AccessRoleDefinition {
  role: string;
  permissions: string[];
}

export interface AccessUserSummary {
  id: number;
  displayName: string;
  discordId: string;
  role: string;
  isActive: boolean;
  isRegistrationComplete: boolean;
}

export interface AccessOverviewResponse {
  permissions: string[];
  roles: AccessRoleDefinition[];
  users: AccessUserSummary[];
}

export interface UpdateUserRoleResponse {
  id: number;
  displayName: string;
  role: string;
  permissions: string[];
}

export interface GenerateFakePlayersRequest {
  count: number;
  minCharactersPerUser: number;
  maxCharactersPerUser: number;
  ensureAllRolesRepresented: boolean;
}

export interface FakeUserSummary {
  id: number;
  displayName: string;
  discordId: string;
  role: string;
  characterCount: number;
}

export interface GenerateFakePlayersResponse {
  usersCreated: number;
  charactersCreated: number;
  users: FakeUserSummary[];
  roleBreakdown: Record<string, number>;
}

export interface StartImpersonationResponse {
  userId: number;
  displayName: string;
  role: string;
}

export interface StopImpersonationResponse {
  userId: number;
  displayName: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);

  getRoster(): Observable<RosterResponse> {
    return this.http.get<RosterResponse>('/api/users/roster');
  }

  getAccessOverview(): Observable<AccessOverviewResponse> {
    return this.http.get<AccessOverviewResponse>('/api/users/access');
  }

  updateUserRole(userId: number, role: string): Observable<UpdateUserRoleResponse> {
    return this.http.put<UpdateUserRoleResponse>(`/api/users/${userId}/role`, { role });
  }

  generateFakePlayers(payload: GenerateFakePlayersRequest): Observable<GenerateFakePlayersResponse> {
    return this.http.post<GenerateFakePlayersResponse>('/api/users/dev/fake-players', payload);
  }

  startImpersonation(userId: number): Observable<StartImpersonationResponse> {
    return this.http.post<StartImpersonationResponse>('/api/users/impersonation/start', { userId });
  }

  stopImpersonation(): Observable<StopImpersonationResponse> {
    return this.http.post<StopImpersonationResponse>('/api/users/impersonation/stop', {});
  }
}
