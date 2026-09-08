/**
 * Generates and plays a phone ringing sound using Web Audio API.
 * No external audio files required.
 */
export class RingtonePlayer {
  private audioContext: AudioContext | null = null;
  private intervalId: number | null = null;
  private playing = false;

  start(): void {
    if (this.playing) return;
    this.playing = true;

    this.audioContext = new AudioContext();
    this.playRingCycle();
    // Repeat every 2 seconds
    this.intervalId = window.setInterval(() => {
      if (this.playing) this.playRingCycle();
    }, 2000);
  }

  stop(): void {
    this.playing = false;
    if (this.intervalId) {
      window.clearInterval(this.intervalId);
      this.intervalId = null;
    }
    if (this.audioContext) {
      this.audioContext.close().catch(() => {});
      this.audioContext = null;
    }
  }

  private playRingCycle(): void {
    if (!this.audioContext || this.audioContext.state === 'closed') return;

    const ctx = this.audioContext;
    const now = ctx.currentTime;

    // Two short beeps (440 Hz + 480 Hz like a real phone)
    for (let i = 0; i < 2; i++) {
      const start = now + i * 0.4;
      this.playBeep(ctx, start, 0.25, [440, 480]);
    }
  }

  private playBeep(ctx: AudioContext, startTime: number, duration: number, frequencies: number[]): void {
    const gainNode = ctx.createGain();
    gainNode.gain.setValueAtTime(0, startTime);
    gainNode.gain.linearRampToValueAtTime(0.3, startTime + 0.02);
    gainNode.gain.setValueAtTime(0.3, startTime + duration - 0.03);
    gainNode.gain.linearRampToValueAtTime(0, startTime + duration);
    gainNode.connect(ctx.destination);

    frequencies.forEach(freq => {
      const oscillator = ctx.createOscillator();
      oscillator.type = 'sine';
      oscillator.frequency.setValueAtTime(freq, startTime);
      oscillator.connect(gainNode);
      oscillator.start(startTime);
      oscillator.stop(startTime + duration);
    });
  }
}

export const ringtonePlayer = new RingtonePlayer();
