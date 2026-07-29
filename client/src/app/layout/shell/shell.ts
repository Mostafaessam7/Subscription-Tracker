import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';
import { TranslationService } from '../../core/services/translation.service';
import { PermissionsService } from '../../core/services/permissions.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { Permissions } from '../../core/models/permissions';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly themeService = inject(ThemeService);
  protected readonly translationService = inject(TranslationService);
  protected readonly permissions = inject(PermissionsService);
  protected readonly Permissions = Permissions;

  readonly isNavOpen = signal(false);

  logout(): void {
    this.authService.logout().subscribe({
      complete: () => this.router.navigateByUrl('/auth/login'),
    });
  }

  switchLocale(): void {
    void this.translationService.setLocale(this.translationService.locale() === 'en' ? 'ar' : 'en');
  }

  toggleNav(): void {
    this.isNavOpen.update((open) => !open);
  }

  closeNav(): void {
    this.isNavOpen.set(false);
  }
}
