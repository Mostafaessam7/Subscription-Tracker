import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DashboardService } from '../../core/services/dashboard.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { BillingFrequency } from '../../core/models/subscription.models';
import { DashboardSummary } from '../../core/models/dashboard.models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, TranslatePipe, DecimalPipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly dashboardService = inject(DashboardService);

  protected readonly BillingFrequency = BillingFrequency;

  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  private readonly summary = signal<DashboardSummary | null>(null);

  readonly totalSubscriptions = computed(() => this.summary()?.totalSubscriptions ?? 0);
  readonly activeCount = computed(() => this.summary()?.activeCount ?? 0);
  readonly trialCount = computed(() => this.summary()?.trialCount ?? 0);
  readonly estimatedMonthlySpend = computed(() => this.summary()?.estimatedMonthlySpend ?? 0);
  readonly upcomingRenewals = computed(() => this.summary()?.upcomingRenewals ?? []);
  readonly spendByFrequency = computed(() => this.summary()?.spendByFrequency ?? []);

  readonly spendByFrequencyMax = computed(() =>
    Math.max(1, ...this.spendByFrequency().map((entry) => entry.count)),
  );

  readonly greeting = (() => {
    const hour = new Date().getHours();
    if (hour < 5) return { key: 'dashboard.greeting.night', emoji: '🌙' };
    if (hour < 12) return { key: 'dashboard.greeting.morning', emoji: '☀️' };
    if (hour < 18) return { key: 'dashboard.greeting.afternoon', emoji: '🌤️' };
    return { key: 'dashboard.greeting.evening', emoji: '🌆' };
  })();

  initials(name: string): string {
    return name
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]!.toUpperCase())
      .join('');
  }

  ngOnInit(): void {
    // Computed server-side over every subscription in the workspace (not capped at a page size) -
    // see GetDashboardSummaryQueryHandler. Previously this fetched pageSize=100 and computed KPIs
    // client-side, which undercounted for any workspace with more than 100 subscriptions.
    this.dashboardService.getSummary().subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }
}
