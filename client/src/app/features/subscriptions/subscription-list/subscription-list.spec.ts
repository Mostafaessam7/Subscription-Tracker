import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { SubscriptionList } from './subscription-list';
import { SubscriptionService } from '../../../core/services/subscription.service';
import { PagedList, Subscription, SubscriptionStatus } from '../../../core/models/subscription.models';

function emptyPage(): PagedList<Subscription> {
  return {
    items: [],
    totalCount: 0,
    pageNumber: 1,
    pageSize: 20,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}

function createList(getSubscriptions: ReturnType<typeof vi.fn>): SubscriptionList {
  TestBed.configureTestingModule({
    providers: [{ provide: SubscriptionService, useValue: { getSubscriptions } }],
  });

  return TestBed.runInInjectionContext(() => new SubscriptionList());
}

describe('SubscriptionList', () => {
  it('loads the first page on construction', () => {
    const getSubscriptions = vi.fn().mockReturnValue(of(emptyPage()));
    createList(getSubscriptions);

    expect(getSubscriptions).toHaveBeenCalledWith(
      expect.objectContaining({ pageNumber: 1, pageSize: 20, sortBy: 'name', sortDescending: false }),
    );
  });

  it('resets to page 1 and reloads when filters are applied', () => {
    const getSubscriptions = vi.fn().mockReturnValue(of(emptyPage()));
    const list = createList(getSubscriptions);
    list.pageNumber = 3;

    list.searchTerm = 'netflix';
    list.status = SubscriptionStatus.Active;
    list.applyFilters();

    expect(list.pageNumber).toBe(1);
    expect(getSubscriptions).toHaveBeenLastCalledWith(
      expect.objectContaining({ pageNumber: 1, searchTerm: 'netflix', status: SubscriptionStatus.Active }),
    );
  });

  it('sends null for an unset search term and status instead of empty strings', () => {
    const getSubscriptions = vi.fn().mockReturnValue(of(emptyPage()));
    const list = createList(getSubscriptions);
    getSubscriptions.mockClear();

    list.searchTerm = '';
    list.status = '';
    list.applyFilters();

    expect(getSubscriptions).toHaveBeenCalledWith(expect.objectContaining({ searchTerm: null, status: null }));
  });

  it('toggles sort direction when sorting the same column twice, and resets it when switching columns', () => {
    const getSubscriptions = vi.fn().mockReturnValue(of(emptyPage()));
    const list = createList(getSubscriptions);

    list.sort('amount');
    expect(list.sortBy).toBe('amount');
    expect(list.sortDescending).toBe(false);

    list.sort('amount');
    expect(list.sortDescending).toBe(true);

    list.sort('nextrenewaldate');
    expect(list.sortBy).toBe('nextrenewaldate');
    expect(list.sortDescending).toBe(false);
  });

  it('updates pageNumber and reloads when going to a different page', () => {
    const getSubscriptions = vi.fn().mockReturnValue(of(emptyPage()));
    const list = createList(getSubscriptions);

    list.goToPage(2);

    expect(list.pageNumber).toBe(2);
    expect(getSubscriptions).toHaveBeenLastCalledWith(expect.objectContaining({ pageNumber: 2 }));
  });

  it('surfaces a generic error and stops loading when the request fails', () => {
    const getSubscriptions = vi.fn().mockReturnValue(throwError(() => new Error('network down')));
    const list = createList(getSubscriptions);

    expect(list.isLoading()).toBe(false);
    expect(list.errorMessage()).toBe('error.generic');
    expect(list.page()).toBeNull();
  });

  describe('initials', () => {
    it('builds initials from up to the first two words of a name', () => {
      const list = createList(vi.fn().mockReturnValue(of(emptyPage())));

      expect(list.initials('Netflix')).toBe('N');
      expect(list.initials('Adobe Creative Cloud')).toBe('AC');
      expect(list.initials('  spaced   out  ')).toBe('SO');
    });
  });
});
