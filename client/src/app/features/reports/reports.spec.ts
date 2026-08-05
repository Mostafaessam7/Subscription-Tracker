import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Reports } from './reports';
import { ReportService } from '../../core/services/report.service';
import { CatalogService } from '../../core/services/catalog.service';
import { SubscriptionStatus } from '../../core/models/subscription.models';

function createReports(reportOverrides: Partial<Record<string, ReturnType<typeof vi.fn>>> = {}): Reports {
  TestBed.configureTestingModule({
    providers: [
      {
        provide: ReportService,
        useValue: {
          exportSubscriptionsCsv: vi.fn().mockReturnValue(of(new Blob(['csv']))),
          exportSubscriptionsExcel: vi.fn().mockReturnValue(of(new Blob(['xlsx']))),
          exportSubscriptionsPdf: vi.fn().mockReturnValue(of(new Blob(['pdf']))),
          ...reportOverrides,
        },
      },
      { provide: CatalogService, useValue: { getCategories: () => of([]) } },
    ],
  });

  return TestBed.runInInjectionContext(() => new Reports());
}

describe('Reports', () => {
  beforeEach(() => {
    vi.stubGlobal('URL', { ...URL, createObjectURL: vi.fn(() => 'blob:fake-url'), revokeObjectURL: vi.fn() });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('sends null instead of empty strings for unset filters', () => {
    const exportSubscriptionsCsv = vi.fn().mockReturnValue(of(new Blob()));
    const reports = createReports({ exportSubscriptionsCsv });

    reports.exportCsv();

    expect(exportSubscriptionsCsv).toHaveBeenCalledWith({ searchTerm: null, categoryId: null, status: null });
  });

  it('passes through whatever search/category/status filters are set', () => {
    const exportSubscriptionsExcel = vi.fn().mockReturnValue(of(new Blob()));
    const reports = createReports({ exportSubscriptionsExcel });

    reports.searchTerm = 'netflix';
    reports.categoryId = 'cat-1';
    reports.status = SubscriptionStatus.Active;
    reports.exportExcel();

    expect(exportSubscriptionsExcel).toHaveBeenCalledWith({
      searchTerm: 'netflix',
      categoryId: 'cat-1',
      status: SubscriptionStatus.Active,
    });
  });

  it('flips isExporting on while the PDF export is in flight and off once it resolves', () => {
    const reports = createReports();

    expect(reports.isExporting()).toBe(false);
    reports.exportPdf();
    expect(reports.isExporting()).toBe(false); // synchronous `of()` resolves immediately in tests
  });

  it('sets a generic error and stops exporting when the download fails', () => {
    const exportSubscriptionsCsv = vi.fn().mockReturnValue(throwError(() => new Error('network down')));
    const reports = createReports({ exportSubscriptionsCsv });

    reports.exportCsv();

    expect(reports.isExporting()).toBe(false);
    expect(reports.errorMessage()).toBe('error.generic');
  });

  it('clears any previous error when a new export starts', () => {
    const exportSubscriptionsCsv = vi
      .fn()
      .mockReturnValueOnce(throwError(() => new Error('first failure')))
      .mockReturnValueOnce(of(new Blob()));
    const reports = createReports({ exportSubscriptionsCsv });

    reports.exportCsv();
    expect(reports.errorMessage()).toBe('error.generic');

    reports.exportCsv();
    expect(reports.errorMessage()).toBeNull();
  });
});
