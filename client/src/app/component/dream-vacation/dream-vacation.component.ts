import {
  Component,
  OnDestroy,
  signal,
  computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SliderModule } from 'primeng/slider';
import { VacationService } from '../../services/vacation.service';
import {
  VacationBuildResponse,
  SimulatorRequest,
  SimulatorResponse,
  TravelTwin,
} from '../../models/vacation.model';

type InputMode = 'text' | 'audio' | 'image';

/** Maps each Travel Twin to a CSS class applied to the host element. */
const TWIN_THEME: Record<TravelTwin, string> = {
  'Luxury Traveler':   'theme-luxury',
  'Nature Escapist':   'theme-nature',
  'Explorer':          'theme-explorer',
  'Urban Discoverer':  'theme-urban',
  'Adrenaline Hunter': 'theme-adrenaline',
};

@Component({
  selector: 'app-dream-vacation',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    TooltipModule,
    ProgressSpinnerModule,
    SliderModule,
  ],
  templateUrl: './dream-vacation.component.html',
  styleUrl: './dream-vacation.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DreamVacationComponent implements OnDestroy {
  private readonly destroy$ = new Subject<void>();
  private readonly simChange$ = new Subject<SimulatorRequest>();

  // ── Input section ─────────────────────────────────────────────────────────
  inputMode = signal<InputMode>('text');
  textInput  = signal('');
  isDragOver = signal(false);
  audioFile  = signal<File | null>(null);
  imageFile  = signal<File | null>(null);
  imagePreviewUrl = signal<string | null>(null);

  // ── State ──────────────────────────────────────────────────────────────────
  loading     = signal(false);
  buildResult = signal<VacationBuildResponse | null>(null);
  simResult   = signal<SimulatorResponse | null>(null);
  errorMsg    = signal<string | null>(null);

  // Recording state
  isRecording = signal(false);
  private mediaRecorder?: MediaRecorder;
  private audioChunks: BlobPart[] = [];

  // ── Simulator sliders ─────────────────────────────────────────────────────
  luxury      = signal(3);
  nature      = signal(3);
  budget      = signal(0);       // 0 = no limit
  attractions = signal(1);

  // ── Computed ───────────────────────────────────────────────────────────────
  themeClass = computed(() => {
    const twin = this.buildResult()?.travelTwin;
    return twin ? TWIN_THEME[twin] : '';
  });

  canSubmit = computed(() => {
    if (this.loading()) return false;
    switch (this.inputMode()) {
      case 'text':  return this.textInput().trim().length > 0;
      case 'audio': return this.audioFile() !== null;
      case 'image': return this.imageFile() !== null;
    }
  });

  constructor(private vacationService: VacationService) {
    // Debounce simulator slider changes — avoids hammering the API while dragging
    this.simChange$
      .pipe(debounceTime(350), takeUntil(this.destroy$))
      .subscribe(req => this.runSimulator(req));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Input mode switching ──────────────────────────────────────────────────

  setMode(mode: InputMode): void {
    this.inputMode.set(mode);
    this.errorMsg.set(null);
  }

  // ── Text submission ───────────────────────────────────────────────────────

  submitText(): void {
    const text = this.textInput().trim();
    if (!text) return;
    this.callBuild(this.vacationService.buildFromText(text));
  }

  // ── Audio recording ───────────────────────────────────────────────────────

  async toggleRecording(): Promise<void> {
    if (this.isRecording()) {
      this.mediaRecorder?.stop();
      this.isRecording.set(false);
      return;
    }

    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.audioChunks = [];
      this.mediaRecorder = new MediaRecorder(stream);

      this.mediaRecorder.ondataavailable = e => this.audioChunks.push(e.data);
      this.mediaRecorder.onstop = () => {
        const blob = new Blob(this.audioChunks, { type: 'audio/webm' });
        const file = new File([blob], 'recording.webm', { type: 'audio/webm' });
        this.audioFile.set(file);
        stream.getTracks().forEach(t => t.stop());
      };

      this.mediaRecorder.start();
      this.isRecording.set(true);
    } catch {
      this.errorMsg.set('Microphone access denied. Please allow microphone access and try again.');
    }
  }

  submitAudio(): void {
    const file = this.audioFile();
    if (!file) return;
    this.callBuild(this.vacationService.buildFromAudio(file));
  }

  clearAudio(): void {
    this.audioFile.set(null);
    this.errorMsg.set(null);
  }

  // ── Image upload ──────────────────────────────────────────────────────────

  onDragOver(e: DragEvent): void {
    e.preventDefault();
    this.isDragOver.set(true);
  }

  onDragLeave(): void {
    this.isDragOver.set(false);
  }

  onDrop(e: DragEvent): void {
    e.preventDefault();
    this.isDragOver.set(false);
    const file = e.dataTransfer?.files[0];
    if (file) this.setImageFile(file);
  }

  onImageFileSelected(e: Event): void {
    const file = (e.target as HTMLInputElement).files?.[0];
    if (file) this.setImageFile(file);
  }

  private setImageFile(file: File): void {
    if (!file.type.startsWith('image/')) {
      this.errorMsg.set('Please upload an image file (jpeg, png, gif, or webp).');
      return;
    }
    this.imageFile.set(file);
    this.errorMsg.set(null);
    const reader = new FileReader();
    reader.onload = () => this.imagePreviewUrl.set(reader.result as string);
    reader.readAsDataURL(file);
  }

  clearImage(): void {
    this.imageFile.set(null);
    this.imagePreviewUrl.set(null);
    this.errorMsg.set(null);
  }

  submitImage(): void {
    const file = this.imageFile();
    if (!file) return;
    this.callBuild(this.vacationService.buildFromImage(file));
  }

  // ── Core build call ───────────────────────────────────────────────────────

  private callBuild(obs: ReturnType<VacationService['buildFromText']>): void {
    this.loading.set(true);
    this.errorMsg.set(null);
    this.buildResult.set(null);
    this.simResult.set(null);

    obs.pipe(takeUntil(this.destroy$)).subscribe({
      next: res => {
        this.buildResult.set(res);
        this.loading.set(false);
        // Auto-run the simulator with default slider values
        this.onSliderChange();
      },
      error: err => {
        this.errorMsg.set(err?.error?.detail ?? err?.message ?? 'Something went wrong. Please try again.');
        this.loading.set(false);
      },
    });
  }

  // ── Simulator ─────────────────────────────────────────────────────────────

  onSliderChange(): void {
    this.simChange$.next({
      luxuryLevel:     this.luxury(),
      natureVibe:      this.nature(),
      budgetLimit:     this.budget(),
      attractionCount: this.attractions(),
    });
  }

  private runSimulator(req: SimulatorRequest): void {
    this.vacationService
      .simulate(req)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: res => this.simResult.set(res),
        error: () => { /* silent — keep previous sim results visible */ },
      });
  }

  // ── Travel Twin helpers ───────────────────────────────────────────────────

  twinIcon(twin: TravelTwin | undefined): string {
    switch (twin) {
      case 'Luxury Traveler':   return 'pi pi-star';
      case 'Nature Escapist':   return 'pi pi-globe';
      case 'Explorer':          return 'pi pi-compass';
      case 'Urban Discoverer':  return 'pi pi-map-marker';
      case 'Adrenaline Hunter': return 'pi pi-bolt';
      default:                  return 'pi pi-heart';
    }
  }

  // ── Budget slider label ───────────────────────────────────────────────────

  budgetLabel = computed(() =>
    this.budget() === 0 ? 'No limit' : `$${this.budget().toLocaleString()}`
  );
}
