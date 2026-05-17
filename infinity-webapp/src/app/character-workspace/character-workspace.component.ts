import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  DkpTransactionEntry,
  CharacterDynamisClears,
  CharacterMissionProgress,
  CharacterWishlistAssignment,
  CharacterWishlistItem,
  CharacterWishlistSource,
  CharacterWorkspaceDetailResponse,
  CharacterWorkspaceListItem,
  CharacterWorkspaceService
} from '../../services/character-workspace.service';
import { AuthService } from '../../services/auth.service';
import { CharacterDetailPanelComponent } from './character-detail-panel.component';

type SaveState = 'idle' | 'dirty' | 'saving' | 'saved' | 'error';

export interface DkpHistoryEntry {
  id: number;
  date: string;
  type: string;
  reason: string;
  amount: number;
}

@Component({
  selector: 'app-character-workspace',
  standalone: true,
  imports: [CommonModule, FormsModule, CharacterDetailPanelComponent],
  templateUrl: './character-workspace.component.html',
  styleUrl: './character-workspace.component.css'
})
export class CharacterWorkspaceComponent implements OnInit, OnDestroy {
  private readonly characterWorkspaceService = inject(CharacterWorkspaceService);
  private readonly authService = inject(AuthService);
  private readonly previewWishlistItems: CharacterWishlistItem[] = [
    {
      itemId: 900001,
      name: 'Abyssal War Hammer',
      requiredLevel: 75,
      slot: 'Main Hand',
      notes: 'Preview seed item for styling checks.',
      allowedJobs: ['WAR', 'PLD', 'DRK'],
      sources: [
        this.createPreviewSource(1, 'Dynamis - Xarcabard', 'dynamis-xarcabard', 1, 'Dynamis', 'dynamis'),
        this.createPreviewSource(2, 'Relic Upgrade', 'relic-upgrade', 2, 'Legacy', 'legacy')
      ]
    },
    {
      itemId: 900002,
      name: 'Sageweave Circlet',
      requiredLevel: 72,
      slot: 'Head',
      notes: 'Magic accuracy and enfeebling bonus.',
      allowedJobs: ['WHM', 'BLM', 'RDM', 'SMN'],
      sources: [
        this.createPreviewSource(3, 'Sky', 'sky', 2, 'Legacy', 'legacy'),
        this.createPreviewSource(4, 'Crafted', 'crafted', 2, 'Legacy', 'legacy')
      ]
    },
    {
      itemId: 900003,
      name: 'Ranger\'s Hawk Mantle',
      requiredLevel: 70,
      slot: 'Back',
      notes: 'Ranged attack focused cape for marksmanship builds.',
      allowedJobs: ['RNG', 'THF', 'NIN'],
      sources: [this.createPreviewSource(5, 'HNMs', 'hnms', 2, 'Legacy', 'legacy')]
    },
    {
      itemId: 900004,
      name: 'Elder Dragon Ring',
      requiredLevel: 73,
      slot: 'Ring',
      notes: null,
      allowedJobs: [],
      sources: [
        this.createPreviewSource(6, 'Kirin', 'kirin', 2, 'Legacy', 'legacy'),
        this.createPreviewSource(7, 'Event Token Exchange', 'event-token-exchange', 2, 'Legacy', 'legacy')
      ]
    },
    {
      itemId: 900005,
      name: 'Corsair\'s Phantom Belt',
      requiredLevel: 68,
      slot: 'Waist',
      notes: 'Great for support and ranged hybrid sets.',
      allowedJobs: ['BRD', 'RNG', 'NIN'],
      sources: [this.createPreviewSource(8, 'Limbus', 'limbus', 2, 'Legacy', 'legacy')]
    },
    {
      itemId: 900006,
      name: 'Verdant Aegis Greaves',
      requiredLevel: 71,
      slot: 'Feet',
      notes: 'High evasion option for survivability sets.',
      allowedJobs: ['THF', 'NIN', 'DNC'],
      sources: []
    }
  ];
  private missionSaveTimer: ReturnType<typeof setTimeout> | null = null;
  private missionSavedNoticeTimer: ReturnType<typeof setTimeout> | null = null;
  private dynamisSaveTimer: ReturnType<typeof setTimeout> | null = null;
  private wishlistSaveTimer: ReturnType<typeof setTimeout> | null = null;
  private isSavingMission = false;
  private isSavingDynamis = false;
  private isSavingWishlist = false;
  private pendingMissionSave = false;
  private pendingDynamisSave = false;
  private pendingWishlistSave = false;
  private lastSavedMissionSignature = '';
  private lastSavedDynamisSignature = '';
  private lastSavedWishlistSignature = '';

