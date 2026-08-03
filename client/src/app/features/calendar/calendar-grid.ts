import { Subscription } from '../../core/models/subscription.models';

export interface CalendarDay {
  date: Date;
  dateKey: string;
  inCurrentMonth: boolean;
  isToday: boolean;
  renewals: Subscription[];
}

export function toDateKey(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}

export function buildRenewalsByDate(subscriptions: readonly Subscription[]): Map<string, Subscription[]> {
  const map = new Map<string, Subscription[]>();
  for (const subscription of subscriptions) {
    if (!subscription.nextRenewalDate) {
      continue;
    }
    const list = map.get(subscription.nextRenewalDate) ?? [];
    list.push(subscription);
    map.set(subscription.nextRenewalDate, list);
  }
  return map;
}

/**
 * Builds a 6x7 (42-cell) month grid starting on the Sunday on/before the 1st of `monthCursor`'s month. Cell dates
 * are built by adding whole days via the Date(y, m, d) overload rather than incrementing a running Date instance,
 * so each cell is independent and immune to any single day's arithmetic drifting the rest via mutation.
 */
export function buildCalendarWeeks(
  monthCursor: Date, today: Date, renewalsByDate: Map<string, Subscription[]>,
): CalendarDay[][] {
  const year = monthCursor.getFullYear();
  const month = monthCursor.getMonth();
  const firstOfMonth = new Date(year, month, 1);
  const gridStartOffsetDays = -firstOfMonth.getDay();
  const todayKey = toDateKey(today);

  const days: CalendarDay[] = [];
  for (let i = 0; i < 42; i++) {
    const date = new Date(year, month, 1 + gridStartOffsetDays + i);
    const dateKey = toDateKey(date);
    days.push({
      date,
      dateKey,
      inCurrentMonth: date.getMonth() === month,
      isToday: dateKey === todayKey,
      renewals: renewalsByDate.get(dateKey) ?? [],
    });
  }

  const weeks: CalendarDay[][] = [];
  for (let i = 0; i < days.length; i += 7) {
    weeks.push(days.slice(i, i + 7));
  }
  return weeks;
}
