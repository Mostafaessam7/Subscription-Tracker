import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { SubscriptionService } from '../../../core/services/subscription.service';
import { PermissionsService } from '../../../core/services/permissions.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { Permissions } from '../../../core/models/permissions';
import {
  GetSubscriptionsParams,
  PagedList,
  Subscription,
  SubscriptionStatus,
} from '../../../core/models/subscription.models';

@Component({
  selector: 'app-subscription-list',
  standalone: true,
  imports: [FormsModule, RouterLink, TranslatePipe],
  templateUrl: './subscription-list.html',
  styleUrl: './subscription-list.scss',
})
export class SubscriptionList {
  private readonly subscriptionService = inject(SubscriptionService);
  private readonly router = inject(Router);
  protected readonly permissions = inject(PermissionsService);
  protected readonly Permissions = Permissions;

  protected readonly SubscriptionStatus = SubscriptionStatus;
  protected readonly statusOptions = [
    SubscriptionStatus.Trial,
    SubscriptionStatus.Active,
    SubscriptionStatus.Paused,
    SubscriptionStatus.Cancelled,
    SubscriptionStatus.Expired,
  ];

  readonly page = signal<PagedList<Subscription> | null>(null);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  searchTerm = '';
  status: SubscriptionStatus | '' = '';
  sortBy = 'name';
  sortDescending = false;
  pageNumber = 1;
  readonly pageSize = 20;

  constructor() {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const params: GetSubscriptionsParams = {
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      searchTerm: this.searchTerm || null,
      status: this.status === '' ? null : this.status,
      sortBy: this.sortBy,
      sortDescending: this.sortDescending,
    };

    this.subscriptionService.getSubscriptions(params).subscribe({
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

  applyFilters(): void {
    this.pageNumber = 1;
    this.load();
  }

  sort(column: string): void {
    if (this.sortBy === column) {
      this.sortDescending = !this.sortDescending;
    } else {
      this.sortBy = column;
      this.sortDescending = false;
    }
    this.load();
  }

  goToPage(pageNumber: number): void {
    this.pageNumber = pageNumber;
    this.load();
  }

  openDetail(id: string): void {
    void this.router.navigate(['/subscriptions', id]);
  }
}
