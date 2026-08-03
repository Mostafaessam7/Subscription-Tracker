import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { WorkspaceService } from '../../core/services/workspace.service';
import { WorkspaceContextService } from '../../core/services/workspace-context.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import {
  AssignableRole,
  PendingInvitation,
  Workspace as WorkspaceModel,
  WorkspaceMember,
} from '../../core/models/workspace.models';

@Component({
  selector: 'app-workspace',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe],
  templateUrl: './workspace.html',
  styleUrl: './workspace.scss',
})
export class Workspace implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly workspaceService = inject(WorkspaceService);
  private readonly workspaceContext = inject(WorkspaceContextService);

  readonly workspace = signal<WorkspaceModel | null>(null);
  readonly assignableRoles = signal<AssignableRole[]>([]);
  readonly pendingInvitations = signal<PendingInvitation[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly inviteSuccess = signal(false);

  readonly settingsForm = this.formBuilder.nonNullable.group({
    defaultCurrencyCode: ['USD', [Validators.required, Validators.maxLength(3)]],
    timeZoneId: ['UTC', [Validators.required]],
    locale: ['en-US', [Validators.required]],
  });

  readonly inviteForm = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    roleId: ['', [Validators.required]],
  });

  ngOnInit(): void {
    this.reload();
    this.workspaceService.getAssignableRoles().subscribe({ next: (roles) => this.assignableRoles.set(roles) });
    this.workspaceService.getPendingInvitations().subscribe({ next: (invitations) => this.pendingInvitations.set(invitations) });
  }

  private reload(): void {
    this.isLoading.set(true);
    this.workspaceService.getMyWorkspace().subscribe({
      next: (workspace) => {
        this.workspace.set(workspace);
        this.settingsForm.setValue({
          defaultCurrencyCode: workspace.defaultCurrencyCode,
          timeZoneId: workspace.timeZoneId,
          locale: workspace.locale,
        });
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  submitSettings(): void {
    if (this.settingsForm.invalid) {
      this.settingsForm.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.workspaceService.updateSettings(this.settingsForm.getRawValue()).subscribe({
      next: () => this.reload(),
      error: () => this.errorMessage.set('error.generic'),
    });
  }

  submitInvite(): void {
    if (this.inviteForm.invalid) {
      this.inviteForm.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    this.inviteSuccess.set(false);
    this.workspaceService.inviteMember(this.inviteForm.getRawValue()).subscribe({
      next: () => {
        this.inviteForm.reset({ email: '', roleId: '' });
        this.inviteSuccess.set(true);
        this.reload();
      },
      error: () => this.errorMessage.set('error.generic'),
    });
  }

  changeMemberRole(member: WorkspaceMember, roleId: string): void {
    this.workspaceService.changeMemberRole(member.memberId, roleId).subscribe({
      next: () => this.reload(),
      error: () => this.errorMessage.set('error.generic'),
    });
  }

  removeMember(member: WorkspaceMember): void {
    this.workspaceService.removeMember(member.memberId).subscribe({
      next: () => this.reload(),
      error: () => this.errorMessage.set('error.generic'),
    });
  }

  acceptInvitation(invitation: PendingInvitation): void {
    this.workspaceService.acceptInvitation(invitation.memberId).subscribe({
      next: () => {
        this.pendingInvitations.update((list) => list.filter((i) => i.memberId !== invitation.memberId));
        this.workspaceContext.refresh();
      },
      error: () => this.errorMessage.set('error.generic'),
    });
  }

  isOwner(member: WorkspaceMember): boolean {
    return this.workspace()?.ownerId === member.userId;
  }
}
