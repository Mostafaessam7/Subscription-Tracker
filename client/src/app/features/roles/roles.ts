import { UpperCasePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RoleService } from '../../core/services/role.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { PermissionCatalogEntry, RoleDetail } from '../../core/models/role.models';

interface PermissionGroup {
  category: string;
  codes: string[];
}

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, UpperCasePipe],
  templateUrl: './roles.html',
  styleUrl: './roles.scss',
})
export class Roles implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly roleService = inject(RoleService);

  readonly roles = signal<RoleDetail[]>([]);
  readonly permissionCatalog = signal<PermissionCatalogEntry[]>([]);
  readonly selectedPermissionCodes = signal<string[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  private editingRoleId: string | null = null;

  readonly permissionGroups = computed<PermissionGroup[]>(() => {
    const groups = new Map<string, string[]>();
    for (const entry of this.permissionCatalog()) {
      const codes = groups.get(entry.category) ?? [];
      codes.push(entry.code);
      groups.set(entry.category, codes);
    }
    return [...groups.entries()].map(([category, codes]) => ({ category, codes }));
  });

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: [''],
  });

  ngOnInit(): void {
    this.roleService.getPermissionCatalog().subscribe({ next: (catalog) => this.permissionCatalog.set(catalog) });
    this.reload();
  }

  private reload(): void {
    this.isLoading.set(true);
    this.roleService.getWorkspaceRoles().subscribe({
      next: (roles) => {
        this.roles.set(roles);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  togglePermission(code: string): void {
    this.selectedPermissionCodes.update((codes) =>
      codes.includes(code) ? codes.filter((c) => c !== code) : [...codes, code],
    );
  }

  isPermissionSelected(code: string): boolean {
    return this.selectedPermissionCodes().includes(code);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    const raw = this.form.getRawValue();
    const request = {
      name: raw.name,
      description: raw.description || null,
      permissionCodes: this.selectedPermissionCodes(),
    };

    const done = () => {
      this.cancelEdit();
      this.reload();
    };

    if (this.editingRoleId) {
      this.roleService.updateRole(this.editingRoleId, request).subscribe({ next: done, error: () => this.errorMessage.set('error.generic') });
    } else {
      this.roleService.createRole(request).subscribe({ next: done, error: () => this.errorMessage.set('error.generic') });
    }
  }

  edit(role: RoleDetail): void {
    this.editingRoleId = role.id;
    this.form.setValue({ name: role.name, description: role.description ?? '' });
    this.selectedPermissionCodes.set([...role.permissions]);
  }

  cancelEdit(): void {
    this.editingRoleId = null;
    this.form.reset({ name: '', description: '' });
    this.selectedPermissionCodes.set([]);
  }

  delete(role: RoleDetail): void {
    this.roleService.deleteRole(role.id).subscribe({
      next: () => {
        if (this.editingRoleId === role.id) {
          this.cancelEdit();
        }
        this.reload();
      },
      error: () => this.errorMessage.set('error.generic'),
    });
  }

  get isEditing(): boolean {
    return this.editingRoleId !== null;
  }
}
