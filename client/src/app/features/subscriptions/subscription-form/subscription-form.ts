import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SubscriptionService } from '../../../core/services/subscription.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { BillingFrequency } from '../../../core/models/subscription.models';

@Component({
  selector: 'app-subscription-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, TranslatePipe],
  templateUrl: './subscription-form.html',
  styleUrl: './subscription-form.scss',
})
export class SubscriptionForm implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly subscriptionService = inject(SubscriptionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly BillingFrequency = BillingFrequency;
  protected readonly billingFrequencyOptions = [
    BillingFrequency.Weekly,
    BillingFrequency.Monthly,
    BillingFrequency.Quarterly,
    BillingFrequency.Yearly,
    BillingFrequency.Custom,
    BillingFrequency.Lifetime,
  ];

  readonly isEditMode = signal(false);
  readonly isSubmitting = signal(false);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  private subscriptionId: string | null = null;

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required]],
    provider: ['', [Validators.required]],
    websiteUrl: [''],
    notes: [''],
    categoryId: [''],
    paymentMethodId: [''],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    currencyCode: ['USD', [Validators.required]],
    billingFrequency: [BillingFrequency.Monthly, [Validators.required]],
    customIntervalDays: [null as number | null],
    startDate: [new Date().toISOString().slice(0, 10), [Validators.required]],
    trialEndDate: [''],
    autoRenewal: [true],
  });

  ngOnInit(): void {
    this.subscriptionId = this.route.snapshot.paramMap.get('id');
    this.isEditMode.set(this.subscriptionId !== null);

    if (this.subscriptionId) {
      this.isLoading.set(true);
      this.subscriptionService.getById(this.subscriptionId).subscribe({
        next: (subscription) => {
          this.form.patchValue({
            name: subscription.name,
            provider: subscription.provider,
            websiteUrl: subscription.websiteUrl ?? '',
            notes: subscription.notes ?? '',
            categoryId: subscription.categoryId ?? '',
            paymentMethodId: subscription.paymentMethodId ?? '',
            amount: subscription.amount,
            currencyCode: subscription.currencyCode,
            billingFrequency: subscription.billingFrequency,
            customIntervalDays: subscription.customIntervalDays,
            startDate: subscription.startDate,
            trialEndDate: subscription.trialEndDate ?? '',
            autoRenewal: subscription.autoRenewal,
          });
          // Billing frequency/start date are immutable after creation on the backend
          // (UpdateSubscriptionCommand doesn't accept them), so lock the fields down in edit mode.
          this.form.controls.billingFrequency.disable();
          this.form.controls.startDate.disable();
          this.form.controls.customIntervalDays.disable();
          this.form.controls.trialEndDate.disable();
          this.form.controls.autoRenewal.disable();
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
          this.errorMessage.set('error.generic');
        },
      });
    }
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    const raw = this.form.getRawValue();

    const shared = {
      name: raw.name,
      provider: raw.provider,
      websiteUrl: raw.websiteUrl || null,
      notes: raw.notes || null,
      categoryId: raw.categoryId || null,
      paymentMethodId: raw.paymentMethodId || null,
      amount: raw.amount,
      currencyCode: raw.currencyCode,
    };

    const onError = (error: unknown): void => {
      this.isSubmitting.set(false);
      this.errorMessage.set(
        error instanceof HttpErrorResponse && error.status === 400 ? 'subscriptions.form.invalid' : 'error.generic',
      );
    };

    if (this.isEditMode() && this.subscriptionId) {
      const id = this.subscriptionId;
      this.subscriptionService.update(id, { ...shared, tagIds: [] }).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          void this.router.navigate(['/subscriptions', id]);
        },
        error: onError,
      });
    } else {
      this.subscriptionService
        .create({
          ...shared,
          billingFrequency: raw.billingFrequency,
          customIntervalDays: raw.customIntervalDays,
          startDate: raw.startDate,
          trialEndDate: raw.trialEndDate || null,
          autoRenewal: raw.autoRenewal,
          tagIds: [],
        })
        .subscribe({
          next: (id) => {
            this.isSubmitting.set(false);
            void this.router.navigate(['/subscriptions', id]);
          },
          error: onError,
        });
    }
  }
}
