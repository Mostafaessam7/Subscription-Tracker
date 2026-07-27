import { Pipe, PipeTransform, inject } from '@angular/core';
import { TranslationService } from '../services/translation.service';

@Pipe({ name: 'translate', pure: false, standalone: true })
export class TranslatePipe implements PipeTransform {
  private readonly translationService = inject(TranslationService);

  transform(key: string, ...args: string[]): string {
    // Reading the signal here keeps this pipe reactive to locale changes despite pure: false being needed
    // because Angular pipes can't otherwise re-run when an unrelated signal (the loaded dictionary) changes.
    this.translationService.locale();
    return this.translationService.translate(key, ...args);
  }
}
