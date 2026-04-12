export interface MissionOption {
  value: string;
  label: string;
  number: string;
  name: string;
}

const mission = (number: string, name: string): MissionOption => ({
  number,
  name,
  value: `${number} - ${name}`,
  label: `${number} - ${name}`
});

export const SAN_DORIA_MISSIONS: MissionOption[] = [
  mission('1-1', 'Smash the Orcish Scouts'),
  mission('1-2', 'Bat Hunt'),
  mission('1-3', 'Save the Children'),
  mission('2-1', 'The Rescue Drill'),
  mission('2-2', 'The Davoi Report'),
  mission('2-3', 'Journey Abroad'),
  mission('3-1', 'Infiltrate Davoi'),
  mission('3-2', 'The Crystal Spring'),
  mission('3-3', 'Appointment to Jeuno'),
  mission('4-1', 'Magicite'),
  mission('5-1', "The Ruins of Fei'Yin"),
  mission('5-2', 'The Shadow Lord'),
  mission('6-1', "Leaute's Last Wishes"),
  mission('6-2', "Ranperre's Final Rest"),
  mission('7-1', 'Prestige of the Papsque'),
  mission('7-2', 'Secret Weapon'),
  mission('8-1', 'Coming of Age'),
  mission('8-2', 'Lightbringer'),
  mission('9-1', 'Breaking Barriers'),
  mission('9-2', 'The Heir to the Light')
];

export const BASTOK_MISSIONS: MissionOption[] = [
  mission('1-1', 'The Zeruhn Report'),
  mission('1-2', 'A Geological Survey'),
  mission('1-3', 'Fetichism'),
  mission('2-1', 'The Crystal Line'),
  mission('2-2', 'Wading Beasts'),
  mission('2-3', 'The Emissary'),
  mission('3-1', 'The Four Musketeers'),
  mission('3-2', 'To the Forsaken Mines'),
  mission('3-3', 'Jeuno'),
  mission('4-1', 'Magicite'),
  mission('5-1', 'Darkness Rising'),
  mission('5-2', 'Xarcabard, Land of Truths'),
  mission('6-1', 'Return of the Talekeeper'),
  mission('6-2', "The Pirates' Cove"),
  mission('7-1', 'The Final Image'),
  mission('7-2', 'On My Way'),
  mission('8-1', 'The Chains That Bind Us'),
  mission('8-2', 'Enter the Talekeeper'),
  mission('9-1', 'The Salt of the Earth'),
  mission('9-2', 'Where Two Paths Converge')
];

export const WINDURST_MISSIONS: MissionOption[] = [
  mission('1-1', 'The Horutoto Ruins Experiment'),
  mission('1-2', 'The Heart of the Matter'),
  mission('1-3', 'The Price of Peace'),
  mission('2-1', 'Lost for Words'),
  mission('2-2', 'A Testing Time'),
  mission('2-3', 'The Three Kingdoms'),
  mission('3-1', 'To Each His Own Right'),
  mission('3-2', 'Written in the Stars'),
  mission('3-3', 'A New Journey'),
  mission('4-1', 'Magicite'),
  mission('5-1', 'The Final Seal'),
  mission('5-2', 'The Shadow Awaits'),
  mission('6-1', 'Full Moon Fountain'),
  mission('6-2', 'Saintly Invitation'),
  mission('7-1', 'The Sixth Ministry'),
  mission('7-2', 'Awakening of the Gods'),
  mission('8-1', 'Vain'),
  mission('8-2', "The Jester Who'd Be King"),
  mission('9-1', 'Doll of the Dead'),
  mission('9-2', 'Moon Reading')
];

export const RISE_OF_THE_ZILART_MISSIONS: MissionOption[] = [
  mission('1', 'The New Frontier'),
  mission('2', "Welcome t'Norg"),
  mission('3', "Kazham's Chieftainness"),
  mission('4', 'The Temple of Uggalepih'),
  mission('5', 'Headstone Pilgrimage'),
  mission('6', 'Through the Quicksand Caves'),
  mission('7', 'The Chamber of Oracles'),
  mission('8', "Return to Delkfutt's Tower"),
  mission('9', "Ro'Maeve"),
  mission('10', 'The Temple of Desolation'),
  mission('11', 'The Hall of the Gods'),
  mission('12', 'The Mithra and the Crystal'),
  mission('13', 'The Gate of the Gods'),
  mission('14', 'Ark Angels (Divine Might)'),
  mission('15', 'The Sealed Shrine'),
  mission('16', 'The Celestial Nexus'),
  mission('17', 'Awakening')
];

export const CHAINS_OF_PROMATHIA_MISSIONS: MissionOption[] = [
  mission('1-1', 'The Rites of Life'),
  mission('1-2', 'Below the Arks'),
  mission('1-3', 'The Mothercrystals'),
  mission('2-1', 'An Invitation West'),
  mission('2-2', 'The Lost City'),
  mission('2-3', 'Distant Beliefs'),
  mission('2-4', 'An Eternal Melody'),
  mission('2-5', 'Ancient Vows'),
  mission('3-1', 'The Call of the Wyrmking'),
  mission('3-2', 'A Vessel Without a Captain'),
  mission('3-3', 'The Road Forks'),
  mission('3-4', 'Tending Aged Wounds'),
  mission('3-5', 'Darkness Named'),
  mission('4-1', 'Sheltering Doubt'),
  mission('4-2', 'The Savage'),
  mission('4-3', 'The Secrets of Worship'),
  mission('4-4', 'Slanderous Utterings'),
  mission('5-1', 'The Enduring Tumult of War'),
  mission('5-2', 'Desires of Emptiness'),
  mission('5-3A', "Three Paths - Louverance's Path"),
  mission('5-3B', "Three Paths - Tenzen's Path"),
  mission('5-3C', "Three Paths - Ulmia's Path"),
  mission('6-1', 'For Whom the Verse is Sung'),
  mission('6-2', 'A Place to Return'),
  mission('6-3', 'More Questions than Answers'),
  mission('6-4', 'One to be Feared'),
  mission('7-1', 'Chains and Bonds'),
  mission('7-2', 'Flames in the Darkness'),
  mission('7-3', 'Fire in the Eyes of Men'),
  mission('7-4', 'Calm Before the Storm'),
  mission('7-5', "The Warrior's Path"),
  mission('8-1', 'Garden of Antiquity'),
  mission('8-2', 'A Fate Decided'),
  mission('8-3', 'When Angels Fall'),
  mission('8-4', 'Dawn')
];

export const EPILOGUE_MISSIONS: MissionOption[] = [
  mission('E-1', 'Storms of Fate'),
  mission('E-2', 'Shadows of the Departed'),
  mission('E-3', 'Apocalypse Nigh'),
  mission('E-4', 'The Last Verse')
];
