import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import {
  CharacterDynamisClears,
  CharacterMissionProgress,
  CharacterWorkspaceDetailResponse,
  CharacterWorkspaceListItem,
  HorizonJobLevel
} from '@services/character-workspace.service';
import {
  BASTOK_MISSIONS,
  CHAINS_OF_PROMATHIA_MISSIONS,
  EPILOGUE_MISSIONS,
  MissionOption,
  RISE_OF_THE_ZILART_MISSIONS,
  SAN_DORIA_MISSIONS,
  WINDURST_MISSIONS
} from './mission-catalog';

type MissionPanelSaveState = 'idle' | 'dirty' | 'saving' | 'saved' | 'error';

@Component({
  selector: 'app-character-detail-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, NgSelectModule],
  templateUrl: './character-detail-panel.component.html',
  styleUrl: './character-detail-panel.component.css'
})
export class CharacterDetailPanelComponent implements OnChanges, OnDestroy {
  @Input() public characters: CharacterWorkspaceListItem[] = [];
  @Input() public mainCharacterId: number | null = null;
  @Input() public selectedCharacterId: number | null = null;
  @Input() public currentDetail: CharacterWorkspaceDetailResponse | null = null;
  @Input() public isLoadingDetail = false;
  @Input() public missionDraft: CharacterMissionProgress = {
    sandOriaMission: null,
    bastokMission: null,
    windurstMission: null,
    riseOfTheZilartMission: null,
    chainsOfPromathiaMission: null,
    epilogueMission: null,
    updatedAt: null
  };
  @Input() public missionSaveState: MissionPanelSaveState = 'idle';
  @Input() public missionSavedNoticeVisible = false;
  @Input() public missionError: string | null = null;
  @Input() public dynamisDraft: CharacterDynamisClears = {
    dynamisSandOria: false, dynamisBastok: false, dynamisWindurst: false, dynamisJeuno: false,
    dynamisBeaucedine: false, dynamisXarcabard: false, dynamisValkurm: false, dynamisBuburimu: false,
    dynamisQufim: false, dynamisTavnazia: false
  };
  @Input() public dynamisSaveState: MissionPanelSaveState = 'idle';
  @Input() public dynamisError: string | null = null;

  public readonly dynamisCityZones: { field: keyof CharacterDynamisClears; label: string }[] = [
    { field: 'dynamisSandOria', label: "San d'Oria" },
    { field: 'dynamisBastok', label: 'Bastok' },
    { field: 'dynamisWindurst', label: 'Windurst' },
    { field: 'dynamisJeuno', label: 'Jeuno' }
  ];

  public readonly dynamisIcelandZones: { field: keyof CharacterDynamisClears; label: string }[] = [
    { field: 'dynamisBeaucedine', label: 'Beaucedine' },
    { field: 'dynamisXarcabard', label: 'Xarcabard' }
  ];

  public readonly dynamisDreamlandZones: { field: keyof CharacterDynamisClears; label: string }[] = [
    { field: 'dynamisValkurm', label: 'Valkurm' },
    { field: 'dynamisBuburimu', label: 'Buburimu' },
    { field: 'dynamisQufim', label: 'Qufim' }
  ];

  public readonly sandOriaMissionOptions: MissionOption[] = SAN_DORIA_MISSIONS;
  public readonly bastokMissionOptions: MissionOption[] = BASTOK_MISSIONS;
  public readonly windurstMissionOptions: MissionOption[] = WINDURST_MISSIONS;
  public readonly riseOfTheZilartMissionOptions: MissionOption[] = RISE_OF_THE_ZILART_MISSIONS;
  public readonly chainsOfPromathiaMissionOptions: MissionOption[] = CHAINS_OF_PROMATHIA_MISSIONS;
  public readonly epilogueMissionOptions: MissionOption[] = EPILOGUE_MISSIONS;

  public showLoadingStatus = false;
  public loadingStatusFadingOut = false;

  private loadingStatusHideTimer: ReturnType<typeof setTimeout> | null = null;

  @Output() public readonly characterSelected = new EventEmitter<number>();
  @Output() public readonly mainCharacterSelected = new EventEmitter<number>();
  @Output() public readonly missionChanged = new EventEmitter<{ field: keyof CharacterMissionProgress; value: string | null }>();
  @Output() public readonly dynamisChanged = new EventEmitter<{ field: keyof CharacterDynamisClears; value: boolean }>();

