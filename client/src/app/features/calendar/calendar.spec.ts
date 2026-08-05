import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Calendar } from './calendar';
import { SubscriptionService } from '../../core/services/subscription.service';
import { BillingFrequency, PagedList, Subscription, SubscriptionStatus } from '../../core/models/subscription.models';
import { CalendarDay } from './calendar-grid';

function fakeSubscription(overrides: Partial<Subscription> = {}): Subscription {
  return {
    id: 'sub-1',
    name: 'Netflix',
    provider: 'Netflix Inc',
    logoUrl: null,
    websiteUrl: null,
    notes: null,
    categoryId: null,
    paymentMethodId: null,
    amount: 10,
    currencyCode: 'USD',
    billingFrequency: BillingFrequency.Monthly,
    customIntervalDays: null,
    startDate: '2026-01-01',
    trialEndDate: null,
    nextRenewalDate: '2026-01-15',
    endDate: null,
    autoRenewal: true,
    status: SubscriptionStatus.Active,
    tagIds: [],
    sharedUserIds: [],
    attachments: [],
    ...overrides,
  };
}

function pagedList(items: Subscription[]): PagedList<Subscription> {
  return { items, totalCount: items.length, pageNumber: 1, pageSize: 100, totalPages: 1, hasPreviousPage: false, hasNextPage: false };
}

function fakeDay(overrides: Partial<CalendarDay> = {}): CalendarDay {
  return {
    date: new Date(2026, 0, 15),
    dateKey: '2026-01-15',
    inCurrentMonth: true,
    isToday: false,
    renewals: [],
    ...overrides,
  };
}

function createCalendar(subscriptions: Subscription[], getSubscriptions?: ReturnType<typeof vi.fn>): Calendar {
  TestBed.configureTestingModule({
    providers: [
      {
        provide: SubscriptionService,
        useValue: { getSubscriptions: getSubscriptions ?? vi.fn().mockReturnValue(of(pagedList(subscriptions))) },
      },
    ],
  });

  const component = TestBed.runInInjectionContext(() => new Calendar());
  component.ngOnInit();
  return component;
}

describe('Calendar', () => {
  it('loads subscriptions on init and stops loading', () => {
    const calendar = createCalendar([fakeSubscription()]);

    expect(calendar.isLoading()).toBe(false);
    expect(calendar.subscriptions().length).toBe(1);
  });

  it('sets a generic error and stops loading when the fetch fails', () => {
    const calendar = createCalendar([], vi.fn().mockReturnValue(throwError(() => new Error('boom'))));

    expect(calendar.isLoading()).toBe(false);
    expect(calendar.errorMessage()).toBe('error.generic');
  });

  it('groups renewals for the selected day via selectedDayRenewals', () => {
    const jan15 = fakeSubscription({ id: 's1', nextRenewalDate: '2026-01-15' });
    const jan16 = fakeSubscription({ id: 's2', nextRenewalDate: '2026-01-16' });
    const calendar = createCalendar([jan15, jan16]);

    calendar.selectedDayKey.set('2026-01-15');

    expect(calendar.selectedDayRenewals().map((s) => s.id)).toEqual(['s1']);
  });

  it('returns an empty list when no day is selected', () => {
    const calendar = createCalendar([fakeSubscription({ nextRenewalDate: '2026-01-15' })]);

    expect(calendar.selectedDayRenewals()).toEqual([]);
  });

  describe('selectDay', () => {
    it('ignores days with no renewals', () => {
      const calendar = createCalendar([]);

      calendar.selectDay(fakeDay({ dateKey: '2026-01-20', renewals: [] }));

      expect(calendar.selectedDayKey()).toBeNull();
    });

    it('selects a day that has renewals', () => {
      const calendar = createCalendar([]);

      calendar.selectDay(fakeDay({ dateKey: '2026-01-20', renewals: [fakeSubscription()] }));

      expect(calendar.selectedDayKey()).toBe('2026-01-20');
    });

    it('toggles off when the same day is selected again', () => {
      const calendar = createCalendar([]);
      const day = fakeDay({ dateKey: '2026-01-20', renewals: [fakeSubscription()] });

      calendar.selectDay(day);
      calendar.selectDay(day);

      expect(calendar.selectedDayKey()).toBeNull();
    });
  });

  describe('month navigation', () => {
    it('moves the cursor to the previous/next month and clears the day selection', () => {
      const calendar = createCalendar([]);
      const startMonth = calendar.monthCursor().getMonth();
      calendar.selectedDayKey.set('2026-01-15');

      calendar.nextMonth();

      expect(calendar.monthCursor().getMonth()).toBe((startMonth + 1) % 12);
      expect(calendar.selectedDayKey()).toBeNull();

      calendar.selectedDayKey.set('2026-02-01');
      calendar.previousMonth();

      expect(calendar.monthCursor().getMonth()).toBe(startMonth);
      expect(calendar.selectedDayKey()).toBeNull();
    });

    it('returns to the current month via goToToday', () => {
      const calendar = createCalendar([]);
      const startMonth = calendar.monthCursor().getMonth();

      calendar.nextMonth();
      calendar.nextMonth();
      calendar.goToToday();

      expect(calendar.monthCursor().getMonth()).toBe(startMonth);
    });
  });
});
