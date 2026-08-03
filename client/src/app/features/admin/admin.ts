import { Component, OnInit, inject, signal } from '@angular/core';
import { AdminService } from '../../core/services/admin.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { AdminUserSummary, AdminWorkspaceSummary, SystemHealth } from '../../core/models/admin.models';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './admin.html',
  styleUrl: './admin.scss',
})
export class Admin implements OnInit {
  private readonly adminService = inject(AdminService);

  readonly health = signal<SystemHealth | null>(null);
  readonly workspaces = signal<AdminWorkspaceSummary[]>([]);
  readonly users = signal<AdminUserSummary[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  private reload(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.adminService.getSystemHealth().subscribe({ next: (health) => this.health.set(health) });
    this.adminService.getWorkspaces().subscribe({ next: (workspaces) => this.workspaces.set(workspaces) });
    this.adminService.getUsers().subscribe({
      next: (users) => {
        this.users.set(users);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  toggleUserStatus(user: AdminUserSummary): void {
    const action = user.status === 'Disabled' ? this.adminService.enableUser(user.id) : this.adminService.disableUser(user.id);
    action.subscribe({
      next: () => this.reload(),
      error: () => this.errorMessage.set('error.generic'),
    });
  }
}
