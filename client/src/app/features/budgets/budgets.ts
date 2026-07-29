import { DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BudgetService } from '../../core/services/budget.service';
import { CatalogService } from '../../core/services/catalog.service';
import { PermissionsService } from '../../core/services/permissions.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { Permissions } from '../../core/models/permissions';
import { Budget, BudgetPeriod } from '../../core/models/budget.models';
import { Category } from '../../core/models/catalog.models';

@Component({
  selector: 'app-budgets',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe, DecimalPipe],
  templateUrl: './budgets.html',
  styleUrl: './budgets.scss',
})
export class Budgets implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly budgetService = inject(BudgetService);
  private readonly catalogService = inject(CatalogService);
  protected readonly permissions = inject(PermissionsService);
  protected readonly Permissions = Permissions;

  protected readonly BudgetPeriod = BudgetPeriod;
  protected readonly periodOptions = [BudgetPeriod.Monthly, BudgetPeriod.Yearly];

  readonly budgets = signal<Budget[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly errorMessage = signal<string | null>(null);
  readonly isLoading = signal(true);

  private editingBudgetId: string | null = null;

  readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    currencyCode: ['USD', [Validators.required]],
    period: [BudgetPeriod.Monthly, [Validators.required]],
    categoryId: [''],
    alertThresholdPercentage: [80, [Validators.required, Validators.min(1), Validators.max(100)]],
  });

  ngOnInit(): void {
    this.catalogService.getCategories().subscribe({ next: (c) => this.categories.set(c) });
    this.reload();
  }

  private reload(): void {
    this.isLoading.set(true);
    this.budgetService.getBudgets().subscribe({
      next: (budgets) => {
        this.budgets.set(budgets);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    const raw = this.form.getRawValue();

    const done = () => {
      this.form.reset({ name: '', amount: 0, currencyCode: 'USD', period: BudgetPeriod.Monthly, categoryId: '', alertThresholdPercentage: 80 });
      this.editingBudgetId = null;
      this.reload();
    };

    if (this.editingBudgetId) {
      this.budgetService
        .updateBudget(this.editingBudgetId, {
          amount: raw.amount,
          currencyCode: raw.currencyCode,
          alertThresholdPercentage: raw.alertThresholdPercentage,
        })
        .subscribe({ next: done, error: () => this.errorMessage.set('error.generic') });
    } else {
      this.budgetService
        .createBudget({
          name: raw.name,
          amount: raw.amount,
          currencyCode: raw.currencyCode,
          period: raw.period,
          categoryId: raw.categoryId || null,
          alertThresholdPercentage: raw.alertThresholdPercentage,
        })
        .subscribe({ next: done, error: () => this.errorMessage.set('error.generic') });
    }
  }

  edit(budget: Budget): void {
    this.editingBudgetId = budget.id;
    this.form.setValue({
      name: budget.name,
      amount: budget.amount,
      currencyCode: budget.currencyCode,
      period: budget.period,
      categoryId: budget.categoryId ?? '',
      alertThresholdPercentage: budget.alertThresholdPercentage,
    });
    this.form.controls.name.disable();
    this.form.controls.period.disable();
    this.form.controls.categoryId.disable();
  }

  cancelEdit(): void {
    this.editingBudgetId = null;
    this.form.reset({ name: '', amount: 0, currencyCode: 'USD', period: BudgetPeriod.Monthly, categoryId: '', alertThresholdPercentage: 80 });
    this.form.controls.name.enable();
    this.form.controls.period.enable();
    this.form.controls.categoryId.enable();
  }

  delete(budget: Budget): void {
    this.budgetService.deleteBudget(budget.id).subscribe({
      next: () => this.budgets.update((list) => list.filter((b) => b.id !== budget.id)),
      error: () => this.errorMessage.set('error.generic'),
    });
  }

  categoryName(categoryId: string | null): string | null {
    return this.categories().find((c) => c.id === categoryId)?.name ?? null;
  }

  get isEditing(): boolean {
    return this.editingBudgetId !== null;
  }
}