  protected readonly isLoadingCharacters = signal(true);
  protected readonly isLoadingDetail = signal(false);
  protected readonly pageError = signal<string | null>(null);
  protected readonly detailError = signal<string | null>(null);
  protected readonly characters = signal<CharacterWorkspaceListItem[]>([]);
  protected readonly mainCharacterId = signal<number | null>(null);
  protected readonly selectedCharacterId = signal<number | null>(null);
  protected readonly detail = signal<CharacterWorkspaceDetailResponse | null>(null);
  protected readonly missionDraft = signal<CharacterMissionProgress>(this.createEmptyMissionProgress());
  protected readonly dynamisDraft = signal<CharacterDynamisClears>(this.createEmptyDynamisClears());
  protected readonly wishlistAssignments = signal<CharacterWishlistAssignment[]>([]);
  protected readonly missionSaveState = signal<SaveState>('idle');
  protected readonly missionSavedNoticeVisible = signal(false);
  protected readonly dynamisSaveState = signal<SaveState>('idle');
  protected readonly wishlistSaveState = signal<SaveState>('idle');
  protected readonly missionError = signal<string | null>(null);
  protected readonly dynamisError = signal<string | null>(null);
  protected readonly wishlistError = signal<string | null>(null);

  // DKP-specific state
  protected readonly canManageDkp = computed(() => {
    return this.authService.canManageDkp();
  });
  protected readonly dkpHistory = signal<DkpHistoryEntry[]>([]);
  protected readonly dkpAwardAmount = signal<number | null>(null);
  protected readonly dkpAwardReason = signal('');
  protected readonly dkpAwardError = signal<string | null>(null);
  protected readonly dkpAwardSaving = signal(false);

  // Tab for the secondary info panel (jobs, dynamis, missions)
  protected readonly secondaryTab = signal<'jobs' | 'dynamis' | 'missions'>('jobs');

  protected readonly wishlistFilter = signal('');

  protected readonly filteredWishlistItems = computed(() => {
    const detail = this.detail();
    const query = this.wishlistFilter().trim().toLowerCase();

    if (!detail) {
      return [] as CharacterWishlistItem[];
    }

    const wishlistItems = detail.wishlist.availableItems.length > 0
      ? detail.wishlist.availableItems
      : this.previewWishlistItems;

    if (!query) {
      return wishlistItems;
    }

    return wishlistItems.filter((item) => {
      const haystacks = [
        item.name,
        item.slot ?? '',
        item.notes ?? '',
        item.allowedJobs.join(' '),
        item.sources.map((source) => this.getWishlistSourceLabel(source)).join(' ')
      ];

      return haystacks.some((value) => value.toLowerCase().includes(query));
    });
  });

  public ngOnInit(): void {
    this.loadCharacters();
  }

  public ngOnDestroy(): void {
    this.clearMissionTimer();
    this.clearMissionSavedNoticeTimer();
    this.clearDynamisTimer();
    this.clearWishlistTimer();
  }

  protected selectCharacter(characterId: number): void {
    if (this.selectedCharacterId() === characterId) {
      return;
    }

    this.selectedCharacterId.set(characterId);
    this.detailError.set(null);
    this.missionError.set(null);
    this.dynamisError.set(null);
    this.wishlistError.set(null);
    this.dkpAwardError.set(null);
    this.missionSaveState.set('idle');
    this.missionSavedNoticeVisible.set(false);
    this.dynamisSaveState.set('idle');
    this.wishlistSaveState.set('idle');
    this.pendingMissionSave = false;
    this.pendingDynamisSave = false;
    this.pendingWishlistSave = false;
    this.clearMissionTimer();
    this.clearDynamisTimer();
    this.clearWishlistTimer();
    this.loadCharacterDetail(characterId);
  }

