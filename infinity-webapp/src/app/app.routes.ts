import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { MainLayoutComponent } from './layout/main-layout.component';
import { authGuard } from './guards/auth.guard';
import { DashboardComponent } from '@containers/dashboard/dashboard.component';
import { RegisterComponent } from '@containers/register/register.component';
import { registrationGuard } from './guards/registration.guard';
import { CharacterWorkspaceComponent } from '@containers/character-workspace/character-workspace.component';
import { ItemCatalogComponent } from '@containers/item-catalog/item-catalog.component';
import { RosterComponent } from '@containers/roster/roster.component';
import { AccessManagementComponent } from '@containers/access-management/access-management.component';

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
            { path: 'characters/:userId', component: CharacterWorkspaceComponent, data: { sectionLabel: 'Character' } },
            { path: 'roster', component: RosterComponent, data: { sectionLabel: 'Roster' } },
            { path: 'access', component: AccessManagementComponent, data: { sectionLabel: 'Roles & Permissions' } },
            { path: 'catalog', component: ItemCatalogComponent, data: { sectionLabel: 'Item Catalog' } },
            { path: 'dashboard', component: DashboardComponent, data: { sectionLabel: 'Dashboard' } }
        ]
    },
    { path: '**', redirectTo: '' }
];
