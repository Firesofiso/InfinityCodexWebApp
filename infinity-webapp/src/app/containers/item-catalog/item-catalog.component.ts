import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { AuthService } from '@services/auth.service';
import {
  ContentSourceReference,
  CatalogItemDetail,
  CatalogItemSummary,
  CatalogJobOption,
  ItemAccessoryStats,
  ItemArmorStats,
  ItemStatModifier,
  ItemWeaponStats,
  ContentSourceOption,
  ItemCatalogService,
  UpsertCatalogItemRequest
} from '@services/item-catalog.service';

type EditorState = 'idle' | 'saving' | 'saved' | 'error';

interface CatalogItemEditorModel {
  id: number | null;
  name: string;
  requiredLevel: number;
  slot: string | null;
  notes: string | null;
  imagePath: string | null;
  itemType: string | null;
  isRare: boolean;
  isExclusive: boolean;
  equipSlotGroup: string | null;
  rawEffectText: string | null;
  armorStats: ItemArmorStats | null;
  weaponStats: ItemWeaponStats | null;
  accessoryStats: ItemAccessoryStats | null;
  modifiers: ItemStatModifier[];
  allowedJobs: string[];
  sourceIds: number[];
  isActive: boolean;
}

interface CatalogSourceOptionView extends ContentSourceOption {
  displayLabel: string;
}

@Component({
  selector: 'app-item-catalog',
  standalone: true,
  imports: [CommonModule, FormsModule, NgSelectModule],
  templateUrl: './item-catalog.component.html',
  styleUrl: './item-catalog.component.css'
})
export class ItemCatalogComponent implements OnInit {
  private readonly itemCatalogService = inject(ItemCatalogService);
  private readonly authService = inject(AuthService);
  protected readonly itemTypeOptions = ['armor', 'weapon', 'accessory', 'consumable', 'other'];

  protected readonly isLoading = signal(true);
  protected readonly isLoadingDetail = signal(false);
  protected readonly isRefreshingList = signal(false);
  protected readonly pageError = signal<string | null>(null);
  protected readonly editorError = signal<string | null>(null);
  protected readonly editorNotice = signal<string | null>(null);
  protected readonly editorState = signal<EditorState>('idle');
  protected readonly selectedItemId = signal<number | null>(null);
  protected readonly search = signal('');
  protected readonly activeFilter = signal<'active' | 'archived' | 'all'>('active');
  protected readonly items = signal<CatalogItemSummary[]>([]);
  protected readonly sourceOptions = signal<ContentSourceOption[]>([]);
  protected readonly jobOptions = signal<CatalogJobOption[]>([]);
  protected readonly editorModel = signal<CatalogItemEditorModel>(this.createEmptyEditorModel());
  protected readonly sourceSelectOptions = computed<CatalogSourceOptionView[]>(() =>
    this.sourceOptions().map((source) => ({
      ...source,
      displayLabel: `${source.group.tag} / ${source.tag || source.name}`
    })));

  protected readonly session = this.authService.session;
  protected readonly canEdit = computed(() => this.session()?.role === 'Admin');

  protected readonly filteredItems = computed(() => {
    const query = this.search().trim().toLowerCase();
    const items = this.items();
    if (!query) {
      return items;
    }

    return items.filter((item) => {
      const sourceText = item.sources.map((source) => this.getSourceBadgeLabel(source)).join(' ');
      const modifierText = item.modifiers.map((modifier) => `${modifier.statKey} ${modifier.statValue} ${modifier.unit ?? ''}`).join(' ');
      const haystacks = [
        item.name,
        item.slot ?? '',
        item.notes ?? '',
        item.itemType ?? '',
        item.equipSlotGroup ?? '',
        item.rawEffectText ?? '',
        item.allowedJobs.join(' '),
        sourceText,
        modifierText
      ];
      return haystacks.some((value) => value.toLowerCase().includes(query));
    });
  });

  public ngOnInit(): void {
    this.loadReferenceData();
    this.loadItems();
  }

