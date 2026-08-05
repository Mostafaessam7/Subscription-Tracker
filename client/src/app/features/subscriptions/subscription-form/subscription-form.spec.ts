import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { SubscriptionForm } from './subscription-form';
import { SubscriptionService } from '../../../core/services/subscription.service';
import { CatalogService } from '../../../core/services/catalog.service';
import { BillingFrequency, Subscription, SubscriptionStatus } from '../../../core/models/subscription.models';

function fakeSubscription(overrides: Partial<Subscription> = {}): Subscription {
  return {
    id: 'sub-1',
    name: 'Netflix',
    provider: 'Netflix Inc',
    logoUrl: null,
    websiteUrl: null,
    notes: null,
    categoryId: null,
    paymentMethodId: null,
    amount: 10,
    currencyCode: 'USD',
    billingFrequency: BillingFrequency.Monthly,
    customIntervalDays: null,
    startDate: '2026-01-01',
    trialEndDate: null,
    nextRenewalDate: null,
    endDate: null,
    autoRenewal: true,
    status: SubscriptionStatus.Active,
    tagIds: [],
    sharedUserIds: [],
    attachments: [],
    ...overrides,
  };
}

const emptyCatalog = { getCategories: () => of([]), getPaymentMethods: () => of([]), getTags: () => of([]) };

function createForm(
  routeId: string | null,
  serviceOverrides: Partial<{
    getById: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
  }> = {},
  navigate: ReturnType<typeof vi.fn> = vi.fn(),
): SubscriptionForm {
  TestBed.configureTestingModule({
    providers: [
      {
        provide: SubscriptionService,
        useValue: {
          getById: vi.fn().mockReturnValue(of(fakeSubscription())),
          create: vi.fn().mockReturnValue(of('new-id')),
          update: vi.fn().mockReturnValue(of(undefined)),
          ...serviceOverrides,
        },
      },
      { provide: CatalogService, useValue: emptyCatalog },
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => routeId } } } },
      { provide: Router, useValue: { navigate } },
    ],
  });

  const component = TestBed.runInInjectionContext(() => new SubscriptionForm());
  component.ngOnInit();
  return component;
}

describe('SubscriptionForm', () => {
  describe('create mode', () => {
    it('is not in edit mode when there is no route id', () => {
      const form = createForm(null);

      expect(form.isEditMode()).toBe(false);
    });

    it('marks the form invalid and touched instead of submitting when required fields are empty', () => {
      const create = vi.fn().mockReturnValue(of('new-id'));
      const form = createForm(null, { create });

      form.submit();

      expect(create).not.toHaveBeenCalled();
      expect(form.form.controls.name.touched).toBe(true);
    });

    it('coerces blank optional fields to null and sends selected tags on create', () => {
      const create = vi.fn().mockReturnValue(of('new-id'));
      const navigate = vi.fn();
      const form = createForm(null, { create }, navigate);

      form.form.patchValue({ name: 'Netflix', provider: 'Netflix Inc', amount: 15.99 });
      form.toggleTag('tag-1');
      form.toggleTag('tag-2');
      form.submit();

      expect(create).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'Netflix',
          provider: 'Netflix Inc',
          websiteUrl: null,
          notes: null,
          categoryId: null,
          paymentMethodId: null,
          tagIds: ['tag-1', 'tag-2'],
        }),
      );
      expect(navigate).toHaveBeenCalledWith(['/subscriptions', 'new-id']);
    });
  });

  describe('edit mode', () => {
    it('loads the subscription, patches the form, and pre-selects its tags', () => {
      const getById = vi.fn().mockReturnValue(
        of(fakeSubscription({ name: 'Spotify', amount: 9.99, tagIds: ['tag-1', 'tag-3'] })),
      );
      const form = createForm('sub-1', { getById });

      expect(form.isEditMode()).toBe(true);
      expect(form.form.controls.name.value).toBe('Spotify');
      expect(form.form.controls.amount.value).toBe(9.99);
      expect(form.isTagSelected('tag-1')).toBe(true);
      expect(form.isTagSelected('tag-2')).toBe(false);
    });

    it('locks immutable fields (billing frequency, start date, etc.) once loaded', () => {
      const form = createForm('sub-1');

      expect(form.form.controls.billingFrequency.disabled).toBe(true);
      expect(form.form.controls.startDate.disabled).toBe(true);
      expect(form.form.controls.customIntervalDays.disabled).toBe(true);
    });

    it('calls update (not create) with the current tag selection and navigates to the detail page', () => {
      const update = vi.fn().mockReturnValue(of(undefined));
      const navigate = vi.fn();
      const form = createForm('sub-1', { update }, navigate);

      form.toggleTag('tag-9');
      form.submit();

      expect(update).toHaveBeenCalledWith('sub-1', expect.objectContaining({ tagIds: ['tag-9'] }));
      expect(navigate).toHaveBeenCalledWith(['/subscriptions', 'sub-1']);
    });

    it('surfaces a generic error and stops loading when fetching the subscription fails', () => {
      const getById = vi.fn().mockReturnValue(throwError(() => new Error('boom')));
      const form = createForm('sub-1', { getById });

      expect(form.isLoading()).toBe(false);
      expect(form.errorMessage()).toBe('error.generic');
    });
  });

  describe('submit error handling', () => {
    it('maps a 400 response to a form-validation message', () => {
      const create = vi.fn().mockReturnValue(throwError(() => new HttpErrorResponse({ status: 400 })));
      const form = createForm(null, { create });
      form.form.patchValue({ name: 'X', provider: 'Y', amount: 5 });

      form.submit();

      expect(form.isSubmitting()).toBe(false);
      expect(form.errorMessage()).toBe('subscriptions.form.invalid');
    });

    it('maps any other failure to the generic error message', () => {
      const create = vi.fn().mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
      const form = createForm(null, { create });
      form.form.patchValue({ name: 'X', provider: 'Y', amount: 5 });

      form.submit();

      expect(form.errorMessage()).toBe('error.generic');
    });
  });

  describe('toggleTag', () => {
    it('adds an unselected tag and removes an already-selected one', () => {
      const form = createForm(null);

      form.toggleTag('tag-1');
      expect(form.isTagSelected('tag-1')).toBe(true);

      form.toggleTag('tag-1');
      expect(form.isTagSelected('tag-1')).toBe(false);
    });
  });
});