  protected setMainCharacter(characterId: number): void {
    if (!this.characters().some((character) => character.characterId === characterId)) {
      return;
    }

    this.mainCharacterId.set(characterId);
    this.characterWorkspaceService.setMainCharacter(characterId).subscribe();
  }

  protected updateMission(field: keyof CharacterMissionProgress, value: string | null): void {
    this.missionDraft.update((current) => ({
      ...current,
      [field]: this.normalizeMissionValue(value)
    }));

    this.missionSaveState.set('dirty');
    this.missionSavedNoticeVisible.set(false);
    this.missionError.set(null);
    this.queueMissionSave();
  }

  protected updateDynamis(field: keyof CharacterDynamisClears, value: boolean): void {
    this.dynamisDraft.update((current) => ({ ...current, [field]: value }));
    this.dynamisSaveState.set('dirty');
    this.dynamisError.set(null);
    this.queueDynamisSave();
  }

  protected toggleWishlistItem(itemId: number): void {
    if (this.isWishlistItemNeeded(itemId)) {
      this.wishlistAssignments.set(this.wishlistAssignments().filter((assignment) => assignment.itemId !== itemId));
    } else {
      const selectedCharacterId = this.selectedCharacterId();
      if (selectedCharacterId === null) {
        return;
      }

      this.wishlistAssignments.set(this.normalizeWishlistAssignments([
        ...this.wishlistAssignments(),
        {
          itemId,
          characterIds: [selectedCharacterId]
        }
      ]));
    }

    this.wishlistSaveState.set('dirty');
    this.wishlistError.set(null);
    this.queueWishlistSave();
  }

  protected toggleWishlistAssignmentCharacter(itemId: number, characterId: number): void {
    const assignments = this.wishlistAssignments();
    const existing = assignments.find((assignment) => assignment.itemId === itemId);

    let nextAssignments: CharacterWishlistAssignment[];

    if (!existing)
    {
      const selectedCharacterId = this.selectedCharacterId();
      const baselineIds = selectedCharacterId !== null && selectedCharacterId !== characterId
        ? [selectedCharacterId, characterId]
        : [characterId];

      nextAssignments = [...assignments, { itemId, characterIds: baselineIds }];
    }
    else
    {
      const hasCharacter = existing.characterIds.includes(characterId);
      const nextCharacterIds = hasCharacter
        ? existing.characterIds.filter((value) => value !== characterId)
        : [...existing.characterIds, characterId];

      nextAssignments = nextCharacterIds.length === 0
        ? assignments.filter((assignment) => assignment.itemId !== itemId)
        : assignments.map((assignment) => assignment.itemId === itemId
          ? { ...assignment, characterIds: nextCharacterIds }
          : assignment);
    }

    this.wishlistAssignments.set(this.normalizeWishlistAssignments(nextAssignments));
    this.wishlistSaveState.set('dirty');
    this.wishlistError.set(null);
    this.queueWishlistSave();
  }

  protected markWishlistItemObtained(itemId: number): void {
    const characterId = this.selectedCharacterId();
    if (characterId === null) {
      return;
    }

    this.characterWorkspaceService.markItemObtained(characterId, itemId).subscribe({
      next: () => {
        const assignments = this.wishlistAssignments();
        const existing = assignments.find((assignment) => assignment.itemId === itemId);
        if (!existing) {
          return;
        }

        const remainingCharacterIds = existing.characterIds.filter((id) => id !== characterId);
        const nextAssignments = remainingCharacterIds.length === 0
          ? assignments.filter((assignment) => assignment.itemId !== itemId)
          : assignments.map((assignment) =>
              assignment.itemId === itemId ? { ...assignment, characterIds: remainingCharacterIds } : assignment
            );

        const normalized = this.normalizeWishlistAssignments(nextAssignments);
        this.wishlistAssignments.set(normalized);
        this.lastSavedWishlistSignature = this.serializeWishlist(normalized);
      },
      error: (error: { error?: { message?: string } }) => {
        this.wishlistError.set(error.error?.message ?? 'Could not mark item as obtained.');
      }
    });
  }

