import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { STATUS_ACTION_LABELS, STATUS_LABELS, TaskSummary, canAdvance } from '../../models/task';

@Component({
  selector: 'app-task-card',
  imports: [MatButtonModule],
  templateUrl: './task-card.html',
  styleUrl: './task-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskCard {
  readonly task = input.required<TaskSummary>();
  readonly selected = input(false);

  /** Emits the task ID when the card is opened. */
  readonly opened = output<string>();

  /** Emits when the user requests the next workflow transition. */
  readonly advanced = output<TaskSummary>();

  protected readonly statusLabel = computed(() => STATUS_LABELS[this.task().status]);
  protected readonly actionLabel = computed(() => STATUS_ACTION_LABELS[this.task().status]);
  protected readonly canAdvance = computed(() => canAdvance(this.task()));

  protected advance(event: Event): void {
    event.stopPropagation();
    this.advanced.emit(this.task());
  }
}
