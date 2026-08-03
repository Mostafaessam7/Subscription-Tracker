import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { ProblemDetails } from '../../../core/models/auth.models';
import { AuthBrandPanel } from '../../../shared/auth-brand-panel/auth-brand-panel';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, AuthBrandPanel],
  templateUrl: './login.html',
})
export class Login {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly requiresTwoFactor = signal(false);

  readonly form = this.formBuilder.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    totpCode: [''],
  });

  submit(): void {
    const isTotpStep = this.requiresTwoFactor();

    if (this.form.controls.email.invalid || this.form.controls.password.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    if (isTotpStep && !/^\d{6}$/.test(this.form.controls.totpCode.value)) {
      this.form.controls.totpCode.markAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.router.navigateByUrl('/dashboard');
      },
      error: (error: unknown) => {
        this.isSubmitting.set(false);

        if (error instanceof HttpErrorResponse) {
          const problem = error.error as ProblemDetails | undefined;
          if (problem?.title === 'Login.TwoFactorRequired') {
            this.requiresTwoFactor.set(true);
            this.errorMessage.set(null);
            return;
          }
          if (problem?.title === 'Login.InvalidTwoFactorCode') {
            this.errorMessage.set('auth.login.invalidTwoFactorCode');
            return;
          }
        }

        this.errorMessage.set(
          error instanceof HttpErrorResponse && error.status === 401 ? 'auth.login.error' : 'error.generic',
        );
      },
    });
  }
}
