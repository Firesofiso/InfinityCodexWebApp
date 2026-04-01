import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  CharacterMissionProgress,
  CharacterWishlistItem,
  CharacterWorkspaceDetailResponse,
  CharacterWorkspaceListItem,
  CharacterWorkspaceService
} from '../../services/character-workspace.service';

type SaveState = 'idle' | 'dirty' | 'saving' | 'saved' | 'error';

@Component({
  selector: 'app-character-workspace',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './character-workspace.component.html',
  styleUrl: './character-workspace.component.css'
})
export class CharacterWorkspaceComponent implements OnInit, OnDestroy {
  private readonly characterWorkspaceService = inject(CharacterWorkspaceService);
  private missionSaveTimer: ReturnType<typeof setTimeout> | null = null;
  private wishlistSaveTimer: ReturnType<typeof setTimeout> | null = null;
  private isSavingMission = false;
  private isSavingWishlist = false;
  private pendingMissionSave = false;
  private pendingWishlistSave = false;
  private lastSavedMissionSignature = '';
  private lastSavedWishlistSignature = '';

  protected readonly isLoadingCharacters = signal(true);
  protected readonly isLoadingDetail = signal(false);
  protected readonly pageError = signal<string | null>(null);
  protected readonly detailError = signal<string | null>(null);
  protected readonly characters = signal<CharacterWorkspaceListItem[]>([]);
  protected readonly selectedCharacterId = signal<number | null>(null);
  protected readonly detail = signal<CharacterWorkspaceDetailResponse | null>(null);
  protected readonly missionDraft = signal<CharacterMissionProgress>(this.createEmptyMissionProgress());
  protected readonly selectedWishlistItemIds = signal<number[]>([]);
  protected readonly missionSaveState = signal<SaveState>('idle');
  protected readonly wishlistSaveState = signal<SaveState>('idle');
  protected readonly missionError = signal<string | null>(null);
  protected readonly wishlistError = signal<string | null>(null);

  protected readonly wishlistFilter = signal('');

  protected readonly filteredWishlistItems = computed(() => {
    const detail = this.detail();
    const query = this.wishlistFilter().trim().toLowerCase();

    if (!detail) {
      return [] as CharacterWishlistItem[];
    }

    if (!query) {
      return detail.wishlist.availableItems;
    }

    return detail.wishlist.availableItems.filter((item) => {
      const haystacks = [
        item.name,
        item.slot ?? '',
        item.notes ?? '',
        item.allowedJobs.join(' '),
        item.sources.join(' ')
      ];

      return haystacks.some((value) => value.toLowerCase().includes(query));
    });
  });

  public ngOnInit(): void {
    this.loadCharacters();
  }

  public ngOnDestroy(): void {
    this.clearMissionTimer();
    this.clearWishlistTimer();
  }

  protected selectCharacter(characterId: number): void {
    if (this.selectedCharacterId() === characterId) {
      return;
    }

    this.selectedCharacterId.set(characterId);
    this.detail.set(null);
    this.detailError.set(null);
    this.missionError.set(null);
    this.wishlistError.set(null);
    this.missionSaveState.set('idle');
    this.wishlistSaveState.set('idle');
    this.pendingMissionSave = false;
    this.pendingWishlistSave = false;
    this.clearMissionTimer();
    this.clearWishlistTimer();
    this.loadCharacterDetail(characterId);
  }

  protected updateMission(field: keyof CharacterMissionProgress, value: string): void {
    this.missionDraft.update((current) => ({
      ...current,
      [field]: value
    }));

    this.missionSaveState.set('dirty');
    this.missionError.set(null);
    this.queueMissionSave();
  }

  protected toggleWishlistItem(itemId: number): void {
    const current = this.selectedWishlistItemIds();
    const next = current.includes(itemId)
      ? current.filter((value) => value !== itemId)
      : [...current, itemId].sort((left, right) => left - right);

    this.selectedWishlistItemIds.set(next);
    this.wishlistSaveState.set('dirty');
    this.wishlistError.set(null);
    this.queueWishlistSave();
  }

  protected isSelectedWishlistItem(itemId: number): boolean {
    return this.selectedWishlistItemIds().includes(itemId);
  }

