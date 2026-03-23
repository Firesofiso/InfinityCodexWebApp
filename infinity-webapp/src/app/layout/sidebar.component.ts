import { Component } from '@angular/core';
import { HouseIcon, LucideAngularModule, OrbitIcon, SearchIcon, SettingsIcon } from 'lucide-angular';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [LucideAngularModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent {
    readonly Settings = SettingsIcon;
    readonly Home = HouseIcon;
    readonly Orbit = OrbitIcon;
}
