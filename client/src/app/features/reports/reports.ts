import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReportService } from '../../core/services/report.service';
import { CatalogService } from '../../core/services/catalog.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { SubscriptionStatus } from '../../core/models/subscription.models';
import { Category } from '../../core/models/catalog.models';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  templateUrl: './reports.html',
  styleUrl: './reports.scss',
})
export class Reports {
  private readonly reportService = inject(ReportService);
  private readonly catalogService = inject(CatalogService);

  protected readonly statusOptions = [
    SubscriptionStatus.Trial,
    SubscriptionStatus.Active,
    SubscriptionStatus.Paused,
    SubscriptionStatus.Cancelled,
    SubscriptionStatus.Expired,
  ];

  readonly categories = signal<Category[]>([]);
  readonly isExporting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  searchTerm = '';
  categoryId: string | '' = '';
  status: SubscriptionStatus | '' = '';

  constructor() {
    this.catalogService.getCategories().subscribe({ next: (c) => this.categories.set(c) });
  }

  private get filters() {
    return {
      searchTerm: this.searchTerm || null,
      categoryId: this.categoryId || null,
      status: this.status === '' ? null : this.status,
    };
  }

  exportCsv(): void {
    this.download(this.reportService.exportSubscriptionsCsv(this.filters), 'subscriptions.csv');
  }

  exportExcel(): void {
    this.download(this.reportService.exportSubscriptionsExcel(this.filters), 'subscriptions.xlsx');
  }

  private download(source: ReturnType<ReportService['exportSubscriptionsCsv']>, fallbackFileName: string): void {
    this.isExporting.set(true);
    this.errorMessage.set(null);

    source.subscribe({
      next: (blob) => {
        this.isExporting.set(false);
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fallbackFileName;
        link.click();
        URL.revokeObjectURL(url);
      },
      error: () => {
        this.isExporting.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }
}
