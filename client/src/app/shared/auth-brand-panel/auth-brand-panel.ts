import { Component, ElementRef, OnDestroy, AfterViewInit, ViewChild } from '@angular/core';
import p5 from 'p5';
import TOPOLOGY, { VantaEffect } from 'vanta/dist/vanta.topology.min';
import { TranslatePipe } from '../../core/pipes/translate.pipe';

@Component({
  selector: 'app-auth-brand-panel',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './auth-brand-panel.html',
  styleUrl: './auth-brand-panel.scss',
})
export class AuthBrandPanel implements AfterViewInit, OnDestroy {
  @ViewChild('vantaHost', { static: true }) private readonly vantaHost!: ElementRef<HTMLDivElement>;

  private effect: VantaEffect | null = null;

  ngAfterViewInit(): void {
    const prefersReducedMotion = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    if (prefersReducedMotion) {
      return;
    }

    this.effect = TOPOLOGY({
      el: this.vantaHost.nativeElement,
      p5,
      mouseControls: true,
      touchControls: true,
      gyroControls: false,
      minHeight: 200,
      minWidth: 200,
      scale: 1,
      scaleMobile: 1,
      color: 0x818cf8,
      backgroundColor: 0x14123a,
    });
  }

  ngOnDestroy(): void {
    this.effect?.destroy();
    this.effect = null;
  }
}
