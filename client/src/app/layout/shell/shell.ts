import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';
import { TranslationService } from '../../core/services/translation.service';
import { PermissionsService } from '../../core/services/permissions.service';
import { WorkspaceContextService } from '../../core/services/workspace-context.service';
import { NotificationService } from '../../core/services/notification.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { Permissions } from '../../core/models/permissions';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, TranslatePipe, DatePipe],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly authService = inject(AuthService);
  private readonly workspaceContext = inject(WorkspaceContextService);
  private readonly router = inject(Router);

  protected readonly themeService = inject(ThemeService);
  protected readonly translationService = inject(TranslationService);
  protected readonly permissions = inject(PermissionsService);
  protected readonly notificationService = inject(NotificationService);
  protected readonly Permissions = Permissions;

  readonly isNavOpen = signal(false);
  readonly isWorkspaceMenuOpen = signal(false);
  readonly isNotificationMenuOpen = signal(false);
  readonly workspaces = this.workspaceContext.workspaces;
  readonly isSwitchingWorkspace = signal(false);

  constructor() {
    this.workspaceContext.refresh();
    this.notificationService.connect();
  }

  toggleNotificationMenu(): void {
    this.isNotificationMenuOpen.update((open) => !open);
  }

  get currentWorkspaceName(): string | null {
    return this.workspaces().find((w) => w.isCurrent)?.name ?? null;
  }

  logout(): void {
    this.notificationService.disconnect();
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

  toggleWorkspaceMenu(): void {
    this.isWorkspaceMenuOpen.update((open) => !open);
  }

  switchWorkspace(workspaceId: string): void {
    if (this.isSwitchingWorkspace()) {
      return;
    }

    this.isSwitchingWorkspace.set(true);
    this.authService.switchWorkspace(workspaceId).subscribe({
      next: () => {
        // Full navigation (not the router) so every component re-fetches under the new workspace context
        // instead of trying to patch already-loaded state for the previous workspace in place.
        window.location.assign('/dashboard');
      },
      error: () => this.isSwitchingWorkspace.set(false),
    });
  }
}