  protected isWishlistItemNeeded(itemId: number): boolean {
    return this.getWishlistAssignmentCharacterIds(itemId).length > 0;
  }

  protected getWishlistAssignmentCharacterIds(itemId: number): number[] {
    return this.wishlistAssignments().find((assignment) => assignment.itemId === itemId)?.characterIds ?? [];
  }

  protected isWishlistAssignedToCharacter(itemId: number, characterId: number): boolean {
    return this.getWishlistAssignmentCharacterIds(itemId).includes(characterId);
  }

  protected getCharacterNameById(characterId: number): string {
    return this.characters().find((character) => character.characterId === characterId)?.name ?? `Character ${characterId}`;
  }

  protected getWishlistSourceLabel(source: CharacterWishlistSource): string {
    return `${source.group.tag} / ${source.tag || source.name}`;
  }

  protected getWishlistSourceSummary(item: CharacterWishlistItem): string {
    if (item.sources.length === 0) {
      return 'No source tags';
    }

    return item.sources.map((source) => this.getWishlistSourceLabel(source)).join(' · ');
  }

  protected submitDkpAward(characterId: number): void {
    const amount = this.dkpAwardAmount();
    const reason = this.dkpAwardReason().trim();

    if (amount === null || amount === 0) {
      this.dkpAwardError.set('Amount must be a non-zero number.');
      return;
    }

    if (!reason) {
      this.dkpAwardError.set('A reason is required.');
      return;
    }

    this.dkpAwardError.set(null);
    this.dkpAwardSaving.set(true);

    this.characterWorkspaceService.adjustDkp(characterId, { amount, reason }).subscribe({
      next: (response) => {
        this.detail.update((current) => {
          if (!current || current.character.characterId !== characterId) {
            return current;
          }

          return {
            ...current,
            character: {
              ...current.character,
              dkpTotal: response.newBalance
            }
          };
        });

        this.dkpHistory.update((current) => [
          this.toHistoryEntry(response.transaction),
          ...current
        ]);

        this.dkpAwardAmount.set(null);
        this.dkpAwardReason.set('');
        this.dkpAwardSaving.set(false);
      },
      error: (error: { error?: { message?: string } }) => {
        this.dkpAwardError.set(error.error?.message ?? 'DKP adjustment could not be saved.');
        this.dkpAwardSaving.set(false);
      }
    });
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
          this.mainCharacterId.set(null);
          this.selectedCharacterId.set(null);
          this.detail.set(null);
          return;
        }

        const preferredCharacterId = this.resolvePreferredCharacterId(characters, response.mainCharacterId ?? null);
        this.mainCharacterId.set(preferredCharacterId);
        this.selectCharacter(preferredCharacterId);
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
        this.dynamisDraft.set(response.dynamis ?? this.createEmptyDynamisClears());
        this.lastSavedDynamisSignature = JSON.stringify(response.dynamis ?? this.createEmptyDynamisClears());
        const assignments = this.normalizeWishlistAssignments(
          response.wishlist.assignments?.length
            ? response.wishlist.assignments
            : (response.wishlist.selectedItemIds ?? []).map((itemId) => ({ itemId, characterIds: [characterId] }))
        );
        this.wishlistAssignments.set(assignments);
        this.lastSavedMissionSignature = this.serializeMission(this.normalizeMissionProgress(response.missions));
        this.lastSavedWishlistSignature = this.serializeWishlist(assignments);
        this.missionSaveState.set('idle');
        this.wishlistSaveState.set('idle');
        this.detailError.set(null);
        this.loadDkpHistory(characterId);
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

  private queueDynamisSave(): void {
    this.clearDynamisTimer();
    this.dynamisSaveTimer = setTimeout(() => {
      if (this.isSavingDynamis) {
        this.pendingDynamisSave = true;
        return;
      }

      void this.commitDynamisSave();
    }, 700);
  }

