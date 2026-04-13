import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { BookOpenIcon, HouseIcon, LucideAngularModule, OrbitIcon, SettingsIcon, SwordsIcon } from 'lucide-angular';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [LucideAngularModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent {
    readonly Settings = SettingsIcon;
    readonly Home = HouseIcon;
    readonly Orbit = OrbitIcon;
    readonly Swords = SwordsIcon;
    readonly BookOpen = BookOpenIcon;
}
