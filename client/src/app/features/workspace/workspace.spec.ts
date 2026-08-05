import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Workspace } from './workspace';
import { WorkspaceService } from '../../core/services/workspace.service';
import { WorkspaceContextService } from '../../core/services/workspace-context.service';
import { AssignableRole, PendingInvitation, Workspace as WorkspaceModel, WorkspaceMember } from '../../core/models/workspace.models';

function fakeWorkspace(overrides: Partial<WorkspaceModel> = {}): WorkspaceModel {
  return {
    id: 'ws-1',
    name: "Mostafa's Workspace",
    ownerId: 'user-owner',
    defaultCurrencyCode: 'USD',
    timeZoneId: 'UTC',
    locale: 'en-US',
    members: [],
    ...overrides,
  };
}

function fakeMember(overrides: Partial<WorkspaceMember> = {}): WorkspaceMember {
  return {
    memberId: 'member-1',
    userId: 'user-1',
    email: 'a@example.com',
    firstName: 'Ada',
    lastName: 'Lovelace',
    roleId: 'role-member',
    roleName: 'Member',
    status: 'Active',
    ...overrides,
  };
}

function createWorkspace(
  workspaceOverrides: Partial<WorkspaceModel> = {},
  serviceOverrides: Partial<Record<string, ReturnType<typeof vi.fn>>> = {},
): Workspace {
  TestBed.configureTestingModule({
    providers: [
      {
        provide: WorkspaceService,
        useValue: {
          getMyWorkspace: vi.fn().mockReturnValue(of(fakeWorkspace(workspaceOverrides))),
          getAssignableRoles: vi.fn().mockReturnValue(of([] as AssignableRole[])),
          getPendingInvitations: vi.fn().mockReturnValue(of([] as PendingInvitation[])),
          updateSettings: vi.fn().mockReturnValue(of(undefined)),
          inviteMember: vi.fn().mockReturnValue(of(undefined)),
          changeMemberRole: vi.fn().mockReturnValue(of(undefined)),
          removeMember: vi.fn().mockReturnValue(of(undefined)),
          acceptInvitation: vi.fn().mockReturnValue(of(undefined)),
          ...serviceOverrides,
        },
      },
      { provide: WorkspaceContextService, useValue: { refresh: vi.fn() } },
    ],
  });

  const component = TestBed.runInInjectionContext(() => new Workspace());
  component.ngOnInit();
  return component;
}

describe('Workspace', () => {
  it('loads the workspace and seeds the settings form from it', () => {
    const workspace = createWorkspace({ defaultCurrencyCode: 'EUR', timeZoneId: 'Europe/Paris', locale: 'fr-FR' });

    expect(workspace.workspace()?.defaultCurrencyCode).toBe('EUR');
    expect(workspace.settingsForm.controls.defaultCurrencyCode.value).toBe('EUR');
    expect(workspace.settingsForm.controls.timeZoneId.value).toBe('Europe/Paris');
    expect(workspace.settingsForm.controls.locale.value).toBe('fr-FR');
  });

  it('does not submit an invalid settings form', () => {
    const updateSettings = vi.fn().mockReturnValue(of(undefined));
    const workspace = createWorkspace({}, { updateSettings });

    workspace.settingsForm.controls.defaultCurrencyCode.setValue('');
    workspace.submitSettings();

    expect(updateSettings).not.toHaveBeenCalled();
  });

  it('identifies the owner by matching ownerId against a member userId', () => {
    const owner = fakeMember({ memberId: 'm-owner', userId: 'user-owner' });
    const nonOwner = fakeMember({ memberId: 'm-other', userId: 'user-other' });
    const workspace = createWorkspace({ ownerId: 'user-owner', members: [owner, nonOwner] });

    expect(workspace.isOwner(owner)).toBe(true);
    expect(workspace.isOwner(nonOwner)).toBe(false);
  });

  it('builds initials from a first and last name', () => {
    const workspace = createWorkspace();

    expect(workspace.initials('Ada', 'Lovelace')).toBe('AL');
    expect(workspace.initials('', '')).toBe('');
  });

  it('does not submit an invalid (missing role) invite form', () => {
    const inviteMember = vi.fn().mockReturnValue(of(undefined));
    const workspace = createWorkspace({}, { inviteMember });

    workspace.inviteForm.controls.email.setValue('teammate@example.com');
    workspace.submitInvite();

    expect(inviteMember).not.toHaveBeenCalled();
  });

  it('invites a member, resets the form, and surfaces a success flag', () => {
    const inviteMember = vi.fn().mockReturnValue(of(undefined));
    const workspace = createWorkspace({}, { inviteMember });

    workspace.inviteForm.setValue({ email: 'teammate@example.com', roleId: 'role-1' });
    workspace.submitInvite();

    expect(inviteMember).toHaveBeenCalledWith({ email: 'teammate@example.com', roleId: 'role-1' });
    expect(workspace.inviteSuccess()).toBe(true);
    expect(workspace.inviteForm.controls.email.value).toBe('');
  });

  it('sets a generic error and does not flag success when inviting fails', () => {
    const inviteMember = vi.fn().mockReturnValue(throwError(() => new Error('boom')));
    const workspace = createWorkspace({}, { inviteMember });

    workspace.inviteForm.setValue({ email: 'teammate@example.com', roleId: 'role-1' });
    workspace.submitInvite();

    expect(workspace.errorMessage()).toBe('error.generic');
    expect(workspace.inviteSuccess()).toBe(false);
  });

  it('refreshes the shared workspace context after accepting an invitation', () => {
    const acceptInvitation = vi.fn().mockReturnValue(of(undefined));
    const refresh = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        {
          provide: WorkspaceService,
          useValue: {
            getMyWorkspace: vi.fn().mockReturnValue(of(fakeWorkspace())),
            getAssignableRoles: vi.fn().mockReturnValue(of([])),
            getPendingInvitations: vi.fn().mockReturnValue(of([])),
            acceptInvitation,
          },
        },
        { provide: WorkspaceContextService, useValue: { refresh } },
      ],
    });
    const workspace = TestBed.runInInjectionContext(() => new Workspace());
    workspace.ngOnInit();

    const invitation: PendingInvitation = {
      workspaceId: 'ws-2',
      workspaceName: 'Other Workspace',
      memberId: 'member-9',
      roleName: 'Viewer',
    };
    workspace.pendingInvitations.set([invitation]);

    workspace.acceptInvitation(invitation);

    expect(acceptInvitation).toHaveBeenCalledWith('member-9');
    expect(refresh).toHaveBeenCalled();
    expect(workspace.pendingInvitations()).toEqual([]);
  });
});
