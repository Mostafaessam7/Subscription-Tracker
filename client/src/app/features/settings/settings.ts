import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CatalogService } from '../../core/services/catalog.service';
import { PermissionsService } from '../../core/services/permissions.service';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { Permissions } from '../../core/models/permissions';
import { Category, PaymentMethod, PaymentMethodType, Tag } from '../../core/models/catalog.models';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [ReactiveFormsModule, TranslatePipe],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
})
export class Settings implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly catalogService = inject(CatalogService);
  protected readonly permissions = inject(PermissionsService);
  protected readonly Permissions = Permissions;

  protected readonly PaymentMethodType = PaymentMethodType;
  protected readonly paymentMethodTypeOptions = [
    PaymentMethodType.CreditCard,
    PaymentMethodType.DebitCard,
    PaymentMethodType.Cash,
    PaymentMethodType.BankAccount,
    PaymentMethodType.PayPal,
    PaymentMethodType.ApplePay,
    PaymentMethodType.GooglePay,
    PaymentMethodType.Crypto,
    PaymentMethodType.Other,
  ];

  readonly categories = signal<Category[]>([]);
  readonly tags = signal<Tag[]>([]);
  readonly paymentMethods = signal<PaymentMethod[]>([]);
  readonly errorMessage = signal<string | null>(null);

  private editingCategoryId: string | null = null;
  private editingTagId: string | null = null;
  private editingPaymentMethodId: string | null = null;

  readonly categoryForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    color: [''],
    icon: [''],
  });

  readonly tagForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(50)]],
    color: [''],
  });

  readonly paymentMethodForm = this.formBuilder.nonNullable.group({
    type: [PaymentMethodType.CreditCard, [Validators.required]],
    label: ['', [Validators.required, Validators.maxLength(100)]],
    maskedDetails: [''],
    isDefault: [false],
  });

  ngOnInit(): void {
    this.reloadAll();
  }

  private reloadAll(): void {
    this.catalogService.getCategories().subscribe({ next: (c) => this.categories.set(c) });
    this.catalogService.getTags().subscribe({ next: (t) => this.tags.set(t) });
    this.catalogService.getPaymentMethods().subscribe({ next: (p) => this.paymentMethods.set(p) });
  }

  private handleError(): void {
    this.errorMessage.set('error.generic');
  }

  submitCategory(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    const raw = this.categoryForm.getRawValue();
    const request = { name: raw.name, color: raw.color || null, icon: raw.icon || null };

    const done = () => {
      this.categoryForm.reset({ name: '', color: '', icon: '' });
      this.editingCategoryId = null;
      this.catalogService.getCategories().subscribe({ next: (c) => this.categories.set(c) });
    };

    if (this.editingCategoryId) {
      this.catalogService.updateCategory(this.editingCategoryId, request).subscribe({ next: done, error: () => this.handleError() });
    } else {
      this.catalogService.createCategory(request).subscribe({ next: done, error: () => this.handleError() });
    }
  }

  editCategory(category: Category): void {
    this.editingCategoryId = category.id;
    this.categoryForm.setValue({ name: category.name, color: category.color ?? '', icon: category.icon ?? '' });
  }

  cancelCategoryEdit(): void {
    this.editingCategoryId = null;
    this.categoryForm.reset({ name: '', color: '', icon: '' });
  }

  deleteCategory(category: Category): void {
    this.catalogService.deleteCategory(category.id).subscribe({
      next: () => this.categories.update((list) => list.filter((c) => c.id !== category.id)),
      error: () => this.handleError(),
    });
  }

  submitTag(): void {
    if (this.tagForm.invalid) {
      this.tagForm.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    const raw = this.tagForm.getRawValue();
    const request = { name: raw.name, color: raw.color || null };

    const done = () => {
      this.tagForm.reset({ name: '', color: '' });
      this.editingTagId = null;
      this.catalogService.getTags().subscribe({ next: (t) => this.tags.set(t) });
    };

    if (this.editingTagId) {
      this.catalogService.updateTag(this.editingTagId, request).subscribe({ next: done, error: () => this.handleError() });
    } else {
      this.catalogService.createTag(request).subscribe({ next: done, error: () => this.handleError() });
    }
  }

  editTag(tag: Tag): void {
    this.editingTagId = tag.id;
    this.tagForm.setValue({ name: tag.name, color: tag.color ?? '' });
  }

  cancelTagEdit(): void {
    this.editingTagId = null;
    this.tagForm.reset({ name: '', color: '' });
  }

  deleteTag(tag: Tag): void {
    this.catalogService.deleteTag(tag.id).subscribe({
      next: () => this.tags.update((list) => list.filter((t) => t.id !== tag.id)),
      error: () => this.handleError(),
    });
  }

  submitPaymentMethod(): void {
    if (this.paymentMethodForm.invalid) {
      this.paymentMethodForm.markAllAsTouched();
      return;
    }

    this.errorMessage.set(null);
    const raw = this.paymentMethodForm.getRawValue();

    const done = () => {
      this.paymentMethodForm.reset({ type: PaymentMethodType.CreditCard, label: '', maskedDetails: '', isDefault: false });
      this.editingPaymentMethodId = null;
      this.catalogService.getPaymentMethods().subscribe({ next: (p) => this.paymentMethods.set(p) });
    };

    if (this.editingPaymentMethodId) {
      this.catalogService
        .updatePaymentMethod(this.editingPaymentMethodId, { label: raw.label, isDefault: raw.isDefault })
        .subscribe({ next: done, error: () => this.handleError() });
    } else {
      this.catalogService
        .createPaymentMethod({
          type: raw.type,
          label: raw.label,
          maskedDetails: raw.maskedDetails || null,
          isDefault: raw.isDefault,
        })
        .subscribe({ next: done, error: () => this.handleError() });
    }
  }

  editPaymentMethod(paymentMethod: PaymentMethod): void {
    this.editingPaymentMethodId = paymentMethod.id;
    this.paymentMethodForm.setValue({
      type: paymentMethod.type,
      label: paymentMethod.label,
      maskedDetails: paymentMethod.maskedDetails ?? '',
      isDefault: paymentMethod.isDefault,
    });
  }

  cancelPaymentMethodEdit(): void {
    this.editingPaymentMethodId = null;
    this.paymentMethodForm.reset({ type: PaymentMethodType.CreditCard, label: '', maskedDetails: '', isDefault: false });
  }

  deletePaymentMethod(paymentMethod: PaymentMethod): void {
    this.catalogService.deletePaymentMethod(paymentMethod.id).subscribe({
      next: () => this.paymentMethods.update((list) => list.filter((p) => p.id !== paymentMethod.id)),
      error: () => this.handleError(),
    });
  }

  isEditingCategory(category: Category): boolean {
    return this.editingCategoryId === category.id;
  }

  isEditingTag(tag: Tag): boolean {
    return this.editingTagId === tag.id;
  }

  isEditingPaymentMethod(paymentMethod: PaymentMethod): boolean {
    return this.editingPaymentMethodId === paymentMethod.id;
  }

  get isEditingAnyCategory(): boolean {
    return this.editingCategoryId !== null;
  }

  get isEditingAnyTag(): boolean {
    return this.editingTagId !== null;
  }

  get isEditingAnyPaymentMethod(): boolean {
    return this.editingPaymentMethodId !== null;
  }
}
