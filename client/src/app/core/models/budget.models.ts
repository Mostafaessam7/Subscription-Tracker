export enum BudgetPeriod {
  Monthly = 0,
  Yearly = 1,
}

export interface Budget {
  id: string;
  name: string;
  amount: number;
  currencyCode: string;
  period: BudgetPeriod;
  categoryId: string | null;
  alertThresholdPercentage: number;
  currentSpend: number;
  hasExceededThreshold: boolean;
}

export interface CreateBudgetRequest {
  name: string;
  amount: number;
  currencyCode: string;
  period: BudgetPeriod;
  categoryId?: string | null;
  alertThresholdPercentage: number;
}

export interface UpdateBudgetRequest {
  amount: number;
  currencyCode: string;
  alertThresholdPercentage: number;
}