  private async commitDynamisSave(): Promise<void> {
    const characterId = this.selectedCharacterId();
    if (characterId === null) {
      return;
    }

    const payload = this.dynamisDraft();
    const signature = JSON.stringify(payload);
    if (signature === this.lastSavedDynamisSignature) {
      this.dynamisSaveState.set('idle');
      return;
    }

    this.isSavingDynamis = true;
    this.dynamisSaveState.set('saving');
    this.dynamisError.set(null);

    this.characterWorkspaceService.updateDynamisClears(characterId, payload).subscribe({
      next: (response) => {
        if (this.selectedCharacterId() !== characterId) {
          return;
        }

        this.dynamisDraft.set(response);
        this.detail.update((current) => current ? { ...current, dynamis: response } : current);
        this.lastSavedDynamisSignature = JSON.stringify(response);
        this.dynamisSaveState.set('idle');
      },
      error: (error: { error?: { message?: string } }) => {
        if (this.selectedCharacterId() !== characterId) {
          return;
        }

        this.dynamisError.set(error.error?.message ?? 'Dynamis clears could not be saved.');
        this.dynamisSaveState.set('error');
      },
      complete: () => {
        this.isSavingDynamis = false;
        if (this.pendingDynamisSave || JSON.stringify(this.dynamisDraft()) !== this.lastSavedDynamisSignature) {
          this.pendingDynamisSave = false;
          this.queueDynamisSave();
        }
      }
    });
  }

