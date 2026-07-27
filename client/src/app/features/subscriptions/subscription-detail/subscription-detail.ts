import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SubscriptionService } from '../../../core/services/subscription.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { Subscription, SubscriptionStatus } from '../../../core/models/subscription.models';

@Component({
  selector: 'app-subscription-detail',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './subscription-detail.html',
  styleUrl: './subscription-detail.scss',
})
export class SubscriptionDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly subscriptionService = inject(SubscriptionService);

  protected readonly SubscriptionStatus = SubscriptionStatus;

  readonly subscription = signal<Subscription | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly isActionInProgress = signal(false);

  private id!: string;

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id')!;
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.subscriptionService.getById(this.id).subscribe({
      next: (subscription) => {
        this.subscription.set(subscription);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  pause(): void {
    this.isActionInProgress.set(true);
    this.subscriptionService.pause(this.id).subscribe({
      next: () => {
        this.isActionInProgress.set(false);
        this.load();
      },
      error: () => {
        this.isActionInProgress.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  resume(): void {
    this.isActionInProgress.set(true);
    this.subscriptionService.resume(this.id).subscribe({
      next: () => {
        this.isActionInProgress.set(false);
        this.load();
      },
      error: () => {
        this.isActionInProgress.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  cancel(): void {
    this.isActionInProgress.set(true);
    const today = new Date().toISOString().slice(0, 10);
    this.subscriptionService.cancel(this.id, { effectiveDate: today, reason: null }).subscribe({
      next: () => {
        this.isActionInProgress.set(false);
        this.load();
      },
      error: () => {
        this.isActionInProgress.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  goToEdit(): void {
    void this.router.navigate(['/subscriptions', this.id, 'edit']);
  }
}
