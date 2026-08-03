import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { AuthBrandPanel } from '../../../shared/auth-brand-panel/auth-brand-panel';

function passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('newPassword')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return password === confirmPassword ? null : { passwordsMismatch: true };
}

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe, AuthBrandPanel],
  templateUrl: './reset-password.html',
})
export class ResetPassword implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  private userId: string | null = null;
  private token: string | null = null;

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly missingParams = signal(false);
  readonly succeeded = signal(false);

  readonly form = this.formBuilder.nonNullable.group(
    {
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', [Validators.required]],
    },
    { validators: passwordsMatchValidator },
  );

  ngOnInit(): void {
    this.userId = this.route.snapshot.queryParamMap.get('userId');
    this.token = this.route.snapshot.queryParamMap.get('token');
    this.missingParams.set(!this.userId || !this.token);
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting() || !this.userId || !this.token) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.authService
      .resetPassword({
        userId: this.userId,
        token: this.token,
        newPassword: this.form.getRawValue().newPassword,
      })
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.succeeded.set(true);
        },
        error: (error: unknown) => {
          this.isSubmitting.set(false);
          this.errorMessage.set(
            error instanceof HttpErrorResponse && error.status === 400
              ? 'auth.resetPassword.error'
              : 'error.generic',
          );
        },
      });
  }
}
