import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterOutlet } from '@angular/router';
import { NavigationEnd } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { SidebarComponent } from './sidebar.component';

@Component({
  selector: 'app-main-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent],
  templateUrl: './main-layout.component.html',
  styleUrl: './main-layout.component.css'
})
export class MainLayoutComponent implements OnInit, OnDestroy {
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly subscriptions = new Subscription();

    protected readonly currentSection = signal('Dashboard');

    public ngOnInit(): void {
      this.updateCurrentSection();

      const navigationSubscription = this.router.events.pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd)
      ).subscribe(() => {
        this.updateCurrentSection();
      });

      this.subscriptions.add(navigationSubscription);
    }

    public ngOnDestroy(): void {
      this.subscriptions.unsubscribe();
    }

    private updateCurrentSection(): void {
      let currentRoute: ActivatedRoute | null = this.route;

      while (currentRoute?.firstChild) {
        currentRoute = currentRoute.firstChild;
      }

      const sectionLabel = currentRoute?.snapshot.data['sectionLabel'];
      this.currentSection.set(typeof sectionLabel === 'string' && sectionLabel.length > 0 ? sectionLabel : 'Dashboard');
    }
}
