import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface CatalogJobOption {
  code: string;
  label: string;
}

export interface ContentSourceReference {
  id: number;
  name: string;
  tag: string;
}

export interface ContentSourceOption extends ContentSourceReference {
  isActive: boolean;
  notes: string;
}

export interface CatalogItemSummary {
  id: number;
  name: string;
  requiredLevel: number;
  slot?: string | null;
  notes?: string | null;
  isActive: boolean;
  updatedAt: string;
  allowedJobs: string[];
  sources: ContentSourceReference[];
}

export interface CatalogItemDetail extends CatalogItemSummary {}

export interface CatalogItemListResponse {
  items: CatalogItemSummary[];
}

export interface CatalogReferenceDataResponse {
  jobs: CatalogJobOption[];
  sources: ContentSourceOption[];
}

export interface UpsertCatalogItemRequest {
  name: string;
  requiredLevel: number;
  slot?: string | null;
  notes?: string | null;
  allowedJobs: string[];
  sourceIds: number[];
}

@Injectable({ providedIn: 'root' })
export class ItemCatalogService {
  private readonly apiUrl = '/api/items';
  private readonly http = inject(HttpClient);

  public getItems(filters?: { search?: string; isActive?: boolean | null; slot?: string; sourceIds?: number[] }): Observable<CatalogItemListResponse> {
    let params = new HttpParams();

    if (filters?.search?.trim()) {
      params = params.set('search', filters.search.trim());
    }

    if (filters?.isActive !== undefined && filters.isActive !== null) {
      params = params.set('isActive', String(filters.isActive));
    }

    if (filters?.slot?.trim()) {
      params = params.set('slot', filters.slot.trim());
    }

    for (const sourceId of filters?.sourceIds ?? []) {
      params = params.append('sourceIds', String(sourceId));
    }

    return this.http.get<CatalogItemListResponse>(this.apiUrl, {
      params,
      withCredentials: true
    });
  }

  public getItem(itemId: number): Observable<CatalogItemDetail> {
    return this.http.get<CatalogItemDetail>(`${this.apiUrl}/${itemId}`, {
      withCredentials: true
    });
  }

  public getReferenceData(): Observable<CatalogReferenceDataResponse> {
    return this.http.get<CatalogReferenceDataResponse>(`${this.apiUrl}/reference-data`, {
      withCredentials: true
    });
  }

  public createItem(payload: UpsertCatalogItemRequest): Observable<CatalogItemDetail> {
    return this.http.post<CatalogItemDetail>(this.apiUrl, payload, {
      withCredentials: true
    });
  }

  public updateItem(itemId: number, payload: UpsertCatalogItemRequest): Observable<CatalogItemDetail> {
    return this.http.put<CatalogItemDetail>(`${this.apiUrl}/${itemId}`, payload, {
      withCredentials: true
    });
  }

  public setArchived(itemId: number, archived: boolean): Observable<CatalogItemDetail> {
    return this.http.post<CatalogItemDetail>(`${this.apiUrl}/${itemId}/archive`, { archived }, {
      withCredentials: true
    });
  }
}