  private readonly jobDisplayOrder = [
    'WAR', 'MNK', 'WHM', 'BLM', 'RDM', 'THF', 'PLD', 'DRK',
    'BST', 'BRD', 'RNG', 'SAM', 'NIN', 'DRG', 'SMN'
  ];
  private readonly jobDisplayOrderMap = new Map(this.jobDisplayOrder.map((jobCode, index) => [jobCode, index]));

  public ngOnChanges(changes: SimpleChanges): void {
    if (!('isLoadingDetail' in changes)) {
      return;
    }

    if (this.isLoadingDetail) {
      this.clearLoadingStatusTimer();
      this.showLoadingStatus = true;
      this.loadingStatusFadingOut = false;
      return;
    }

    if (!this.showLoadingStatus) {
      return;
    }

    this.loadingStatusFadingOut = true;
    this.clearLoadingStatusTimer();
    this.loadingStatusHideTimer = setTimeout(() => {
      this.showLoadingStatus = false;
      this.loadingStatusFadingOut = false;
      this.loadingStatusHideTimer = null;
    }, 260);
  }

  public ngOnDestroy(): void {
    this.clearLoadingStatusTimer();
  }

  public selectCharacter(characterId: number): void {
    this.characterSelected.emit(characterId);
  }

  public getOrderedJobs(jobs?: HorizonJobLevel[] | null): HorizonJobLevel[] {
    if (!jobs?.length) {
      return [];
    }

    return [...jobs].sort((left, right) => {
      const leftIndex = this.jobDisplayOrderMap.get(left.jobCode) ?? Number.MAX_SAFE_INTEGER;
      const rightIndex = this.jobDisplayOrderMap.get(right.jobCode) ?? Number.MAX_SAFE_INTEGER;

      if (leftIndex !== rightIndex) {
        return leftIndex - rightIndex;
      }

      return left.jobCode.localeCompare(right.jobCode);
    });
  }

  public getInitial(name: string): string {
    return name.trim().charAt(0).toUpperCase();
  }

  public isActive(characterId: number): boolean {
    return this.selectedCharacterId === characterId;
  }

  public hasLiveDetailForCharacter(characterId: number): boolean {
    return this.currentDetail?.character.characterId === characterId;
  }

  public getPortraitUrl(character: CharacterWorkspaceListItem): string | null {
    if (this.isActive(character.characterId) && this.hasLiveDetailForCharacter(character.characterId)) {
      return this.currentDetail?.horizon?.portraitUrl ?? character.portraitUrl ?? null;
    }

    return character.portraitUrl ?? null;
  }

  public getDetailText(character: CharacterWorkspaceListItem): string {
    if (this.isActive(character.characterId) && this.hasLiveDetailForCharacter(character.characterId)) {
      return this.currentDetail?.horizon?.jobString || 'No live Horizon job data available.';
    }

    if (character.lastSyncedAt) {
      return `Synced ${new Date(character.lastSyncedAt).toLocaleDateString()}`;
    }

    return 'Click to view';
  }

  public onMissionInput(field: keyof CharacterMissionProgress, value: string | null): void {
    this.missionChanged.emit({ field, value });
  }

  public onDynamisToggle(field: keyof CharacterDynamisClears, value: boolean): void {
    this.dynamisChanged.emit({ field, value });
  }

  public get areIcelandsAccessible(): boolean {
    return this.dynamisDraft.dynamisSandOria
      && this.dynamisDraft.dynamisBastok
      && this.dynamisDraft.dynamisWindurst
      && this.dynamisDraft.dynamisJeuno;
  }

  public get isTavnaziaAccessible(): boolean {
    return this.dynamisDraft.dynamisValkurm
      && this.dynamisDraft.dynamisBuburimu
      && this.dynamisDraft.dynamisQufim;
  }

  public isMainCharacter(characterId: number): boolean {
    return this.mainCharacterId === characterId;
  }

  public getDisplayCharacters(): CharacterWorkspaceListItem[] {
    if (this.mainCharacterId === null) {
      return this.characters;
    }

    const mainCharacter = this.characters.find((character) => character.characterId === this.mainCharacterId);
    if (!mainCharacter) {
      return this.characters;
    }

    return [
      mainCharacter,
      ...this.characters.filter((character) => character.characterId !== this.mainCharacterId)
    ];
  }

  public setSelectedAsMainCharacter(): void {
    if (this.selectedCharacterId === null) {
      return;
    }

    this.mainCharacterSelected.emit(this.selectedCharacterId);
  }

  private clearLoadingStatusTimer(): void {
    if (this.loadingStatusHideTimer) {
      clearTimeout(this.loadingStatusHideTimer);
      this.loadingStatusHideTimer = null;
    }
  }
}