  protected selectItem(itemId: number): void {
    if (this.selectedItemId() === itemId) {
      return;
    }

    this.selectedItemId.set(itemId);
    this.editorError.set(null);
    this.editorNotice.set(null);
    this.editorState.set('idle');
    this.isLoadingDetail.set(true);

    this.itemCatalogService.getItem(itemId).subscribe({
      next: (item) => {
        this.editorModel.set(this.mapItemToEditorModel(item));
        this.isLoadingDetail.set(false);
      },
      error: () => {
        this.editorError.set('Unable to load item detail right now.');
        this.isLoadingDetail.set(false);
      }
    });
  }

  protected startCreate(): void {
    this.selectedItemId.set(null);
    this.editorError.set(null);
    this.editorNotice.set(null);
    this.editorState.set('idle');
    this.editorModel.set(this.createEmptyEditorModel());
  }

  protected saveItem(): void {
    if (!this.canEdit() || this.editorState() === 'saving') {
      return;
    }

    const payload = this.buildPayload(this.editorModel());
    this.editorState.set('saving');
    this.editorError.set(null);
    this.editorNotice.set(null);

    const itemId = this.editorModel().id;
    const request$ = itemId === null
      ? this.itemCatalogService.createItem(payload)
      : this.itemCatalogService.updateItem(itemId, payload);

    request$.subscribe({
      next: (item) => {
        this.editorModel.set(this.mapItemToEditorModel(item));
        this.selectedItemId.set(item.id);
        this.editorState.set('saved');
        this.editorNotice.set(itemId === null ? 'Catalog item created.' : 'Catalog item updated.');
        this.loadItems(true);
      },
      error: (error: unknown) => {
        this.editorState.set('error');
        this.editorError.set(this.getErrorMessage(error, 'Catalog item could not be saved.'));
      }
    });
  }

  protected toggleArchived(): void {
    const model = this.editorModel();
    if (!this.canEdit() || model.id === null || this.editorState() === 'saving') {
      return;
    }

    this.editorState.set('saving');
    this.editorError.set(null);
    this.editorNotice.set(null);

    this.itemCatalogService.setArchived(model.id, model.isActive).subscribe({
      next: (item) => {
        this.editorModel.set(this.mapItemToEditorModel(item));
        this.editorState.set('saved');
        this.editorNotice.set(item.isActive ? 'Catalog item restored.' : 'Catalog item archived.');
        this.loadItems(true);
      },
      error: (error: unknown) => {
        this.editorState.set('error');
        this.editorError.set(this.getErrorMessage(error, 'Catalog item archive state could not be updated.'));
      }
    });
  }

  protected setSearch(value: string): void {
    this.search.set(value);
  }

  protected updateName(value: string): void {
    this.editorModel.update((model) => ({ ...model, name: value }));
  }

  protected updateRequiredLevel(value: number): void {
    this.editorModel.update((model) => ({ ...model, requiredLevel: Number(value) || 0 }));
  }

  protected updateSlot(value: string | null): void {
    this.editorModel.update((model) => ({ ...model, slot: value }));
  }

  protected updateAllowedJobs(value: string[] | null): void {
    this.editorModel.update((model) => ({ ...model, allowedJobs: value ?? [] }));
  }

  protected updateSourceIds(value: number[] | null): void {
    this.editorModel.update((model) => ({ ...model, sourceIds: value ?? [] }));
  }

  protected updateNotes(value: string | null): void {
    this.editorModel.update((model) => ({ ...model, notes: value }));
  }

  protected updateImagePath(value: string | null): void {
    this.editorModel.update((model) => ({ ...model, imagePath: value }));
  }

  protected updateItemType(value: string | null): void {
    this.editorModel.update((model) => ({ ...model, itemType: value }));
  }

  protected updateEquipSlotGroup(value: string | null): void {
    this.editorModel.update((model) => ({ ...model, equipSlotGroup: value }));
  }

  protected updateRawEffectText(value: string | null): void {
    this.editorModel.update((model) => ({ ...model, rawEffectText: value }));
  }

  protected updateIsRare(value: boolean): void {
    this.editorModel.update((model) => ({ ...model, isRare: value }));
  }

  protected updateIsExclusive(value: boolean): void {
    this.editorModel.update((model) => ({ ...model, isExclusive: value }));
  }

  protected updateArmorStat(field: keyof ItemArmorStats, value: number | null): void {
    this.editorModel.update((model) => ({
      ...model,
      armorStats: {
        ...(model.armorStats ?? {}),
        [field]: value
      }
    }));
  }

