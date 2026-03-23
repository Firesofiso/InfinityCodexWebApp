import { Component, OnInit, signal } from "@angular/core";
import { AuthService } from "../../services/auth.service";
import { CharacterProfileDetail } from "../character-profile-detail/character-profile-detail";
import { FullCalendarModule } from '@fullcalendar/angular';
import { CalendarOptions } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';

@Component({
    selector: 'app-home',
    imports: [CharacterProfileDetail, FullCalendarModule],
    templateUrl: './home.component.html'
})
export class HomeComponent implements OnInit {
    private authService: AuthService = new AuthService();
    public isLoggedIn = signal<boolean>(false);
    calendarOptions: CalendarOptions = {
        initialView: 'dayGridMonth',
        plugins: [dayGridPlugin]
    };

    public ngOnInit(): void {
        this.checkAuth();
    }

    public checkAuth() {
        this.authService.checkAuth().subscribe({
            next: (response) => {
                this.isLoggedIn.set(true);
                console.log('Login successful:', response);
            },
            error: (error) => {
                this.isLoggedIn.set(false);
                console.error('Login failed:', error);
            }
        });
    }

    public discordLogin() { 
        window.location.href = 'https://unrealistic-skyla-demagogically.ngrok-free.dev/auth/discord/login';
        // this.authService.discordLogin().subscribe({
        //     next: (response) => {
        //         console.log('Login successful:', response);
        //         // Handle successful login, e.g., navigate to dashboard
        //     },
        //     error: (error) => {
        //         console.error('Login failed:', error);
        //         // Handle login failure, e.g., show error message
        //     }
        // });
    }
}