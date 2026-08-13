import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Dashboard } from './dashboard';
import { DashboardService } from '../../core/services/dashboard.service';
import { BillingFrequency } from '../../core/models/subscription.models';
import { DashboardSummary } from '../../core/models/dashboard.models';

function fakeSummary(overrides: Partial<DashboardSummary> = {}): DashboardSummary {
  return {
    totalSubscriptions: 4,
    activeCount: 2,
    trialCount: 1,
    estimatedMonthlySpend: 43.33,
    upcomingRenewals: [
      { subscriptionId: '1', name: 'Sooner', amount: 10, currencyCode: 'USD', nextRenewalDate: '2026-01-05', daysUntil: 4 },
      { subscriptionId: '2', name: 'Later', amount: 10, currencyCode: 'USD', nextRenewalDate: '2026-01-20', daysUntil: 19 },
    ],
    spendByFrequency: [{ frequency: BillingFrequency.Monthly, count: 2 }],
    ...overrides,
  };
}

function createDashboard(summary: DashboardSummary): Dashboard {
  TestBed.configureTestingModule({
    providers: [{ provide: DashboardService, useValue: { getSummary: () => of(summary) } }],
  });

  const dashboard = TestBed.runInInjectionContext(() => new Dashboard());
  dashboard.ngOnInit();
  return dashboard;
}

describe('Dashboard', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('exposes the KPIs returned by the dashboard summary endpoint', () => {
    const dashboard = createDashboard(fakeSummary());

    expect(dashboard.isLoading()).toBe(false);
    expect(dashboard.totalSubscriptions()).toBe(4);
    expect(dashboard.activeCount()).toBe(2);
    expect(dashboard.trialCount()).toBe(1);
    expect(dashboard.estimatedMonthlySpend()).toBe(43.33);
  });

  it('exposes the upcoming renewals list as returned, already ordered by the backend', () => {
    const dashboard = createDashboard(fakeSummary());

    const renewals = dashboard.upcomingRenewals();
    expect(renewals.map((r) => r.name)).toEqual(['Sooner', 'Later']);
    expect(renewals[0].daysUntil).toBe(4);
  });

  it('exposes the frequency breakdown and computes the chart-bar max', () => {
    const dashboard = createDashboard(
      fakeSummary({
        spendByFrequency: [
          { frequency: BillingFrequency.Monthly, count: 2 },
          { frequency: BillingFrequency.Yearly, count: 1 },
        ],
      }),
    );

    expect(dashboard.spendByFrequency()[0]).toEqual({ frequency: BillingFrequency.Monthly, count: 2 });
    expect(dashboard.spendByFrequencyMax()).toBe(2);
  });

  it('defaults spendByFrequencyMax to 1 when there is no data, to avoid a divide-by-zero bar width', () => {
    const dashboard = createDashboard(fakeSummary({ spendByFrequency: [] }));

    expect(dashboard.spendByFrequencyMax()).toBe(1);
  });

  it('surfaces a generic error and stops loading when the summary request fails', () => {
    TestBed.configureTestingModule({
      providers: [{ provide: DashboardService, useValue: { getSummary: () => throwError(() => new Error('boom')) } }],
    });
    const dashboard = TestBed.runInInjectionContext(() => new Dashboard());
    dashboard.ngOnInit();

    expect(dashboard.isLoading()).toBe(false);
    expect(dashboard.errorMessage()).toBe('error.generic');
  });

  it('builds initials from up to the first two words of a name', () => {
    const dashboard = createDashboard(fakeSummary());

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
    const dashboard = createDashboard(fakeSummary());

    expect(dashboard.greeting.key).toBe(expectedKey);
  });
});
