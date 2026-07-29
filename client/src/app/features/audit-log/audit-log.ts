import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { AuditLogService } from '../../core/services/audit-log.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { AuditLogPage } from '../../core/models/audit-log.models';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [TranslatePipe, DatePipe],
  templateUrl: './audit-log.html',
  styleUrl: './audit-log.scss',
})
export class AuditLog {
  private readonly auditLogService = inject(AuditLogService);

  readonly page = signal<AuditLogPage | null>(null);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  pageNumber = 1;
  readonly pageSize = 25;

  constructor() {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.auditLogService.getAuditLogs(this.pageNumber, this.pageSize).subscribe({
      next: (page) => {
        this.page.set(page);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  goToPage(pageNumber: number): void {
    this.pageNumber = pageNumber;
    this.load();
  }

  formatAction(action: string): string {
    return action.replace(/Command$/, '').replace(/([a-z])([A-Z])/g, '$1 $2');
  }
}
