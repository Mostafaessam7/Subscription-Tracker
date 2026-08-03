import { buildCalendarWeeks, buildRenewalsByDate, toDateKey } from './calendar-grid';
import { BillingFrequency, Subscription, SubscriptionStatus } from '../../core/models/subscription.models';

function fakeSubscription(id: string, nextRenewalDate: string | null): Subscription {
  return {
    id,
    name: 'Netflix',
    provider: 'Netflix Inc',
    logoUrl: null,
    websiteUrl: null,
    notes: null,
    categoryId: null,
    paymentMethodId: null,
    amount: 15.99,
    currencyCode: 'USD',
    billingFrequency: BillingFrequency.Monthly,
    customIntervalDays: null,
    startDate: '2026-08-01',
    trialEndDate: null,
    nextRenewalDate,
    endDate: null,
    autoRenewal: true,
    status: SubscriptionStatus.Active,
    tagIds: [],
    sharedUserIds: [],
    attachments: [],
  };
}

function flatten(weeks: ReturnType<typeof buildCalendarWeeks>) {
  return weeks.flat();
}

describe('buildCalendarWeeks', () => {
  it('produces exactly 42 cells across 6 weeks', () => {
    const weeks = buildCalendarWeeks(new Date(2026, 8, 1), new Date(2026, 8, 15), new Map());

    expect(weeks).toHaveLength(6);
    expect(flatten(weeks)).toHaveLength(42);
  });

  it('places every day of the viewed month, in order, marked as in-month', () => {
    const weeks = buildCalendarWeeks(new Date(2026, 8, 1), new Date(2026, 8, 15), new Map());
    const inMonthDays = flatten(weeks).filter((d) => d.inCurrentMonth);

    expect(inMonthDays).toHaveLength(30); // September has 30 days
    expect(inMonthDays[0].dateKey).toBe('2026-09-01');
    expect(inMonthDays[29].dateKey).toBe('2026-09-30');
  });

  it('marks the leading/trailing days from adjacent months as outside the current month', () => {
    const weeks = buildCalendarWeeks(new Date(2026, 8, 1), new Date(2026, 8, 15), new Map());
    const cells = flatten(weeks);

    const septFirstIndex = cells.findIndex((d) => d.dateKey === '2026-09-01');
    const septLastIndex = cells.findIndex((d) => d.dateKey === '2026-09-30');

    for (let i = 0; i < septFirstIndex; i++) {
      expect(cells[i].inCurrentMonth).toBe(false);
    }
    for (let i = septLastIndex + 1; i < cells.length; i++) {
      expect(cells[i].inCurrentMonth).toBe(false);
    }
  });

  it('attaches a renewal to the exact calendar cell matching its date, not an adjacent-month cell with the same day number', () => {
    const renewals = buildRenewalsByDate([fakeSubscription('sub-1', '2026-09-01')]);
    const weeks = buildCalendarWeeks(new Date(2026, 8, 1), new Date(2026, 8, 15), renewals);
    const cells = flatten(weeks);

    const sept1 = cells.find((d) => d.dateKey === '2026-09-01')!;
    expect(sept1.renewals).toHaveLength(1);
    expect(sept1.inCurrentMonth).toBe(true);

    // The trailing October 1st cell (same day-of-month number) must NOT pick up September 1st's renewal.
    const oct1 = cells.find((d) => d.dateKey === '2026-10-01');
    if (oct1) {
      expect(oct1.renewals).toHaveLength(0);
    }
  });

  it('marks today correctly and only today', () => {
    const weeks = buildCalendarWeeks(new Date(2026, 8, 1), new Date(2026, 8, 15), new Map());
    const todays = flatten(weeks).filter((d) => d.isToday);

    expect(todays).toHaveLength(1);
    expect(todays[0].dateKey).toBe('2026-09-15');
  });
});

describe('toDateKey', () => {
  it('zero-pads month and day', () => {
    expect(toDateKey(new Date(2026, 0, 5))).toBe('2026-01-05');
  });
});
