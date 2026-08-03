import { Injectable, inject, signal } from '@angular/core';
import { WorkspaceService } from './workspace.service';
import { MyWorkspaceSummary } from '../models/workspace.models';

/**
 * Shared, app-wide cache of "which workspaces does the current user belong to" (for the shell's workspace
 * switcher). A plain per-component fetch would go stale the moment a user accepts a pending invitation on the
 * Workspace page without touching the shell - callers that change membership (accept/leave) must call
 * refresh() so the switcher picks it up without requiring a manual browser refresh.
 */
@Injectable({ providedIn: 'root' })
export class WorkspaceContextService {
  private readonly workspaceService = inject(WorkspaceService);
  private readonly workspacesSignal = signal<MyWorkspaceSummary[]>([]);

  readonly workspaces = this.workspacesSignal.asReadonly();

  refresh(): void {
    this.workspaceService.getMyWorkspaces().subscribe({
      next: (workspaces) => this.workspacesSignal.set(workspaces),
      error: () => this.workspacesSignal.set([]),
    });
  }
}
