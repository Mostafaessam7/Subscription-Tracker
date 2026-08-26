import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { SecurityService } from '../../core/services/security.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { CurrentUser, Session, SetupTwoFactorResponse } from '../../core/models/security.models';

@Component({
  selector: 'app-security',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, DatePipe],
  templateUrl: './security.html',
  styleUrl: './security.scss',
})
export class Security implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly securityService = inject(SecurityService);

  readonly currentUser = signal<CurrentUser | null>(null);
  readonly sessions = signal<Session[]>([]);
  readonly setupInfo = signal<SetupTwoFactorResponse | null>(null);
  readonly recoveryCodes = signal<string[] | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly isLoading = signal(true);
  readonly isSubmitting = signal(false);

  readonly enableForm = this.formBuilder.nonNullable.group({
    code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
  });

  readonly disableForm = this.formBuilder.nonNullable.group({
    code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
  });

  ngOnInit(): void {
    this.reload();
    this.securityService.getSessions().subscribe({ next: (sessions) => this.sessions.set(sessions) });
  }

  private reload(): void {
    this.isLoading.set(true);
    this.securityService.getCurrentUser().subscribe({
      next: (user) => {
        this.currentUser.set(user);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  startSetup(): void {
    this.errorMessage.set(null);
    this.securityService.setupTwoFactor().subscribe({
      next: (info) => this.setupInfo.set(info),
      error: () => this.errorMessage.set('error.generic'),
    });
  }

  cancelSetup(): void {
    this.setupInfo.set(null);
    this.enableForm.reset({ code: '' });
  }

  confirmEnable(): void {
    if (this.enableForm.invalid || !this.setupInfo()) {
      this.enableForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.securityService.enableTwoFactor(this.setupInfo()!.secret, this.enableForm.getRawValue().code).subscribe({
      next: (response) => {
        this.isSubmitting.set(false);
        this.setupInfo.set(null);
        this.enableForm.reset({ code: '' });
        this.successMessage.set('security.twoFactor.enabled');
        // Shown once, here - the backend never returns these again after this call, so the user must save
        // them now (or lose the ability to recover the account if their authenticator device is ever lost).
        this.recoveryCodes.set(response.recoveryCodes);
        this.reload();
      },
      error: (error: unknown) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(
          error instanceof HttpErrorResponse && error.status === 400 ? 'security.twoFactor.invalidCode' : 'error.generic',
        );
      },
    });
  }

  acknowledgeRecoveryCodes(): void {
    this.recoveryCodes.set(null);
  }

  confirmDisable(): void {
    if (this.disableForm.invalid) {
      this.disableForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.securityService.disableTwoFactor(this.disableForm.getRawValue().code).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.disableForm.reset({ code: '' });
        this.successMessage.set('security.twoFactor.disabled');
        this.reload();
      },
      error: (error: unknown) => {
        this.isSubmitting.set(false);
        this.errorMessage.set(
          error instanceof HttpErrorResponse && error.status === 400 ? 'security.twoFactor.invalidCode' : 'error.generic',
        );
      },
    });
  }

  revokeSession(session: Session): void {
    this.securityService.revokeSession(session.id).subscribe({
      next: () => this.sessions.update((list) => list.filter((s) => s.id !== session.id)),
      error: () => this.errorMessage.set('error.generic'),
    });
  }
}
