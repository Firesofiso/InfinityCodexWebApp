import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { MainLayoutComponent } from './layout/main-layout.component';
import { authGuard } from './guards/auth.guard';
import { DashboardComponent } from './dashboard/dashboard.component';
import { RegisterComponent } from './register/register.component';
import { registrationGuard } from './guards/registration.guard';
import { CharacterWorkspaceComponent } from './character-workspace/character-workspace.component';

export const routes: Routes = [
    { path: '', component: HomeComponent },
    { path: 'register', component: RegisterComponent, canActivate: [registrationGuard] },
    {
        path: 'app',
        component: MainLayoutComponent,
        canActivate: [authGuard],
        children: [
            { path: '', redirectTo: 'characters', pathMatch: 'full' },
            { path: 'characters', component: CharacterWorkspaceComponent, data: { sectionLabel: 'Characters' } },
            { path: 'dashboard', component: DashboardComponent, data: { sectionLabel: 'Dashboard' } }
        ]
    },
    { path: '**', redirectTo: '' }
];
