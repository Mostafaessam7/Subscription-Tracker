declare module 'vanta/dist/vanta.net.min' {
  import type * as THREE from 'three';

  export interface VantaNetOptions {
    el: HTMLElement | null;
    THREE?: typeof THREE;
    mouseControls?: boolean;
    touchControls?: boolean;
    gyroControls?: boolean;
    minHeight?: number;
    minWidth?: number;
    scale?: number;
    scaleMobile?: number;
    color?: number;
    backgroundColor?: number;
    points?: number;
    maxDistance?: number;
    spacing?: number;
    showDots?: boolean;
  }

  export interface VantaEffect {
    destroy(): void;
    setOptions(options: Partial<VantaNetOptions>): void;
  }

  export default function NET(options: VantaNetOptions): VantaEffect;
}

declare module 'vanta/dist/vanta.topology.min' {
  // TOPOLOGY (unlike NET/WAVES) is a p5.js effect, not a THREE.js one - it takes a `p5`
  // constructor instead of `THREE`.
  export interface VantaTopologyOptions {
    el: HTMLElement | null;
    p5?: unknown;
    mouseControls?: boolean;
    touchControls?: boolean;
    gyroControls?: boolean;
    minHeight?: number;
    minWidth?: number;
    scale?: number;
    scaleMobile?: number;
    color?: number;
    backgroundColor?: number;
  }

  export interface VantaEffect {
    destroy(): void;
    setOptions(options: Partial<VantaTopologyOptions>): void;
  }

  export default function TOPOLOGY(options: VantaTopologyOptions): VantaEffect;
}

declare module 'vanta/dist/vanta.waves.min' {
  import type * as THREE from 'three';

  export interface VantaWavesOptions {
    el: HTMLElement | null;
    THREE?: typeof THREE;
    mouseControls?: boolean;
    touchControls?: boolean;
    gyroControls?: boolean;
    minHeight?: number;
    minWidth?: number;
    scale?: number;
    scaleMobile?: number;
    color?: number;
    shininess?: number;
    waveHeight?: number;
    waveSpeed?: number;
    zoom?: number;
  }

  export interface VantaEffect {
    destroy(): void;
    setOptions(options: Partial<VantaWavesOptions>): void;
  }

  export default function WAVES(options: VantaWavesOptions): VantaEffect;
}
