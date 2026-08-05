import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { Settings } from './settings';
import { CatalogService } from '../../core/services/catalog.service';
import { Category, PaymentMethod, PaymentMethodType, Tag } from '../../core/models/catalog.models';

function fakeCategory(overrides: Partial<Category> = {}): Category {
  return { id: 'cat-1', name: 'Streaming', color: '#ff0000', icon: 'tv', ...overrides };
}

function fakeTag(overrides: Partial<Tag> = {}): Tag {
  return { id: 'tag-1', name: 'Essential', color: '#00ff00', ...overrides };
}

function fakePaymentMethod(overrides: Partial<PaymentMethod> = {}): PaymentMethod {
  return { id: 'pm-1', type: PaymentMethodType.CreditCard, label: 'Visa', maskedDetails: '**** 1234', isDefault: false, ...overrides };
}

function createSettings(catalogOverrides: Partial<Record<string, ReturnType<typeof vi.fn>>> = {}): Settings {
  TestBed.configureTestingModule({
    providers: [
      {
        provide: CatalogService,
        useValue: {
          getCategories: vi.fn().mockReturnValue(of([])),
          getTags: vi.fn().mockReturnValue(of([])),
          getPaymentMethods: vi.fn().mockReturnValue(of([])),
          createCategory: vi.fn().mockReturnValue(of('new-cat-id')),
          updateCategory: vi.fn().mockReturnValue(of(undefined)),
          deleteCategory: vi.fn().mockReturnValue(of(undefined)),
          createTag: vi.fn().mockReturnValue(of('new-tag-id')),
          updateTag: vi.fn().mockReturnValue(of(undefined)),
          deleteTag: vi.fn().mockReturnValue(of(undefined)),
          createPaymentMethod: vi.fn().mockReturnValue(of('new-pm-id')),
          updatePaymentMethod: vi.fn().mockReturnValue(of(undefined)),
          deletePaymentMethod: vi.fn().mockReturnValue(of(undefined)),
          ...catalogOverrides,
        },
      },
    ],
  });

  const component = TestBed.runInInjectionContext(() => new Settings());
  component.ngOnInit();
  return component;
}

describe('Settings', () => {
  describe('categories', () => {
    it('does not submit an invalid (empty-name) category form', () => {
      const createCategory = vi.fn().mockReturnValue(of('id'));
      const settings = createSettings({ createCategory });

      settings.submitCategory();

      expect(createCategory).not.toHaveBeenCalled();
      expect(settings.categoryForm.controls.name.touched).toBe(true);
    });

    it('creates a category with null instead of empty strings for optional fields', () => {
      const createCategory = vi.fn().mockReturnValue(of('id'));
      const settings = createSettings({ createCategory });

      settings.categoryForm.setValue({ name: 'Gaming', color: '', icon: '' });
      settings.submitCategory();

      expect(createCategory).toHaveBeenCalledWith({ name: 'Gaming', color: null, icon: null });
    });

    it('switches to update mode via editCategory and back via cancelCategoryEdit', () => {
      const settings = createSettings();
      const category = fakeCategory();

      expect(settings.isEditingAnyCategory).toBe(false);

      settings.editCategory(category);
      expect(settings.isEditingAnyCategory).toBe(true);
      expect(settings.isEditingCategory(category)).toBe(true);
      expect(settings.categoryForm.controls.name.value).toBe('Streaming');

      settings.cancelCategoryEdit();
      expect(settings.isEditingAnyCategory).toBe(false);
    });

    it('calls updateCategory (not create) while editing, then clears edit state', () => {
      const updateCategory = vi.fn().mockReturnValue(of(undefined));
      const settings = createSettings({ updateCategory });

      settings.editCategory(fakeCategory({ id: 'cat-1' }));
      settings.categoryForm.patchValue({ name: 'Renamed' });
      settings.submitCategory();

      expect(updateCategory).toHaveBeenCalledWith('cat-1', { name: 'Renamed', color: '#ff0000', icon: 'tv' });
      expect(settings.isEditingAnyCategory).toBe(false);
    });

    it('removes the category from the list on successful delete', () => {
      const settings = createSettings();
      settings.categories.set([fakeCategory({ id: 'cat-1' }), fakeCategory({ id: 'cat-2' })]);

      settings.deleteCategory(fakeCategory({ id: 'cat-1' }));

      expect(settings.categories().map((c) => c.id)).toEqual(['cat-2']);
    });

    it('sets a generic error message when deleting a category fails', () => {
      const deleteCategory = vi.fn().mockReturnValue(throwError(() => new Error('boom')));
      const settings = createSettings({ deleteCategory });
      settings.categories.set([fakeCategory({ id: 'cat-1' })]);

      settings.deleteCategory(fakeCategory({ id: 'cat-1' }));

      expect(settings.errorMessage()).toBe('error.generic');
      expect(settings.categories().length).toBe(1);
    });
  });

  describe('tags', () => {
    it('creates a tag with a null color when left blank', () => {
      const createTag = vi.fn().mockReturnValue(of('id'));
      const settings = createSettings({ createTag });

      settings.tagForm.setValue({ name: 'Personal', color: '' });
      settings.submitTag();

      expect(createTag).toHaveBeenCalledWith({ name: 'Personal', color: null });
    });

    it('tracks per-tag edit state independently of other tags', () => {
      const settings = createSettings();
      const tagA = fakeTag({ id: 'tag-a' });
      const tagB = fakeTag({ id: 'tag-b' });

      settings.editTag(tagA);

      expect(settings.isEditingTag(tagA)).toBe(true);
      expect(settings.isEditingTag(tagB)).toBe(false);
    });
  });

  describe('payment methods', () => {
    it('creates a payment method, coercing a blank masked-details field to null', () => {
      const createPaymentMethod = vi.fn().mockReturnValue(of('id'));
      const settings = createSettings({ createPaymentMethod });

      settings.paymentMethodForm.setValue({
        type: PaymentMethodType.PayPal,
        label: 'PayPal',
        maskedDetails: '',
        isDefault: true,
      });
      settings.submitPaymentMethod();

      expect(createPaymentMethod).toHaveBeenCalledWith({
        type: PaymentMethodType.PayPal,
        label: 'PayPal',
        maskedDetails: null,
        isDefault: true,
      });
    });

    it('only sends label and isDefault on update (type/maskedDetails are immutable)', () => {
      const updatePaymentMethod = vi.fn().mockReturnValue(of(undefined));
      const settings = createSettings({ updatePaymentMethod });

      settings.editPaymentMethod(fakePaymentMethod({ id: 'pm-1' }));
      settings.paymentMethodForm.patchValue({ label: 'Visa Debit', isDefault: true });
      settings.submitPaymentMethod();

      expect(updatePaymentMethod).toHaveBeenCalledWith('pm-1', { label: 'Visa Debit', isDefault: true });
    });

    it('resets the form after a successful submit', () => {
      const settings = createSettings();

      settings.paymentMethodForm.setValue({
        type: PaymentMethodType.Cash,
        label: 'Cash',
        maskedDetails: '',
        isDefault: false,
      });
      settings.submitPaymentMethod();

      expect(settings.paymentMethodForm.controls.label.value).toBe('');
      expect(settings.paymentMethodForm.controls.type.value).toBe(PaymentMethodType.CreditCard);
    });
  });
});
