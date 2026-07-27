import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { SubscriptionService } from '../../../core/services/subscription.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';
import { Subscription, SubscriptionStatus } from '../../../core/models/subscription.models';
import { Category, PaymentMethod, Tag } from '../../../core/models/catalog.models';

@Component({
  selector: 'app-subscription-detail',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './subscription-detail.html',
  styleUrl: './subscription-detail.scss',
})
export class SubscriptionDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly subscriptionService = inject(SubscriptionService);
  private readonly catalogService = inject(CatalogService);

  protected readonly SubscriptionStatus = SubscriptionStatus;

  readonly subscription = signal<Subscription | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly isActionInProgress = signal(false);

  private readonly categories = signal<Category[]>([]);
  private readonly paymentMethods = signal<PaymentMethod[]>([]);
  private readonly tags = signal<Tag[]>([]);

  readonly categoryName = computed(() => {
    const categoryId = this.subscription()?.categoryId;
    return this.categories().find((c) => c.id === categoryId)?.name ?? null;
  });

  readonly paymentMethodLabel = computed(() => {
    const paymentMethodId = this.subscription()?.paymentMethodId;
    return this.paymentMethods().find((p) => p.id === paymentMethodId)?.label ?? null;
  });

  readonly tagNames = computed(() => {
    const tagIds = this.subscription()?.tagIds ?? [];
    return this.tags()
      .filter((t) => tagIds.includes(t.id))
      .map((t) => t.name);
  });

  private id!: string;

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id')!;
    this.catalogService.getCategories().subscribe({ next: (c) => this.categories.set(c) });
    this.catalogService.getPaymentMethods().subscribe({ next: (p) => this.paymentMethods.set(p) });
    this.catalogService.getTags().subscribe({ next: (t) => this.tags.set(t) });
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.subscriptionService.getById(this.id).subscribe({
      next: (subscription) => {
        this.subscription.set(subscription);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  pause(): void {
    this.isActionInProgress.set(true);
    this.subscriptionService.pause(this.id).subscribe({
      next: () => {
        this.isActionInProgress.set(false);
        this.load();
      },
      error: () => {
        this.isActionInProgress.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  resume(): void {
    this.isActionInProgress.set(true);
    this.subscriptionService.resume(this.id).subscribe({
      next: () => {
        this.isActionInProgress.set(false);
        this.load();
      },
      error: () => {
        this.isActionInProgress.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  cancel(): void {
    this.isActionInProgress.set(true);
    const today = new Date().toISOString().slice(0, 10);
    this.subscriptionService.cancel(this.id, { effectiveDate: today, reason: null }).subscribe({
      next: () => {
        this.isActionInProgress.set(false);
        this.load();
      },
      error: () => {
        this.isActionInProgress.set(false);
        this.errorMessage.set('error.generic');
      },
    });
  }

  goToEdit(): void {
    void this.router.navigate(['/subscriptions', this.id, 'edit']);
  }
}
