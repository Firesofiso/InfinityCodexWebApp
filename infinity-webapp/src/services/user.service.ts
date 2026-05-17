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
}
