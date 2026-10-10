import React, { useState } from 'react';
import {
  ChevronLeft,
  ChevronRight,
  ArrowUpDown,
  ArrowUp,
  ArrowDown,
  TrendingUp,
  TrendingDown,
  Minus,
  Inbox,
} from 'lucide-react';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';
import { Skeleton } from './DataDisplay';

/* =========================================================================
 * 1. SEMANTIC TABLE PRIMITIVES (High density Provider/Admin styling)
 * ========================================================================= */
export const Table: React.FC<{ children: React.ReactNode; className?: string }> = ({
  children,
  className = '',
}) => {
  return (
    <div className="w-full overflow-x-auto rounded-2xl border border-[#212638] bg-[#111520]">
      <table className={twMerge('w-full text-left text-xs border-collapse', className)}>
        {children}
      </table>
    </div>
  );
};

export const TableHeader: React.FC<{ children: React.ReactNode; className?: string }> = ({
  children,
  className = '',
}) => {
  return (
    <thead className={twMerge('bg-[#151B27] border-b border-[#212638] text-[#7E88A8] uppercase font-semibold text-[10px] tracking-wider select-none', className)}>
      {children}
    </thead>
  );
};

export const TableBody: React.FC<{ children: React.ReactNode; className?: string }> = ({
  children,
  className = '',
}) => {
  return <tbody className={twMerge('divide-y divide-[#212638] text-[#ECEFFE]', className)}>{children}</tbody>;
};

export const TableRow: React.FC<{
  children: React.ReactNode;
  className?: string;
  onClick?: () => void;
  selected?: boolean;
}> = ({ children, className = '', onClick, selected = false }) => {
  return (
    <tr
      onClick={onClick}
      className={twMerge(
        clsx(
          'transition-colors',
          onClick && 'cursor-pointer hover:bg-[#181D2C]',
          selected && 'bg-[#181D2C] ring-1 ring-inset ring-[#E8546A]/50',
          className
        )
      )}
    >
      {children}
    </tr>
  );
};

export const TableHeadCell: React.FC<{
  children: React.ReactNode;
  className?: string;
  sortable?: boolean;
  sorted?: 'asc' | 'desc' | null;
  onSort?: () => void;
}> = ({ children, className = '', sortable = false, sorted = null, onSort }) => {
  return (
    <th
      scope="col"
      onClick={sortable ? onSort : undefined}
      className={twMerge(
        clsx(
          'px-4 py-3 font-semibold',
          sortable && 'cursor-pointer hover:text-white transition-colors',
          className
        )
      )}
    >
      <div className="flex items-center gap-1.5">
        <span>{children}</span>
        {sortable && (
          <span className="text-[#7E88A8]">
            {sorted === 'asc' ? (
              <ArrowUp className="w-3 h-3 text-[#E8546A]" />
            ) : sorted === 'desc' ? (
              <ArrowDown className="w-3 h-3 text-[#E8546A]" />
            ) : (
              <ArrowUpDown className="w-3 h-3 opacity-40 hover:opacity-100" />
            )}
          </span>
        )}
      </div>
    </th>
  );
};

export const TableCell: React.FC<{ children: React.ReactNode; className?: string }> = ({
  children,
  className = '',
}) => {
  return <td className={twMerge('px-4 py-3.5 text-xs text-[#ECEFFE]', className)}>{children}</td>;
};

/* =========================================================================
 * 2. PAGINATION COMPONENT
 * ========================================================================= */
export interface PaginationProps {
  currentPage: number;
  totalPages: number;
  totalItems?: number;
  pageSize?: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
  className?: string;
}

export const Pagination: React.FC<PaginationProps> = ({
  currentPage,
  totalPages,
  totalItems,
  pageSize,
  onPageChange,
  onPageSizeChange,
  className = '',
}) => {
  if (totalPages <= 1 && !totalItems) return null;

  return (
    <div className={twMerge('flex flex-col sm:flex-row items-center justify-between gap-3 pt-4 text-xs text-[#7E88A8]', className)}>
      <div className="flex items-center gap-2">
        {totalItems !== undefined && (
          <span>
            Total <strong className="text-[#ECEFFE]">{totalItems}</strong> entries
          </span>
        )}
        {pageSize && onPageSizeChange && (
          <div className="flex items-center gap-1.5 ml-2">
            <span>Show:</span>
            <select
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="bg-[#181D2C] border border-[#212638] rounded-lg px-2 py-1 text-xs text-[#ECEFFE] focus:outline-none"
            >
              <option value={10}>10</option>
              <option value={25}>25</option>
              <option value={50}>50</option>
              <option value={100}>100</option>
            </select>
          </div>
        )}
      </div>

      <div className="flex items-center gap-1.5">
        <button
          type="button"
          onClick={() => onPageChange(Math.max(1, currentPage - 1))}
          disabled={currentPage <= 1}
          className="flex items-center gap-1 px-3 py-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-[#ECEFFE] disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          <ChevronLeft className="w-3.5 h-3.5" /> Prev
        </button>

        <span className="px-2 text-xs font-medium">
          Page <strong className="text-white">{currentPage}</strong> of{' '}
          <strong className="text-white">{totalPages || 1}</strong>
        </span>

        <button
          type="button"
          onClick={() => onPageChange(Math.min(totalPages, currentPage + 1))}
          disabled={currentPage >= totalPages}
          className="flex items-center gap-1 px-3 py-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-[#ECEFFE] disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >
          Next <ChevronRight className="w-3.5 h-3.5" />
        </button>
      </div>
    </div>
  );
};

