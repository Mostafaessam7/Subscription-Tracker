import { BillingFrequency } from './subscription.models';

export interface UpcomingRenewal {
  subscriptionId: string;
  name: string;
  amount: number;
  currencyCode: string;
  nextRenewalDate: string;
  daysUntil: number;
}

export interface FrequencyBreakdown {
  frequency: BillingFrequency;
  count: number;
}

export interface DashboardSummary {
  totalSubscriptions: number;
  activeCount: number;
  trialCount: number;
  estimatedMonthlySpend: number;
  upcomingRenewals: UpcomingRenewal[];
  spendByFrequency: FrequencyBreakdown[];
}
