import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, RegistrationContextResponse } from '@services/auth.service';
import { CharacterSearchResult, CharacterSearchService } from '@services/character-search.service';

interface JobOption {
    code: string;
    label: string;
    tier: 'basic' | 'advanced';
    iconUrl?: string;
}

const JOB_OPTIONS: JobOption[] = [
    { code: 'WAR', label: 'Warrior', tier: 'basic' },
    { code: 'MNK', label: 'Monk', tier: 'basic' },
    { code: 'WHM', label: 'White Mage', tier: 'basic' },
    { code: 'BLM', label: 'Black Mage', tier: 'basic' },
    { code: 'RDM', label: 'Red Mage', tier: 'basic' },
    { code: 'THF', label: 'Thief', tier: 'basic' },
    { code: 'PLD', label: 'Paladin', tier: 'advanced' },
    { code: 'DRK', label: 'Dark Knight', tier: 'advanced' },
    { code: 'BST', label: 'Beastmaster', tier: 'advanced' },
    { code: 'BRD', label: 'Bard', tier: 'advanced' },
    { code: 'RNG', label: 'Ranger', tier: 'advanced' },
    { code: 'SAM', label: 'Samurai', tier: 'advanced' },
    { code: 'NIN', label: 'Ninja', tier: 'advanced' },
    { code: 'DRG', label: 'Dragoon', tier: 'advanced' },
    { code: 'SMN', label: 'Summoner', tier: 'advanced' }
];

const JOB_ALIASES: Record<string, string> = {
    war: 'WAR',
    warrior: 'WAR',
    mnk: 'MNK',
    monk: 'MNK',
    whm: 'WHM',
    'white mage': 'WHM',
    blm: 'BLM',
    'black mage': 'BLM',
    rdm: 'RDM',
    'red mage': 'RDM',
    thf: 'THF',
    thief: 'THF',
    pld: 'PLD',
    paladin: 'PLD',
    drk: 'DRK',
    'dark knight': 'DRK',
    bst: 'BST',
    beastmaster: 'BST',
    'beast master': 'BST',
    brd: 'BRD',
    bard: 'BRD',
    rng: 'RNG',
    ranger: 'RNG',
    sam: 'SAM',
    samurai: 'SAM',
    nin: 'NIN',
    ninja: 'NIN',
    drg: 'DRG',
    dragoon: 'DRG',
    smn: 'SMN',
    summoner: 'SMN'
};

@Component({
    selector: 'app-register',
    imports: [CommonModule, FormsModule],
    templateUrl: './register.component.html'
})
export class RegisterComponent implements OnInit, OnDestroy {
    private authService = inject(AuthService);
    private characterSearchService = inject(CharacterSearchService);
    private router = inject(Router);
    private characterSearchTimer: ReturnType<typeof setTimeout> | null = null;

    public displayName = '';
    public characterSearchRaw = '';
    public loading = signal(true);
    public submitting = signal(false);
    public errorMessage = signal<string | null>(null);
    public characterSearchError = signal<string | null>(null);
    public isSearchingCharacters = signal(false);
    public readonly selectedCharacters = signal<string[]>([]);
    public readonly searchResults = signal<CharacterSearchResult[]>([]);
    public readonly maxCharacters = 3;
    public readonly selectedPreferredJobs = signal<string[]>([]);
    public readonly basicJobs = JOB_OPTIONS.filter((job) => job.tier === 'basic');
    public readonly advancedJobs = JOB_OPTIONS.filter((job) => job.tier === 'advanced');

    public ngOnInit(): void {
        this.authService.getRegistrationContext().subscribe({
            next: (response: RegistrationContextResponse) => {
                if (response.isRegistrationComplete) {
                    this.router.navigate(['/app']);
                    return;
                }

                this.displayName = response.displayName ?? '';
                this.selectedPreferredJobs.set(this.normalizePreferredJobs(response.preferredJobs ?? []));
                this.selectedCharacters.set(this.normalizeCharacterNames(response.characterNames ?? []));
                this.loading.set(false);
            },
            error: () => {
                this.errorMessage.set('Could not load your registration context. Please retry from the home page.');
                this.loading.set(false);
            }
        });
    }

