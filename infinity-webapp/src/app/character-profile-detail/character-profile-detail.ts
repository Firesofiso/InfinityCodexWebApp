import { Component } from '@angular/core';

export enum Job {
  Warrior = 'Warrior',
  Monk = 'Monk',
  WhiteMage = 'White Mage',
  BlackMage = 'Black Mage',
  RedMage = 'Red Mage',
  Thief = 'Thief',  
  Paladin = 'Paladin',
  DarkKnight = 'Dark Knight',
  BeastMaser = 'Beast Master',
  Bard = 'Bard',
  Ranger = 'Ranger',
  Samurai = 'Samurai',
  Ninja = 'Ninja',
  Dragoon = 'Dragoon',
  Summoner = 'Summoner'
}
export class Character {
  name: string;
  jobLevels: { [jobName in Job]?: number };

  constructor(name: string, jobLevels: { [jobName in Job]?: number }) {
    this.name = name;
    this.jobLevels = jobLevels;
  }
}

@Component({
  selector: 'app-character-profile-detail',
  imports: [],
  templateUrl: './character-profile-detail.html',
  styleUrl: './character-profile-detail.css',
})
export class CharacterProfileDetail {
  public allJobs = Object.values(Job);
  public character = new Character('John Doe', {
    [Job.Warrior]: 75,
    [Job.Monk]: 75,
    [Job.WhiteMage]: 75,  
    [Job.BlackMage]: 75,
    [Job.RedMage]: 75,
    [Job.Thief]: 75,
    [Job.Paladin]: 75,
    [Job.DarkKnight]: 0,
    [Job.BeastMaser]: 75,
    [Job.Bard]: 75,
    [Job.Ranger]: 0,
    [Job.Samurai]: 75,
    [Job.Ninja]: 75,
    [Job.Dragoon]: 75,
    [Job.Summoner]: 75
  });
}