  protected updateWeaponStat(field: keyof ItemWeaponStats, value: number | null): void {
    this.editorModel.update((model) => ({
      ...model,
      weaponStats: {
        ...(model.weaponStats ?? {}),
        [field]: value
      }
    }));
  }

  protected updateAccessoryStat(field: keyof ItemAccessoryStats, value: number | null): void {
    this.editorModel.update((model) => ({
      ...model,
      accessoryStats: {
        ...(model.accessoryStats ?? {}),
        [field]: value
      }
    }));
  }

  protected addModifier(): void {
    this.editorModel.update((model) => ({
      ...model,
      modifiers: [
        ...model.modifiers,
        {
          statKey: '',
          statValue: 0,
          unit: null,
          sortOrder: model.modifiers.length
        }
      ]
    }));
  }

  protected updateModifier(index: number, patch: Partial<ItemStatModifier>): void {
    this.editorModel.update((model) => ({
      ...model,
      modifiers: model.modifiers.map((modifier, modifierIndex) => modifierIndex === index
        ? { ...modifier, ...patch }
        : modifier)
    }));
  }

  protected removeModifier(index: number): void {
    this.editorModel.update((model) => ({
      ...model,
      modifiers: model.modifiers
        .filter((_, modifierIndex) => modifierIndex !== index)
        .map((modifier, modifierIndex) => ({ ...modifier, sortOrder: modifierIndex }))
    }));
  }

  protected trackByModifierIndex(index: number): number {
    return index;
  }

  protected getSourceBadgeLabel(source: ContentSourceReference): string {
    return `${source.group.tag} / ${source.tag || source.name}`;
  }

  protected getItemMetaLine(item: CatalogItemSummary): string {
    const parts = [item.slot || 'No slot yet'];

    if (item.requiredLevel > 0) {
      parts.push(`Lv ${item.requiredLevel}`);
    }

    if (item.itemType) {
      parts.push(item.itemType);
    }

    if (item.isRare) {
      parts.push('Rare');
    }

    if (item.isExclusive) {
      parts.push('Ex');
    }

    if (!item.isActive) {
      parts.push('Archived');
    }

    return parts.join(' · ');
  }

  protected setActiveFilter(value: 'active' | 'archived' | 'all'): void {
    this.activeFilter.set(value);
    this.loadItems(true);
  }

  protected trackByItemId(_index: number, item: CatalogItemSummary): number {
    return item.id;
  }

  private loadReferenceData(): void {
    this.itemCatalogService.getReferenceData().subscribe({
      next: (response) => {
        this.jobOptions.set(response.jobs);
        this.sourceOptions.set(response.sources.filter((source) => source.isActive));
      },
      error: () => {
        this.pageError.set('Unable to load catalog reference data.');
      }
    });
  }

  private loadItems(isRefresh: boolean = false): void {
    if (isRefresh) {
      this.isRefreshingList.set(true);
    } else {
      this.isLoading.set(true);
    }

    this.pageError.set(null);

    this.itemCatalogService.getItems({
      isActive: this.activeFilter() === 'all' ? null : this.activeFilter() === 'active'
    }).subscribe({
      next: (response) => {
        this.items.set(response.items);
        if (this.selectedItemId() === null && response.items.length > 0) {
          this.selectItem(response.items[0].id);
        }
        if (this.selectedItemId() !== null && !response.items.some((item) => item.id === this.selectedItemId())) {
          this.startCreate();
        }
        this.isLoading.set(false);
        this.isRefreshingList.set(false);
      },
      error: () => {
        this.pageError.set('Unable to load the item catalog right now.');
        this.isLoading.set(false);
        this.isRefreshingList.set(false);
      }
    });
  }

  private createEmptyEditorModel(): CatalogItemEditorModel {
    return {
      id: null,
      name: '',
      requiredLevel: 0,
      slot: null,
      notes: null,
      imagePath: null,
      itemType: 'armor',
      isRare: false,
      isExclusive: false,
      equipSlotGroup: null,
      rawEffectText: null,
      armorStats: null,
      weaponStats: null,
      accessoryStats: null,
      modifiers: [],
      allowedJobs: [],
      sourceIds: [],
      isActive: true
    };
  }

