import { Component, ElementRef, OnDestroy, AfterViewInit, ViewChild } from '@angular/core';
import * as THREE from 'three';
import WAVES, { VantaEffect } from 'vanta/dist/vanta.waves.min';
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

    this.effect = WAVES({
      el: this.vantaHost.nativeElement,
      THREE,
      mouseControls: true,
      touchControls: true,
      gyroControls: false,
      minHeight: 200,
      minWidth: 200,
      scale: 1,
      scaleMobile: 1,
      color: 0x4338ca,
      shininess: 45,
      waveHeight: 22,
      waveSpeed: 0.85,
      zoom: 0.85,
    });
  }

  ngOnDestroy(): void {
    this.effect?.destroy();
    this.effect = null;
  }
}
