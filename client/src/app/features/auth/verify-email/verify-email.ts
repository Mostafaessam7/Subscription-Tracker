import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { TranslatePipe } from '../../../core/pipes/translate.pipe';

type VerifyEmailState = 'verifying' | 'success' | 'error';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './verify-email.html',
  styleUrl: '../login/login.scss',
})
export class VerifyEmail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  readonly state = signal<VerifyEmailState>('verifying');

  ngOnInit(): void {
    const userId = this.route.snapshot.queryParamMap.get('userId');
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!userId || !token) {
      this.state.set('error');
      return;
    }

    this.authService.verifyEmail({ userId, token }).subscribe({
      next: () => this.state.set('success'),
      error: () => this.state.set('error'),
    });
  }
}