  private mapItemToEditorModel(item: CatalogItemDetail): CatalogItemEditorModel {
    return {
      id: item.id,
      name: item.name,
      requiredLevel: item.requiredLevel,
      slot: item.slot ?? null,
      notes: item.notes ?? null,
      imagePath: item.imagePath ?? null,
      itemType: item.itemType ?? null,
      isRare: item.isRare,
      isExclusive: item.isExclusive,
      equipSlotGroup: item.equipSlotGroup ?? null,
      rawEffectText: item.rawEffectText ?? null,
      armorStats: item.armorStats ?? null,
      weaponStats: item.weaponStats ?? null,
      accessoryStats: item.accessoryStats ?? null,
      modifiers: item.modifiers.map((modifier) => ({ ...modifier })),
      allowedJobs: [...item.allowedJobs],
      sourceIds: item.sources.map((source) => source.id),
      isActive: item.isActive
    };
  }

  private buildPayload(model: CatalogItemEditorModel): UpsertCatalogItemRequest {
    return {
      name: model.name.trim(),
      requiredLevel: Number(model.requiredLevel) || 0,
      slot: model.slot?.trim() ? model.slot.trim() : null,
      notes: model.notes?.trim() ? model.notes.trim() : null,
      imagePath: model.imagePath?.trim() ? model.imagePath.trim() : null,
      itemType: model.itemType?.trim() ? model.itemType.trim().toLowerCase() : null,
      isRare: model.isRare,
      isExclusive: model.isExclusive,
      equipSlotGroup: model.equipSlotGroup?.trim() ? model.equipSlotGroup.trim() : null,
      rawEffectText: model.rawEffectText?.trim() ? model.rawEffectText.trim() : null,
      armorStats: this.normalizeArmorStats(model.armorStats),
      weaponStats: this.normalizeWeaponStats(model.weaponStats),
      accessoryStats: this.normalizeAccessoryStats(model.accessoryStats),
      modifiers: model.modifiers
        .map((modifier, index) => ({
          statKey: modifier.statKey.trim(),
          statValue: Number(modifier.statValue),
          unit: modifier.unit?.trim() ? modifier.unit.trim() : null,
          sortOrder: index
        }))
        .filter((modifier) => modifier.statKey.length > 0 && !Number.isNaN(modifier.statValue)),
      allowedJobs: [...model.allowedJobs],
      sourceIds: [...model.sourceIds]
    };
  }

  private normalizeArmorStats(stats: ItemArmorStats | null): ItemArmorStats | null {
    if (!stats) {
      return null;
    }

    const defense = this.normalizeNumberValue(stats.defense);
    const magicDefense = this.normalizeNumberValue(stats.magicDefense);

    if (defense === null && magicDefense === null) {
      return null;
    }

    return { defense, magicDefense };
  }

  private normalizeWeaponStats(stats: ItemWeaponStats | null): ItemWeaponStats | null {
    if (!stats) {
      return null;
    }

    const damage = this.normalizeNumberValue(stats.damage);
    const delay = this.normalizeNumberValue(stats.delay);
    const dps = this.normalizeNumberValue(stats.dps);

    if (damage === null && delay === null && dps === null) {
      return null;
    }

    return { damage, delay, dps };
  }

  private normalizeAccessoryStats(stats: ItemAccessoryStats | null): ItemAccessoryStats | null {
    if (!stats) {
      return null;
    }

    const charges = this.normalizeNumberValue(stats.charges);
    const recastSeconds = this.normalizeNumberValue(stats.recastSeconds);

    if (charges === null && recastSeconds === null) {
      return null;
    }

    return { charges, recastSeconds };
  }

  private normalizeNumberValue(value: number | null | undefined): number | null {
    if (value === null || value === undefined) {
      return null;
    }

    const numericValue = Number(value);
    return Number.isNaN(numericValue) ? null : numericValue;
  }

  private getErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const message = error.error?.message;
      if (typeof message === 'string' && message.length > 0) {
        return message;
      }
      if (error.status === 403) {
        return 'Your account does not have permission to modify catalog items.';
      }
    }

    return fallback;
  }
}