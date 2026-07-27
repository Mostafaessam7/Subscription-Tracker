import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SubscriptionService } from '../../core/services/subscription.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { BillingFrequency, Subscription, SubscriptionStatus } from '../../core/models/subscription.models';

const MONTHLY_NORMALIZATION_FACTOR: Record<BillingFrequency, number> = {
  [BillingFrequency.Weekly]: 52 / 12,
  [BillingFrequency.Monthly]: 1,
  [BillingFrequency.Quarterly]: 1 / 3,
  [BillingFrequency.Yearly]: 1 / 12,
  [BillingFrequency.Custom]: 0, // computed per-subscription from customIntervalDays instead
  [BillingFrequency.Lifetime]: 0,
};

const UPCOMING_RENEWAL_WINDOW_DAYS = 30;
const UPCOMING_RENEWAL_LIST_SIZE = 5;
// Backend has no Category/Tag/PaymentMethod list endpoints yet (see HANDOVER.md §6), so this
// dashboard breaks spend down by billing frequency instead of category name until that lands.

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, TranslatePipe, DecimalPipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly subscriptionService = inject(SubscriptionService);

  protected readonly SubscriptionStatus = SubscriptionStatus;
  protected readonly BillingFrequency = BillingFrequency;

  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  private readonly subscriptions = signal<Subscription[]>([]);

  readonly activeCount = computed(
    () => this.subscriptions().filter((s) => s.status === SubscriptionStatus.Active).length,
  );

  readonly trialCount = computed(
    () => this.subscriptions().filter((s) => s.status === SubscriptionStatus.Trial).length,
  );

  readonly estimatedMonthlySpend = computed(() =>
    this.subscriptions()
      .filter((s) => s.status === SubscriptionStatus.Active || s.status === SubscriptionStatus.Trial)
      .reduce((total, s) => total + this.normalizeToMonthly(s), 0),
  );

  readonly upcomingRenewals = computed(() => {
    const today = new Date();
    const windowEnd = new Date(today);
    windowEnd.setDate(windowEnd.getDate() + UPCOMING_RENEWAL_WINDOW_DAYS);

    return this.subscriptions()
      .filter((s) => s.nextRenewalDate)
      .filter((s) => {
        const renewal = new Date(s.nextRenewalDate!);
        return renewal >= today && renewal <= windowEnd;
      })
      .sort((a, b) => a.nextRenewalDate!.localeCompare(b.nextRenewalDate!))
      .slice(0, UPCOMING_RENEWAL_LIST_SIZE);
  });

  readonly spendByFrequency = computed(() => {
    const totals = new Map<BillingFrequency, number>();
    for (const subscription of this.subscriptions()) {
      if (subscription.status !== SubscriptionStatus.Active && subscription.status !== SubscriptionStatus.Trial) {
        continue;
      }
      totals.set(subscription.billingFrequency, (totals.get(subscription.billingFrequency) ?? 0) + 1);
    }
    return Array.from(totals.entries()).map(([frequency, count]) => ({ frequency, count }));
  });

  ngOnInit(): void {
    // pageSize capped at the backend's max (GetSubscriptionsQueryValidator allows 1-100);
    // revisit with a dedicated aggregate endpoint if workspaces grow past this.
    this.subscriptionService.getSubscriptions({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: (page) => {
        this.subscriptions.set(page.items);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  private normalizeToMonthly(subscription: Subscription): number {
    if (subscription.billingFrequency === BillingFrequency.Custom) {
      return subscription.customIntervalDays ? (subscription.amount * 30) / subscription.customIntervalDays : 0;
    }
    return subscription.amount * MONTHLY_NORMALIZATION_FACTOR[subscription.billingFrequency];
  }
}
