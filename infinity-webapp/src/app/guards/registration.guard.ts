import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '@services/auth.service';

export const registrationGuard: CanActivateFn = () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (authService.hasPendingRegistrationSession()) {
        return of(true);
    }

    if (authService.hasAuthenticatedSession()) {
        return of(router.createUrlTree(['/app']));
    }

    return authService.refreshSession().pipe(
        map((session) => {
            if (!session.isAuthenticated) {
                return router.createUrlTree(['/']);
            }

            const registrationStatus = session.registrationStatus ?? (session.isRegistrationComplete ? 'complete' : 'pending');
            return registrationStatus === 'pending'
                ? true
                : router.createUrlTree(['/app']);
        }),
        catchError(() => of(router.createUrlTree(['/'])))
    );
};