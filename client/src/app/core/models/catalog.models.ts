export enum PaymentMethodType {
  CreditCard = 0,
  DebitCard = 1,
  Cash = 2,
  BankAccount = 3,
  PayPal = 4,
  ApplePay = 5,
  GooglePay = 6,
  Crypto = 7,
  Other = 8,
}

export interface Category {
  id: string;
  name: string;
  color: string | null;
  icon: string | null;
}

export interface Tag {
  id: string;
  name: string;
  color: string | null;
}

export interface PaymentMethod {
  id: string;
  type: PaymentMethodType;
  label: string;
  maskedDetails: string | null;
  isDefault: boolean;
}

export interface CreateCategoryRequest {
  name: string;
  color?: string | null;
  icon?: string | null;
}

export type UpdateCategoryRequest = CreateCategoryRequest;

export interface CreateTagRequest {
  name: string;
  color?: string | null;
}

export type UpdateTagRequest = CreateTagRequest;

export interface CreatePaymentMethodRequest {
  type: PaymentMethodType;
  label: string;
  maskedDetails?: string | null;
  isDefault: boolean;
}

export interface UpdatePaymentMethodRequest {
  label: string;
  isDefault: boolean;
}
