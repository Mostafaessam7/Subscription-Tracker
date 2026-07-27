import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type Locale = 'en' | 'ar';

const LOCALE_KEY = 'st.locale';
const RTL_LOCALES: ReadonlySet<Locale> = new Set(['ar']);

@Injectable({ providedIn: 'root' })
export class TranslationService {
  private translations: Record<string, string> = {};

  readonly locale = signal<Locale>(this.resolveInitialLocale());
  readonly isRtl = signal<boolean>(RTL_LOCALES.has(this.locale()));

  constructor(private readonly http: HttpClient) {}

  async load(): Promise<void> {
    await this.loadLocale(this.locale());
  }

  async setLocale(locale: Locale): Promise<void> {
    if (locale === this.locale()) {
      return;
    }

    await this.loadLocale(locale);
    localStorage.setItem(LOCALE_KEY, locale);
    this.locale.set(locale);
    this.isRtl.set(RTL_LOCALES.has(locale));
    document.documentElement.setAttribute('lang', locale);
    document.documentElement.setAttribute('dir', RTL_LOCALES.has(locale) ? 'rtl' : 'ltr');
  }

  translate(key: string, ...args: string[]): string {
    const template = this.translations[key] ?? key;
    return args.reduce<string>((text, arg, index) => text.replace(`{${index}}`, arg), template);
  }

  private async loadLocale(locale: Locale): Promise<void> {
    this.translations = await firstValueFrom(this.http.get<Record<string, string>>(`/i18n/${locale}.json`));
  }

  private resolveInitialLocale(): Locale {
    const stored = localStorage.getItem(LOCALE_KEY);
    if (stored === 'en' || stored === 'ar') {
      return stored;
    }

    return navigator.language.toLowerCase().startsWith('ar') ? 'ar' : 'en';
  }
}