/* =========================================================================
 * 3. DATATABLE COMPONENT
 * ========================================================================= */
export interface ColumnDef<T> {
  key: string;
  header: string;
  render?: (item: T) => React.ReactNode;
  sortable?: boolean;
  className?: string;
}

export interface DataTableProps<T> {
  columns: ColumnDef<T>[];
  data: T[];
  keyField?: keyof T;
  isLoading?: boolean;
  emptyMessage?: string;
  onRowClick?: (item: T) => void;
  selectedId?: string | number | null;
  className?: string;
}

export function DataTable<T extends Record<string, any>>({
  columns,
  data,
  keyField = 'id' as keyof T,
  isLoading = false,
  emptyMessage = 'No records found',
  onRowClick,
  selectedId,
  className = '',
}: DataTableProps<T>) {
  const [sortKey, setSortKey] = useState<string | null>(null);
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');

  const handleSort = (key: string) => {
    if (sortKey === key) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
    } else {
      setSortKey(key);
      setSortOrder('asc');
    }
  };

  const sortedData = React.useMemo(() => {
    if (!sortKey) return data;
    return [...data].sort((a, b) => {
      const aVal = a[sortKey];
      const bVal = b[sortKey];
      if (aVal === bVal) return 0;
      if (aVal == null) return 1;
      if (bVal == null) return -1;
      const res = aVal > bVal ? 1 : -1;
      return sortOrder === 'asc' ? res : -res;
    });
  }, [data, sortKey, sortOrder]);

  return (
    <Table className={className}>
      <TableHeader>
        <tr>
          {columns.map((col) => (
            <TableHeadCell
              key={col.key}
              sortable={col.sortable}
              sorted={sortKey === col.key ? sortOrder : null}
              onSort={() => handleSort(col.key)}
              className={col.className}
            >
              {col.header}
            </TableHeadCell>
          ))}
        </tr>
      </TableHeader>
      <TableBody>
        {isLoading ? (
          // Skeletons when loading
          [...Array(5)].map((_, i) => (
            <tr key={`loading-${i}`} className="border-b border-[#212638]">
              {columns.map((col) => (
                <td key={col.key} className="px-4 py-3.5">
                  <Skeleton className="h-4 w-3/4" />
                </td>
              ))}
            </tr>
          ))
        ) : sortedData.length === 0 ? (
          <tr>
            <td colSpan={columns.length} className="px-4 py-12 text-center text-[#7E88A8]">
              <div className="flex flex-col items-center justify-center space-y-2">
                <Inbox className="w-6 h-6 text-[#7E88A8]/60" />
                <span>{emptyMessage}</span>
              </div>
            </td>
          </tr>
        ) : (
          sortedData.map((item, idx) => {
            const isSelected = selectedId != null && item[keyField] === selectedId;
            return (
              <TableRow
                key={item[keyField] != null ? String(item[keyField]) : idx}
                onClick={onRowClick ? () => onRowClick(item) : undefined}
                selected={isSelected}
              >
                {columns.map((col) => (
                  <TableCell key={col.key} className={col.className}>
                    {col.render ? col.render(item) : item[col.key]}
                  </TableCell>
                ))}
              </TableRow>
            );
          })
        )}
      </TableBody>
    </Table>
  );
}

/* =========================================================================
 * 4. STAT COMPONENT (Compact Tile & Inline Stat)
 * ========================================================================= */
export interface StatProps {
  label: string;
  value: string | number;
  change?: string | number;
  trend?: 'up' | 'down' | 'neutral';
  icon?: React.ReactNode;
  caption?: string;
  className?: string;
}

export const Stat: React.FC<StatProps> = ({
  label,
  value,
  change,
  trend = 'neutral',
  icon,
  caption,
  className = '',
}) => {
  return (
    <div className={twMerge('p-4 rounded-xl bg-[#111520] border border-[#212638] flex items-center justify-between gap-4', className)}>
      <div className="space-y-1">
        <p className="text-[11px] font-semibold text-[#7E88A8] uppercase tracking-wider">{label}</p>
        <p className="font-heading text-2xl font-bold text-white">{value}</p>
        {(change || caption) && (
          <div className="flex items-center gap-1.5 text-xs">
            {change && (
              <span
                className={`font-semibold flex items-center gap-0.5 ${
                  trend === 'up'
                    ? 'text-[#34D399]'
                    : trend === 'down'
                    ? 'text-red-400'
                    : 'text-[#7E88A8]'
                }`}
              >
                {trend === 'up' && <TrendingUp className="w-3 h-3" />}
                {trend === 'down' && <TrendingDown className="w-3 h-3" />}
                {trend === 'neutral' && <Minus className="w-3 h-3" />}
                {change}
              </span>
            )}
            {caption && <span className="text-[#7E88A8]">{caption}</span>}
          </div>
        )}
      </div>
      {icon && (
        <div className="p-3 rounded-xl bg-[#181D2C] border border-[#212638] text-[#E8546A]">
          {icon}
        </div>
      )}
    </div>
  );
};
