import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Dashboard } from './dashboard';
import { SubscriptionService } from '../../core/services/subscription.service';
import { BillingFrequency, PagedList, Subscription, SubscriptionStatus } from '../../core/models/subscription.models';

function fakeSubscription(overrides: Partial<Subscription> = {}): Subscription {
  return {
    id: overrides.id ?? 'sub-1',
    name: overrides.name ?? 'Netflix',
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
    nextRenewalDate: null,
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
  return {
    items,
    totalCount: items.length,
    pageNumber: 1,
    pageSize: 100,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}

function createDashboard(subscriptions: Subscription[]): Dashboard {
  TestBed.configureTestingModule({
    providers: [{ provide: SubscriptionService, useValue: { getSubscriptions: () => of(pagedList(subscriptions)) } }],
  });

  const dashboard = TestBed.runInInjectionContext(() => new Dashboard());
  dashboard.ngOnInit();
  return dashboard;
}

describe('Dashboard', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('counts active and trial subscriptions separately', () => {
    const dashboard = createDashboard([
      fakeSubscription({ id: '1', status: SubscriptionStatus.Active }),
      fakeSubscription({ id: '2', status: SubscriptionStatus.Active }),
      fakeSubscription({ id: '3', status: SubscriptionStatus.Trial }),
      fakeSubscription({ id: '4', status: SubscriptionStatus.Cancelled }),
    ]);

    expect(dashboard.activeCount()).toBe(2);
    expect(dashboard.trialCount()).toBe(1);
    expect(dashboard.totalSubscriptions()).toBe(4);
  });

  it('normalizes every billing frequency to a monthly spend estimate', () => {
    const dashboard = createDashboard([
      fakeSubscription({ id: 'weekly', amount: 10, billingFrequency: BillingFrequency.Weekly }),
      fakeSubscription({ id: 'monthly', amount: 10, billingFrequency: BillingFrequency.Monthly }),
      fakeSubscription({ id: 'quarterly', amount: 30, billingFrequency: BillingFrequency.Quarterly }),
      fakeSubscription({ id: 'yearly', amount: 120, billingFrequency: BillingFrequency.Yearly }),
      fakeSubscription({
        id: 'custom',
        amount: 15,
        billingFrequency: BillingFrequency.Custom,
        customIntervalDays: 30,
      }),
      fakeSubscription({ id: 'lifetime', amount: 500, billingFrequency: BillingFrequency.Lifetime }),
    ]);

    // weekly: 10 * 52/12 ≈ 43.33, monthly: 10, quarterly: 30/3 = 10, yearly: 120/12 = 10, custom: 15*30/30 = 15, lifetime: 0
    expect(dashboard.estimatedMonthlySpend()).toBeCloseTo(43.333 + 10 + 10 + 10 + 15 + 0, 2);
  });

  it('excludes paused/cancelled/expired subscriptions from the spend estimate', () => {
    const dashboard = createDashboard([
      fakeSubscription({ id: 'active', amount: 10, status: SubscriptionStatus.Active }),
      fakeSubscription({ id: 'paused', amount: 999, status: SubscriptionStatus.Paused }),
      fakeSubscription({ id: 'cancelled', amount: 999, status: SubscriptionStatus.Cancelled }),
    ]);

    expect(dashboard.estimatedMonthlySpend()).toBe(10);
  });

  it('treats a custom-frequency subscription with no interval set as contributing zero spend', () => {
    const dashboard = createDashboard([
      fakeSubscription({ amount: 100, billingFrequency: BillingFrequency.Custom, customIntervalDays: null }),
    ]);

    expect(dashboard.estimatedMonthlySpend()).toBe(0);
  });

  it('only includes renewals within the next 30 days, sorted soonest first', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 0, 1));

    const dashboard = createDashboard([
      fakeSubscription({ id: 'too-soon-not', name: 'Far', nextRenewalDate: '2026-03-01' }),
      fakeSubscription({ id: 'in-window-late', name: 'Later', nextRenewalDate: '2026-01-20' }),
      fakeSubscription({ id: 'in-window-early', name: 'Sooner', nextRenewalDate: '2026-01-05' }),
      fakeSubscription({ id: 'no-date', name: 'NoDate', nextRenewalDate: null }),
    ]);

    const renewals = dashboard.upcomingRenewals();
    expect(renewals.map((r) => r.subscription.name)).toEqual(['Sooner', 'Later']);
    expect(renewals[0].daysUntil).toBe(4);
    expect(renewals[1].daysUntil).toBe(19);
  });

  it('caps the upcoming renewals list at 5 entries', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 0, 1));

    const subs = Array.from({ length: 8 }, (_, i) =>
      fakeSubscription({ id: `s${i}`, nextRenewalDate: `2026-01-0${(i % 9) + 1}` }),
    );
    const dashboard = createDashboard(subs);

    expect(dashboard.upcomingRenewals().length).toBe(5);
  });

  it('groups active/trial subscriptions by billing frequency, most common first', () => {
    const dashboard = createDashboard([
      fakeSubscription({ id: '1', billingFrequency: BillingFrequency.Monthly }),
      fakeSubscription({ id: '2', billingFrequency: BillingFrequency.Monthly }),
      fakeSubscription({ id: '3', billingFrequency: BillingFrequency.Yearly }),
      fakeSubscription({ id: '4', billingFrequency: BillingFrequency.Yearly, status: SubscriptionStatus.Cancelled }),
    ]);

    const byFrequency = dashboard.spendByFrequency();
    expect(byFrequency[0]).toEqual({ frequency: BillingFrequency.Monthly, count: 2 });
    expect(dashboard.spendByFrequencyMax()).toBe(2);
  });

  it('builds initials from up to the first two words of a name', () => {
    const dashboard = createDashboard([]);

    expect(dashboard.initials('Netflix')).toBe('N');
    expect(dashboard.initials('Adobe Creative Cloud')).toBe('AC');
    expect(dashboard.initials('  spaced   out  ')).toBe('SO');
  });

  it.each([
    [3, 'dashboard.greeting.night'],
    [9, 'dashboard.greeting.morning'],
    [14, 'dashboard.greeting.afternoon'],
    [20, 'dashboard.greeting.evening'],
  ] as const)('picks the %i o\'clock greeting as %s', (hour, expectedKey) => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 0, 1, hour));
    TestBed.resetTestingModule();
    const dashboard = createDashboard([]);

    expect(dashboard.greeting.key).toBe(expectedKey);
  });
});
