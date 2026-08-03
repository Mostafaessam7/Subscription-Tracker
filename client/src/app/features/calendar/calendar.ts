import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SubscriptionService } from '../../core/services/subscription.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { Subscription } from '../../core/models/subscription.models';
import { buildCalendarWeeks, buildRenewalsByDate, CalendarDay } from './calendar-grid';

@Component({
  selector: 'app-calendar',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './calendar.html',
  styleUrl: './calendar.scss',
})
export class Calendar implements OnInit {
  private readonly subscriptionService = inject(SubscriptionService);

  private readonly today = new Date();
  readonly monthCursor = signal(new Date(this.today.getFullYear(), this.today.getMonth(), 1));
  readonly subscriptions = signal<Subscription[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly selectedDayKey = signal<string | null>(null);

  readonly monthLabel = computed(() =>
    this.monthCursor().toLocaleDateString(undefined, { month: 'long', year: 'numeric' }),
  );

  private readonly renewalsByDate = computed(() => buildRenewalsByDate(this.subscriptions()));

  readonly weeks = computed<CalendarDay[][]>(() =>
    buildCalendarWeeks(this.monthCursor(), this.today, this.renewalsByDate()),
  );

  readonly selectedDayRenewals = computed(() => {
    const key = this.selectedDayKey();
    return key ? (this.renewalsByDate().get(key) ?? []) : [];
  });

  ngOnInit(): void {
    this.isLoading.set(true);
    // Mirrors the dashboard's approach: fetch up to the backend's max page size and aggregate client-side.
    // Same >100-subscription undercount caveat applies here as there.
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

  previousMonth(): void {
    const cursor = this.monthCursor();
    this.monthCursor.set(new Date(cursor.getFullYear(), cursor.getMonth() - 1, 1));
    this.selectedDayKey.set(null);
  }

  nextMonth(): void {
    const cursor = this.monthCursor();
    this.monthCursor.set(new Date(cursor.getFullYear(), cursor.getMonth() + 1, 1));
    this.selectedDayKey.set(null);
  }

  goToToday(): void {
    this.monthCursor.set(new Date(this.today.getFullYear(), this.today.getMonth(), 1));
    this.selectedDayKey.set(null);
  }

  selectDay(day: CalendarDay): void {
    if (day.renewals.length === 0) {
      return;
    }
    this.selectedDayKey.set(this.selectedDayKey() === day.dateKey ? null : day.dateKey);
  }
}