  protected getInitial(name: string): string {
    return name.trim().charAt(0).toUpperCase();
  }

  protected getNationLabel(nation?: number | null): string {
    switch (nation) {
      case 0:
        return "San d'Oria";
      case 1:
        return 'Bastok';
      case 2:
        return 'Windurst';
      default:
        return 'Unknown';
    }
  }

  protected getSaveStateMessage(state: SaveState, updatedAt?: string | null): string {
    if (state === 'saving') {
      return 'Saving...';
    }

    if (state === 'dirty') {
      return 'Unsaved changes';
    }

    if (state === 'saved') {
      if (updatedAt) {
        return `Saved ${new Date(updatedAt).toLocaleTimeString([], { hour: 'numeric', minute: '2-digit' })}`;
      }

      return 'Saved';
    }

    if (state === 'error') {
      return 'Save failed';
    }

    return 'Up to date';
  }

  private loadCharacters(): void {
    this.isLoadingCharacters.set(true);
    this.pageError.set(null);

    this.characterWorkspaceService.getCharacters().subscribe({
      next: (response) => {
        const characters = response.characters ?? [];
        this.characters.set(characters);
        this.isLoadingCharacters.set(false);

        if (characters.length === 0) {
          this.selectedCharacterId.set(null);
          this.detail.set(null);
          return;
        }

        this.selectCharacter(characters[0].characterId);
      },
      error: (error: { error?: { message?: string } }) => {
        this.pageError.set(error.error?.message ?? 'Unable to load your characters.');
        this.isLoadingCharacters.set(false);
      }
    });
  }

  private loadCharacterDetail(characterId: number): void {
    this.isLoadingDetail.set(true);

    this.characterWorkspaceService.getCharacter(characterId).subscribe({
      next: (response) => {
        if (this.selectedCharacterId() !== characterId) {
          return;
        }

        this.detail.set(response);
        this.missionDraft.set(this.normalizeMissionProgress(response.missions));
        this.selectedWishlistItemIds.set([...(response.wishlist.selectedItemIds ?? [])].sort((left, right) => left - right));
        this.lastSavedMissionSignature = this.serializeMission(this.normalizeMissionProgress(response.missions));
        this.lastSavedWishlistSignature = this.serializeWishlist(response.wishlist.selectedItemIds ?? []);
        this.missionSaveState.set('idle');
        this.wishlistSaveState.set('idle');
        this.detailError.set(null);
        this.wishlistFilter.set('');
        this.isLoadingDetail.set(false);
      },
      error: (error: { error?: { message?: string } }) => {
        if (this.selectedCharacterId() !== characterId) {
          return;
        }

        this.detailError.set(error.error?.message ?? 'Unable to load this character workspace.');
        this.isLoadingDetail.set(false);
      }
    });
  }

  private queueMissionSave(): void {
    this.clearMissionTimer();
    this.missionSaveTimer = setTimeout(() => {
      if (this.isSavingMission) {
        this.pendingMissionSave = true;
        return;
      }

      void this.commitMissionSave();
    }, 700);
  }

  private queueWishlistSave(): void {
    this.clearWishlistTimer();
    this.wishlistSaveTimer = setTimeout(() => {
      if (this.isSavingWishlist) {
        this.pendingWishlistSave = true;
        return;
      }

      void this.commitWishlistSave();
    }, 700);
  }

  private async commitMissionSave(): Promise<void> {
    const characterId = this.selectedCharacterId();
    if (characterId === null) {
      return;
    }

    const payload = this.normalizeMissionProgress(this.missionDraft());
    const signature = this.serializeMission(payload);
    if (signature === this.lastSavedMissionSignature) {
      this.missionSaveState.set('saved');
      return;
    }

    this.isSavingMission = true;
    this.missionSaveState.set('saving');
    this.missionError.set(null);

    this.characterWorkspaceService.updateMissions(characterId, payload).subscribe({
      next: (response) => {
        if (this.selectedCharacterId() !== characterId) {
          return;
        }

        const normalizedResponse = this.normalizeMissionProgress(response);
        this.missionDraft.set(normalizedResponse);
        this.detail.update((current) => current ? { ...current, missions: response } : current);
        this.lastSavedMissionSignature = this.serializeMission(normalizedResponse);
        this.missionSaveState.set('saved');
      },
      error: (error: { error?: { message?: string } }) => {
        if (this.selectedCharacterId() !== characterId) {
          return;
        }

        this.missionError.set(error.error?.message ?? 'Mission progress could not be saved.');
        this.missionSaveState.set('error');
      },
      complete: () => {
        this.isSavingMission = false;
        if (this.pendingMissionSave || this.serializeMission(this.normalizeMissionProgress(this.missionDraft())) !== this.lastSavedMissionSignature) {
          this.pendingMissionSave = false;
          this.queueMissionSave();
        }
      }
    });
  }