    public submit(): void {
        if (this.submitting()) {
            return;
        }

        const normalizedDisplayName = this.displayName.trim();
        if (!normalizedDisplayName) {
            this.errorMessage.set('Display name is required.');
            return;
        }

        this.errorMessage.set(null);
        this.submitting.set(true);

        this.authService.completeRegistration({
            displayName: normalizedDisplayName,
            preferredJobs: this.selectedPreferredJobs(),
            characterNames: this.selectedCharacters()
        }).subscribe({
            next: () => {
                this.router.navigate(['/app']);
            },
            error: () => {
                this.errorMessage.set('Registration could not be completed. Please try again.');
                this.submitting.set(false);
            }
        });
    }

    public ngOnDestroy(): void {
        if (this.characterSearchTimer) {
            clearTimeout(this.characterSearchTimer);
            this.characterSearchTimer = null;
        }
    }

    public onCharacterSearchInput(): void {
        const query = this.characterSearchRaw.trim();
        this.characterSearchError.set(null);

        if (this.characterSearchTimer) {
            clearTimeout(this.characterSearchTimer);
            this.characterSearchTimer = null;
        }

        if (query.length < 2) {
            this.searchResults.set([]);
            this.isSearchingCharacters.set(false);
            return;
        }

        this.characterSearchTimer = setTimeout(() => {
            this.isSearchingCharacters.set(true);

            this.characterSearchService.searchCharacters(query).subscribe({
                next: (response) => {
                    this.searchResults.set(response.results ?? []);
                    this.isSearchingCharacters.set(false);
                },
                error: (error: { error?: { message?: string } }) => {
                    this.characterSearchError.set(error.error?.message ?? 'Character search is unavailable right now.');
                    this.searchResults.set([]);
                    this.isSearchingCharacters.set(false);
                }
            });
        }, 400);
    }

    public addCharacter(characterName: string): void {
        const normalized = characterName.trim();
        if (!normalized) {
            return;
        }

        const existing = this.selectedCharacters();
        if (existing.some((name) => name.toLowerCase() === normalized.toLowerCase())) {
            return;
        }

        if (existing.length >= this.maxCharacters) {
            this.characterSearchError.set(`You can select up to ${this.maxCharacters} characters.`);
            return;
        }

        this.characterSearchError.set(null);
        this.selectedCharacters.set([...existing, normalized]);
    }

    public removeCharacter(characterName: string): void {
        this.selectedCharacters.set(
            this.selectedCharacters().filter((name) => name.toLowerCase() !== characterName.toLowerCase())
        );
    }

    public canAddCharacter(characterName: string): boolean {
        const selected = this.selectedCharacters();
        if (selected.length >= this.maxCharacters) {
            return false;
        }

        return !selected.some((name) => name.toLowerCase() === characterName.toLowerCase());
    }

    public getInitial(name: string): string {
        return name.trim().charAt(0).toUpperCase();
    }

    public togglePreferredJob(jobCode: string): void {
        const normalizedJobCode = jobCode.trim().toUpperCase();
        const current = this.selectedPreferredJobs();

        if (current.includes(normalizedJobCode)) {
            this.selectedPreferredJobs.set(current.filter((job) => job !== normalizedJobCode));
            return;
        }

        this.selectedPreferredJobs.set([...current, normalizedJobCode]);
    }

    public isPreferredJobSelected(jobCode: string): boolean {
        return this.selectedPreferredJobs().includes(jobCode);
    }

    private normalizePreferredJobs(preferredJobs: string[]): string[] {
        const normalized = preferredJobs
            .map((job) => JOB_ALIASES[job.trim().toLowerCase()] ?? job.trim().toUpperCase())
            .filter((job, index, all) => !!job && JOB_OPTIONS.some((option) => option.code === job) && all.indexOf(job) === index);

        return normalized;
    }

    private normalizeCharacterNames(characterNames: string[]): string[] {
        return characterNames
            .map((characterName) => characterName.trim())
            .filter((characterName, index, all) => !!characterName && all.findIndex((entry) => entry.toLowerCase() === characterName.toLowerCase()) === index)
            .slice(0, this.maxCharacters);
    }
}
