import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, catchError, of } from 'rxjs';
import { AuthService } from '../../services/auth.service';

export const authGuard: CanActivateFn = () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (authService.hasAuthenticatedSession()) {
        return of(true);
    }

    return authService.refreshSession().pipe(
        map((session) => {
            if (session.isAuthenticated && (session.registrationStatus ?? (session.isRegistrationComplete ? 'complete' : 'pending')) === 'complete') {
                return true;
            }

            if (session.isAuthenticated) {
                return router.createUrlTree(['/register']);
            }

            return router.createUrlTree(['/']);
        }),
        catchError(() => of(router.createUrlTree(['/'])))
    );
};