  private async commitWishlistSave(): Promise<void> {
    const characterId = this.selectedCharacterId();
    if (characterId === null) {
      return;
    }

    const selectedItemIds = [...this.selectedWishlistItemIds()].sort((left, right) => left - right);
    const signature = this.serializeWishlist(selectedItemIds);
    if (signature === this.lastSavedWishlistSignature) {
      this.wishlistSaveState.set('saved');
      return;
    }

    this.isSavingWishlist = true;
    this.wishlistSaveState.set('saving');
    this.wishlistError.set(null);

    this.characterWorkspaceService.updateWishlist(characterId, selectedItemIds).subscribe({
      next: (response) => {
        if (this.selectedCharacterId() !== characterId) {
          return;
        }

        const normalizedIds = [...(response.selectedItemIds ?? [])].sort((left, right) => left - right);
        this.selectedWishlistItemIds.set(normalizedIds);
        this.detail.update((current) => current
          ? { ...current, wishlist: { ...current.wishlist, selectedItemIds: normalizedIds } }
          : current);
        this.lastSavedWishlistSignature = this.serializeWishlist(normalizedIds);
        this.wishlistSaveState.set('saved');
      },
      error: (error: { error?: { message?: string } }) => {
        if (this.selectedCharacterId() !== characterId) {
          return;
        }

        this.wishlistError.set(error.error?.message ?? 'Wishlist changes could not be saved.');
        this.wishlistSaveState.set('error');
      },
      complete: () => {
        this.isSavingWishlist = false;
        if (this.pendingWishlistSave || this.serializeWishlist(this.selectedWishlistItemIds()) !== this.lastSavedWishlistSignature) {
          this.pendingWishlistSave = false;
          this.queueWishlistSave();
        }
      }
    });
  }

  private clearMissionTimer(): void {
    if (this.missionSaveTimer) {
      clearTimeout(this.missionSaveTimer);
      this.missionSaveTimer = null;
    }
  }

  private clearWishlistTimer(): void {
    if (this.wishlistSaveTimer) {
      clearTimeout(this.wishlistSaveTimer);
      this.wishlistSaveTimer = null;
    }
  }

  private normalizeMissionProgress(progress: CharacterMissionProgress): CharacterMissionProgress {
    return {
      sanDOriaMission: progress.sanDOriaMission ?? '',
      bastokMission: progress.bastokMission ?? '',
      windurstMission: progress.windurstMission ?? '',
      riseOfTheZilartMission: progress.riseOfTheZilartMission ?? '',
      chainsOfPromathiaMission: progress.chainsOfPromathiaMission ?? '',
      updatedAt: progress.updatedAt ?? null
    };
  }

  private createEmptyMissionProgress(): CharacterMissionProgress {
    return {
      sanDOriaMission: '',
      bastokMission: '',
      windurstMission: '',
      riseOfTheZilartMission: '',
      chainsOfPromathiaMission: '',
      updatedAt: null
    };
  }

  private serializeMission(progress: CharacterMissionProgress): string {
    return JSON.stringify({
      sanDOriaMission: progress.sanDOriaMission ?? '',
      bastokMission: progress.bastokMission ?? '',
      windurstMission: progress.windurstMission ?? '',
      riseOfTheZilartMission: progress.riseOfTheZilartMission ?? '',
      chainsOfPromathiaMission: progress.chainsOfPromathiaMission ?? ''
    });
  }

  private serializeWishlist(itemIds: number[]): string {
    return JSON.stringify([...itemIds].sort((left, right) => left - right));
  }
}