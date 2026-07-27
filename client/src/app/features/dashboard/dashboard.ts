import { Component } from '@angular/core';
import { TranslatePipe } from '../../core/pipes/translate.pipe';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [TranslatePipe],
  template: `<h1>{{ 'nav.dashboard' | translate }}</h1>`,
})
export class Dashboard {}