  private clearDynamisTimer(): void {
    if (this.dynamisSaveTimer) {
      clearTimeout(this.dynamisSaveTimer);
      this.dynamisSaveTimer = null;
    }
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
      this.missionSaveState.set('idle');
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
        this.missionSaveState.set('idle');
        this.showMissionSavedNotice();
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

  private loadDkpHistory(characterId: number): void {
    this.characterWorkspaceService.getDkpTransactions(characterId, 50).subscribe({
      next: (response) => {
        if (this.selectedCharacterId() !== characterId) {
          return;
        }

        this.dkpHistory.set(response.entries.map((entry) => this.toHistoryEntry(entry)));
      },
      error: () => {
        if (this.selectedCharacterId() !== characterId) {
          return;
        }

        this.dkpHistory.set([]);
      }
    });
  }

  private toHistoryEntry(entry: DkpTransactionEntry): DkpHistoryEntry {
    const date = new Date(entry.createdAt);
    const dateLabel = Number.isNaN(date.getTime())
      ? entry.createdAt
      : date.toISOString().slice(0, 10);

    return {
      id: entry.id,
      date: dateLabel,
      type: this.getDkpTypeLabel(entry.sourceType, entry.amount),
      reason: entry.reason,
      amount: entry.amount
    };
  }

  private getDkpTypeLabel(sourceType: string, amount: number): string {
    if (sourceType === 'bulk_earn_event') {
      return 'Earned';
    }

    if (sourceType === 'manual_adjustment') {
      return amount >= 0 ? 'Adjusted +' : 'Adjusted -';
    }

    return amount >= 0 ? 'Earned' : 'Spent';
  }

  private async commitWishlistSave(): Promise<void> {
    const characterId = this.selectedCharacterId();
    if (characterId === null) {
      return;
    }

    const assignments = this.normalizeWishlistAssignments(this.wishlistAssignments());
    const signature = this.serializeWishlist(assignments);
    if (signature === this.lastSavedWishlistSignature) {
      this.wishlistSaveState.set('saved');
      return;
    }

    this.isSavingWishlist = true;
    this.wishlistSaveState.set('saving');
    this.wishlistError.set(null);

    this.characterWorkspaceService.updateWishlist(characterId, assignments).subscribe({
      next: (response) => {
        if (this.selectedCharacterId() !== characterId) {
          return;
        }

        const normalizedAssignments = this.normalizeWishlistAssignments(response.assignments ?? []);
        this.wishlistAssignments.set(normalizedAssignments);
        this.detail.update((current) => current
          ? {
            ...current,
            wishlist: {
              ...current.wishlist,
              selectedItemIds: [...(response.selectedItemIds ?? [])].sort((left, right) => left - right),
              assignments: normalizedAssignments
            }
          }
          : current);
        this.lastSavedWishlistSignature = this.serializeWishlist(normalizedAssignments);
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
        if (this.pendingWishlistSave || this.serializeWishlist(this.wishlistAssignments()) !== this.lastSavedWishlistSignature) {
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

  private showMissionSavedNotice(): void {
    this.clearMissionSavedNoticeTimer();
    this.missionSavedNoticeVisible.set(true);
    this.missionSavedNoticeTimer = setTimeout(() => {
      this.missionSavedNoticeVisible.set(false);
    }, 1800);
  }

  private clearMissionSavedNoticeTimer(): void {
    if (this.missionSavedNoticeTimer) {
      clearTimeout(this.missionSavedNoticeTimer);
      this.missionSavedNoticeTimer = null;
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
      sandOriaMission: this.normalizeMissionValue(progress.sandOriaMission),
      bastokMission: this.normalizeMissionValue(progress.bastokMission),
      windurstMission: this.normalizeMissionValue(progress.windurstMission),
      riseOfTheZilartMission: this.normalizeMissionValue(progress.riseOfTheZilartMission),
      chainsOfPromathiaMission: this.normalizeMissionValue(progress.chainsOfPromathiaMission),
      epilogueMission: this.normalizeMissionValue(progress.epilogueMission),
      updatedAt: progress.updatedAt ?? null
    };
  }

  private createEmptyDynamisClears(): CharacterDynamisClears {
    return {
      dynamisSandOria: false,
      dynamisBastok: false,
      dynamisWindurst: false,
      dynamisJeuno: false,
      dynamisBeaucedine: false,
      dynamisXarcabard: false,
      dynamisValkurm: false,
      dynamisBuburimu: false,
      dynamisQufim: false,
      dynamisTavnazia: false
    };
  }

  private createEmptyMissionProgress(): CharacterMissionProgress {
    return {
      sandOriaMission: null,
      bastokMission: null,
      windurstMission: null,
      riseOfTheZilartMission: null,
      chainsOfPromathiaMission: null,
      epilogueMission: null,
      updatedAt: null
    };
  }

  private createPreviewSource(
    id: number,
    name: string,
    tag: string,
    groupId: number,
    groupName: string,
    groupTag: string
  ): CharacterWishlistSource {
    return {
      id,
      name,
      tag,
      group: {
        id: groupId,
        name: groupName,
        tag: groupTag
      }
    };
  }

  private serializeMission(progress: CharacterMissionProgress): string {
    return JSON.stringify({
      sandOriaMission: this.normalizeMissionValue(progress.sandOriaMission),
      bastokMission: this.normalizeMissionValue(progress.bastokMission),
      windurstMission: this.normalizeMissionValue(progress.windurstMission),
      riseOfTheZilartMission: this.normalizeMissionValue(progress.riseOfTheZilartMission),
      chainsOfPromathiaMission: this.normalizeMissionValue(progress.chainsOfPromathiaMission),
      epilogueMission: this.normalizeMissionValue(progress.epilogueMission)
    });
  }

  private normalizeMissionValue(value: string | null | undefined): string | null {
    if (value === null || value === undefined) {
      return null;
    }

    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }

  private normalizeWishlistAssignments(assignments: CharacterWishlistAssignment[]): CharacterWishlistAssignment[] {
    return assignments
      .filter((assignment) => assignment.itemId > 0)
      .map((assignment) => ({
        itemId: assignment.itemId,
        characterIds: [...new Set(assignment.characterIds)].sort((left, right) => left - right)
      }))
      .filter((assignment) => assignment.characterIds.length > 0)
      .sort((left, right) => left.itemId - right.itemId);
  }

  private serializeWishlist(assignments: CharacterWishlistAssignment[]): string {
    return JSON.stringify(this.normalizeWishlistAssignments(assignments));
  }

  private resolvePreferredCharacterId(characters: CharacterWorkspaceListItem[], serverMainCharacterId: number | null): number {
    if (serverMainCharacterId !== null && characters.some((character) => character.characterId === serverMainCharacterId)) {
      return serverMainCharacterId;
    }

    return characters[0].characterId;
  }

}