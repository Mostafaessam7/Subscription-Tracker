import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Roles } from './roles';
import { RoleService } from '../../core/services/role.service';
import { PermissionCatalogEntry, RoleDetail } from '../../core/models/role.models';

function fakeRole(overrides: Partial<RoleDetail> = {}): RoleDetail {
  return { id: 'role-1', name: 'Member', description: 'Standard member', isSystemRole: false, permissions: ['subscriptions:view'], ...overrides };
}

function createRoles(
  roleServiceOverrides: Partial<Record<string, ReturnType<typeof vi.fn>>> = {},
  catalog: PermissionCatalogEntry[] = [],
): Roles {
  TestBed.configureTestingModule({
    providers: [
      {
        provide: RoleService,
        useValue: {
          getPermissionCatalog: vi.fn().mockReturnValue(of(catalog)),
          getWorkspaceRoles: vi.fn().mockReturnValue(of([])),
          createRole: vi.fn().mockReturnValue(of('new-role-id')),
          updateRole: vi.fn().mockReturnValue(of(undefined)),
          deleteRole: vi.fn().mockReturnValue(of(undefined)),
          ...roleServiceOverrides,
        },
      },
    ],
  });

  const component = TestBed.runInInjectionContext(() => new Roles());
  component.ngOnInit();
  return component;
}

describe('Roles', () => {
  it('loads roles and stops loading', () => {
    const roles = createRoles({ getWorkspaceRoles: vi.fn().mockReturnValue(of([fakeRole()])) });

    expect(roles.isLoading()).toBe(false);
    expect(roles.roles().length).toBe(1);
  });

  it('sets a generic error when loading roles fails', () => {
    const roles = createRoles({ getWorkspaceRoles: vi.fn().mockReturnValue(throwError(() => new Error('boom'))) });

    expect(roles.isLoading()).toBe(false);
    expect(roles.errorMessage()).toBe('error.generic');
  });

  it('groups the permission catalog by category', () => {
    const roles = createRoles({}, [
      { code: 'subscriptions:view', category: 'subscriptions' },
      { code: 'subscriptions:edit', category: 'subscriptions' },
      { code: 'budgets:view', category: 'budgets' },
    ]);

    const groups = roles.permissionGroups();

    expect(groups).toEqual([
      { category: 'subscriptions', codes: ['subscriptions:view', 'subscriptions:edit'] },
      { category: 'budgets', codes: ['budgets:view'] },
    ]);
  });

  describe('togglePermission', () => {
    it('adds an unselected code and removes an already-selected one', () => {
      const roles = createRoles();

      roles.togglePermission('budgets:manage');
      expect(roles.isPermissionSelected('budgets:manage')).toBe(true);

      roles.togglePermission('budgets:manage');
      expect(roles.isPermissionSelected('budgets:manage')).toBe(false);
    });
  });

  describe('edit / cancelEdit', () => {
    it('seeds the form and permission selection from the role being edited', () => {
      const roles = createRoles();
      const role = fakeRole({ name: 'Custom', description: 'desc', permissions: ['a:b', 'c:d'] });

      roles.edit(role);

      expect(roles.isEditing).toBe(true);
      expect(roles.form.controls.name.value).toBe('Custom');
      expect(roles.selectedPermissionCodes()).toEqual(['a:b', 'c:d']);
    });

    it('clears edit state, the form, and the permission selection on cancel', () => {
      const roles = createRoles();
      roles.edit(fakeRole({ permissions: ['a:b'] }));

      roles.cancelEdit();

      expect(roles.isEditing).toBe(false);
      expect(roles.form.controls.name.value).toBe('');
      expect(roles.selectedPermissionCodes()).toEqual([]);
    });
  });

  describe('submit', () => {
    it('does not submit an invalid (empty-name) form', () => {
      const createRole = vi.fn().mockReturnValue(of('id'));
      const roles = createRoles({ createRole });

      roles.submit();

      expect(createRole).not.toHaveBeenCalled();
      expect(roles.form.controls.name.touched).toBe(true);
    });

    it('creates a role with the selected permissions and a null description when blank', () => {
      const createRole = vi.fn().mockReturnValue(of('id'));
      const roles = createRoles({ createRole });

      roles.form.setValue({ name: 'Analyst', description: '' });
      roles.togglePermission('reports:view');
      roles.submit();

      expect(createRole).toHaveBeenCalledWith({ name: 'Analyst', description: null, permissionCodes: ['reports:view'] });
    });

    it('calls updateRole (not create) while editing an existing role', () => {
      const updateRole = vi.fn().mockReturnValue(of(undefined));
      const roles = createRoles({ updateRole });
      roles.edit(fakeRole({ id: 'role-9' }));

      roles.form.patchValue({ name: 'Renamed' });
      roles.submit();

      expect(updateRole).toHaveBeenCalledWith('role-9', expect.objectContaining({ name: 'Renamed' }));
      expect(roles.isEditing).toBe(false);
    });
  });

  describe('delete', () => {
    it('exits edit mode when deleting the role currently being edited', () => {
      const roles = createRoles();
      const role = fakeRole({ id: 'role-1' });
      roles.edit(role);

      roles.delete(role);

      expect(roles.isEditing).toBe(false);
    });

    it('leaves edit state untouched when deleting a different role', () => {
      const roles = createRoles();
      roles.edit(fakeRole({ id: 'role-being-edited' }));

      roles.delete(fakeRole({ id: 'some-other-role' }));

      expect(roles.isEditing).toBe(true);
    });

    it('sets a generic error when deleting fails', () => {
      const roles = createRoles({ deleteRole: vi.fn().mockReturnValue(throwError(() => new Error('boom'))) });

      roles.delete(fakeRole());

      expect(roles.errorMessage()).toBe('error.generic');
    });
  });
});
