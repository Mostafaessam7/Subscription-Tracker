import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Budgets } from './budgets';
import { BudgetService } from '../../core/services/budget.service';
import { CatalogService } from '../../core/services/catalog.service';
import { Budget, BudgetPeriod } from '../../core/models/budget.models';
import { Category } from '../../core/models/catalog.models';

function fakeBudget(overrides: Partial<Budget> = {}): Budget {
  return {
    id: 'budget-1',
    name: 'Streaming',
    amount: 100,
    currencyCode: 'USD',
    period: BudgetPeriod.Monthly,
    categoryId: null,
    alertThresholdPercentage: 80,
    currentSpend: 0,
    hasExceededThreshold: false,
    ...overrides,
  };
}

function createBudgets(budgets: Budget[], categories: Category[] = []): Budgets {
  TestBed.configureTestingModule({
    providers: [
      { provide: BudgetService, useValue: { getBudgets: () => of(budgets) } },
      { provide: CatalogService, useValue: { getCategories: () => of(categories) } },
    ],
  });

  const component = TestBed.runInInjectionContext(() => new Budgets());
  component.ngOnInit();
  return component;
}

describe('Budgets', () => {
  describe('spendPercentage', () => {
    it('computes the plain spend/amount ratio as a percentage', () => {
      const component = createBudgets([]);

      expect(component.spendPercentage(fakeBudget({ amount: 100, currentSpend: 25 }))).toBe(25);
    });

    it('clamps at 100 once spend exceeds the budgeted amount', () => {
      const component = createBudgets([]);

      expect(component.spendPercentage(fakeBudget({ amount: 100, currentSpend: 250 }))).toBe(100);
    });

    it('returns 0 instead of dividing by zero when the budget amount is zero', () => {
      const component = createBudgets([]);

      expect(component.spendPercentage(fakeBudget({ amount: 0, currentSpend: 50 }))).toBe(0);
    });

    it('returns 0 when nothing has been spent yet', () => {
      const component = createBudgets([]);

      expect(component.spendPercentage(fakeBudget({ amount: 100, currentSpend: 0 }))).toBe(0);
    });
  });

  describe('categoryName', () => {
    it('resolves a matching category id to its name', () => {
      const component = createBudgets(
        [],
        [{ id: 'cat-1', name: 'Streaming', color: null, icon: null }],
      );

      expect(component.categoryName('cat-1')).toBe('Streaming');
    });

    it('returns null for a null category id (workspace-wide budget)', () => {
      const component = createBudgets([], [{ id: 'cat-1', name: 'Streaming', color: null, icon: null }]);

      expect(component.categoryName(null)).toBeNull();
    });

    it('returns null when the category id does not match any loaded category', () => {
      const component = createBudgets([], [{ id: 'cat-1', name: 'Streaming', color: null, icon: null }]);

      expect(component.categoryName('does-not-exist')).toBeNull();
    });
  });

  describe('isEditing', () => {
    it('is false until edit() has been called', () => {
      const component = createBudgets([fakeBudget()]);

      expect(component.isEditing).toBe(false);
    });

    it('is true after edit() and false again after cancelEdit()', () => {
      const component = createBudgets([fakeBudget({ id: 'budget-1' })]);

      component.edit(fakeBudget({ id: 'budget-1' }));
      expect(component.isEditing).toBe(true);

      component.cancelEdit();
      expect(component.isEditing).toBe(false);
    });
  });
});
