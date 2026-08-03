import { Component, ElementRef, OnDestroy, AfterViewInit, ViewChild, inject } from '@angular/core';
import * as THREE from 'three';
import NET, { VantaEffect } from 'vanta/dist/vanta.net.min';
import { TranslatePipe } from '../../core/pipes/translate.pipe';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-auth-brand-panel',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './auth-brand-panel.html',
  styleUrl: './auth-brand-panel.scss',
})
export class AuthBrandPanel implements AfterViewInit, OnDestroy {
  @ViewChild('vantaHost', { static: true }) private readonly vantaHost!: ElementRef<HTMLDivElement>;

  private readonly themeService = inject(ThemeService);
  private effect: VantaEffect | null = null;

  ngAfterViewInit(): void {
    const prefersReducedMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    if (prefersReducedMotion) {
      return;
    }

    const isDark = this.themeService.theme() === 'dark';

    this.effect = NET({
      el: this.vantaHost.nativeElement,
      THREE,
      mouseControls: true,
      touchControls: true,
      gyroControls: false,
      minHeight: 200,
      minWidth: 200,
      scale: 1,
      scaleMobile: 1,
      color: isDark ? 0x818cf8 : 0xffffff,
      backgroundColor: isDark ? 0x14161a : 0x4f46e5,
      points: 11,
      maxDistance: 22,
      spacing: 17,
      showDots: true,
    });
  }

  ngOnDestroy(): void {
    this.effect?.destroy();
    this.effect = null;
  }
}
