import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import {
  Bell,
  CheckCircle2,
  Calendar,
  ShoppingBag,
  Clock,
  CheckCheck,
  Trash2,
  ArrowRight,
  Filter,
  Sparkles,
  AlertCircle,
  RefreshCw,
  LogIn,
} from 'lucide-react';
import { useAuth } from '../../app/providers/AuthProvider';
import { notificationsApi, CustomerNotificationItem } from '../../services/api/notifications';
import { Pagination } from '../../components/data-display/TableComponents';

export const CustomerNotificationsPage: React.FC = () => {
  const { token, isAuthenticated } = useAuth();
  const [filter, setFilter] = useState<'all' | 'unread' | 'booking' | 'order' | 'reminder'>('all');
  const [notifications, setNotifications] = useState<CustomerNotificationItem[]>([]);
  const [page, setPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(10);
  const [totalPages, setTotalPages] = useState<number>(1);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [unreadCount, setUnreadCount] = useState<number>(0);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const loadNotifications = async () => {
    if (!token) {
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setErrorMsg(null);
    try {
      const res = await notificationsApi.getMyNotifications(token, {
        page,
        pageSize,
        type: filter === 'all' || filter === 'unread' ? undefined : filter,
        unreadOnly: filter === 'unread' ? true : undefined,
      });

      const list = res.items || res.notifications || [];
      setNotifications(list);
      setUnreadCount(res.unreadCount ?? 0);
      setTotalCount(res.totalCount ?? list.length);
      setTotalPages(res.totalPages ?? Math.max(1, Math.ceil((res.totalCount ?? list.length) / pageSize)));
    } catch (err: any) {
      console.error('Failed to load notifications:', err);
      setErrorMsg(err.message || 'Unable to load notifications.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (token) {
      loadNotifications();
    } else {
      setIsLoading(false);
    }
  }, [token, page, pageSize, filter]);

  const markAllAsRead = async () => {
    if (!token) return;
    try {
      await notificationsApi.markAllRead(token);
      setNotifications((prev) => prev.map((item) => ({ ...item, read: true })));
    } catch (err) {
      console.error('Failed to mark all as read:', err);
    }
  };

  const markAsRead = async (id: string) => {
    if (!token) return;
    try {
      await notificationsApi.markAsRead(token, id, true);
      setNotifications((prev) =>
        prev.map((item) => (item.id === id ? { ...item, read: true } : item))
      );
    } catch (err) {
      console.error('Failed to mark notification as read:', err);
    }
  };


  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#273142] pb-6">
        <div>
          <div className="inline-flex items-center gap-2 px-2.5 py-1 rounded-[6px] bg-[#1A2130] border border-[#273142] text-[11px] font-medium text-[#E8546A] mb-2">
            <Bell className="w-3.5 h-3.5" />
            <span>Activity Feed</span>
          </div>
          <h1 className="font-heading text-3xl font-bold text-[#F4F6FA] tracking-tight">
            Notifications &amp; Alerts
          </h1>
          <p className="text-xs text-[#C3CAD6] mt-1">
            Real-time updates regarding your booking holds, confirmations, reminders, and retail shipments
          </p>
        </div>

        {/* Global Actions */}
        <div className="flex items-center gap-2.5">
          {unreadCount > 0 && (
            <button
              onClick={markAllAsRead}
              className="px-3 py-1.5 rounded-[8px] bg-[#1A2130] hover:bg-[#273142] border border-[#273142] text-xs font-semibold text-[#F4F6FA] transition-colors flex items-center gap-1.5"
            >
              <CheckCheck className="w-3.5 h-3.5 text-[#34D399]" />
              <span>Mark all read</span>
            </button>
          )}

          <button
            onClick={loadNotifications}
            disabled={isLoading}
            className="px-3 py-1.5 rounded-[8px] bg-[#1A2130] hover:bg-[#273142] border border-[#273142] text-xs font-semibold text-[#8F9AAF] hover:text-[#F4F6FA] transition-colors flex items-center gap-1.5"
            title="Refresh notifications"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin' : ''}`} />
            <span>Refresh</span>
          </button>
        </div>
      </div>

      {/* Filter Tabs */}
      <div className="flex items-center gap-2 overflow-x-auto pb-1">
        {[
          { key: 'all', label: `All (${totalCount || notifications.length})` },
          { key: 'unread', label: `Unread (${unreadCount})` },
          { key: 'booking', label: 'Bookings' },
          { key: 'order', label: 'Orders' },
          { key: 'reminder', label: 'Reminders' },
        ].map((tab) => (
          <button
            key={tab.key}
            onClick={() => {
              setFilter(tab.key as any);
              setPage(1); // Section 84: Reset page to 1 on filter changes
            }}
            className={`px-3.5 py-1.5 rounded-[8px] text-xs font-semibold whitespace-nowrap transition-colors ${
              filter === tab.key
                ? 'bg-[#E8546A] text-white shadow-sm'
                : 'bg-[#111620] hover:bg-[#1A2130] text-[#C3CAD6] border border-[#273142]'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Notification List */}
      <div className="space-y-3">
        {isLoading ? (
          <div className="bg-[#111620] border border-[#273142] rounded-[16px] p-12 text-center space-y-3">
            <RefreshCw className="w-6 h-6 text-[#E8546A] animate-spin mx-auto" />
            <p className="text-xs text-[#8F9AAF]">Syncing your notifications...</p>
          </div>
        ) : errorMsg ? (
          <div className="bg-[#111620] border border-red-500/20 rounded-[16px] p-8 text-center space-y-4">
            <div className="w-12 h-12 rounded-[12px] bg-red-500/10 border border-red-500/20 text-red-400 mx-auto flex items-center justify-center">
              <AlertCircle className="w-6 h-6" />
            </div>
            <div>
              <h3 className="font-heading font-bold text-base text-[#F4F6FA]">Unable to load notifications</h3>
              <p className="text-xs text-[#C3CAD6] max-w-sm mx-auto mt-1">{errorMsg}</p>
            </div>
            <button
              onClick={loadNotifications}
              className="inline-flex items-center gap-2 px-4 py-2 rounded-[8px] bg-[#E8546A] hover:bg-[#F06A7D] text-white text-xs font-bold transition-all shadow-md"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              <span>Try Again</span>
            </button>
          </div>
        ) : !isAuthenticated && notifications.length === 0 ? (
          <div className="bg-[#111620] border border-[#273142] rounded-[16px] p-12 text-center space-y-4">
            <div className="w-12 h-12 rounded-[12px] bg-[#1A2130] border border-[#273142] text-[#8F9AAF] mx-auto flex items-center justify-center">
              <LogIn className="w-6 h-6 text-[#E8546A]" />
            </div>
            <div>
              <h3 className="font-heading font-bold text-base text-[#F4F6FA]">Sign in to View Notifications</h3>
              <p className="text-xs text-[#C3CAD6] max-w-sm mx-auto mt-1">
                Log in to your Bookline account to track your appointment holds, order dispatches, and personal reminders.
              </p>
            </div>
            <Link
              to="/login"
              className="inline-flex items-center gap-2 px-4 py-2 rounded-[8px] bg-[#E8546A] hover:bg-[#F06A7D] text-white text-xs font-bold transition-all shadow-md"
            >
              <span>Sign In</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          </div>
        ) : notifications.length === 0 ? (
          <div className="bg-[#111620] border border-[#273142] rounded-[16px] p-12 text-center space-y-4">
            <div className="w-12 h-12 rounded-[12px] bg-[#1A2130] border border-[#273142] text-[#8F9AAF] mx-auto flex items-center justify-center">
              <Bell className="w-6 h-6" />
            </div>
            <div>
              <h3 className="font-heading font-bold text-base text-[#F4F6FA]">No Notifications Found</h3>
              <p className="text-xs text-[#C3CAD6] max-w-sm mx-auto mt-1">
                You're all caught up! New schedule updates, order fulfillment notes, and appointment reminders will appear here.
              </p>
            </div>
            <Link
              to="/discover"
              className="inline-flex items-center gap-2 px-4 py-2 rounded-[8px] bg-[#E8546A] hover:bg-[#F06A7D] text-white text-xs font-bold transition-all shadow-md"
            >
              <span>Explore Marketplace</span>
              <ArrowRight className="w-3.5 h-3.5" />
            </Link>
          </div>
        ) : (
          notifications.map((notif) => {
            const isBooking = notif.type === 'booking';
            const isOrder = notif.type === 'order';
            const isReminder = notif.type === 'reminder';

            return (
              <div
                key={notif.id}
                onClick={() => markAsRead(notif.id)}
                className={`bg-[#111620] border rounded-[12px] p-4 sm:p-5 transition-all group relative flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 ${
                  notif.read
                    ? 'border-[#273142] opacity-85 hover:opacity-100 hover:border-[#344054]'
                    : 'border-[#344054] bg-[#151B27] shadow-sm'
                }`}
              >
                <div className="flex items-start gap-3.5 min-w-0">
                  {/* Status Indicator Icon */}
                  <div
                    className={`w-10 h-10 rounded-[8px] flex items-center justify-center flex-shrink-0 ${
                      isBooking
                        ? 'bg-[#34D399]/15 text-[#34D399] border border-[#34D399]/30'
                        : isOrder
                        ? 'bg-[#E8546A]/15 text-[#E8546A] border border-[#E8546A]/30'
                        : isReminder
                        ? 'bg-[#FBBF24]/15 text-[#FBBF24] border border-[#FBBF24]/30'
                        : 'bg-[#60A5FA]/15 text-[#60A5FA] border border-[#60A5FA]/30'
                    }`}
                  >
                    {isBooking && <Calendar className="w-5 h-5" />}
                    {isOrder && <ShoppingBag className="w-5 h-5" />}
                    {isReminder && <Clock className="w-5 h-5" />}
                    {notif.type === 'system' && <Sparkles className="w-5 h-5" />}
                  </div>

                  <div className="space-y-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <h3 className="font-heading font-bold text-sm text-[#F4F6FA] group-hover:text-[#E8546A] transition-colors">
                        {notif.title}
                      </h3>
                      {!notif.read && (
                        <span className="w-2 h-2 rounded-full bg-[#E8546A] inline-block" />
                      )}
                    </div>

                    <p className="text-xs text-[#C3CAD6] leading-relaxed">
                      {notif.message}
                    </p>

                    <div className="flex items-center gap-2 text-[11px] text-[#8F9AAF] pt-1">
                      <Clock className="w-3 h-3" />
                      <span>{notif.time || 'Recently'}</span>
                    </div>
                  </div>
                </div>

                {/* Direct Action Link */}
                <div className="flex items-center gap-3 w-full sm:w-auto justify-end flex-shrink-0 pt-2 sm:pt-0 border-t sm:border-t-0 border-[#273142]">
                  <Link
                    to={notif.link || '/appointments'}
                    className="px-3.5 py-1.5 rounded-[8px] bg-[#1A2130] hover:bg-[#E8546A] text-[#F4F6FA] hover:text-white text-xs font-semibold transition-colors flex items-center gap-1.5"
                  >
                    <span>View Details</span>
                    <ArrowRight className="w-3.5 h-3.5" />
                  </Link>
                </div>
              </div>
            );
          })
        )}

        {notifications.length > 0 && totalPages > 1 && (
          <div className="pt-4 border-t border-[#273142]">
            <Pagination
              currentPage={page}
              totalPages={totalPages}
              totalItems={totalCount}
              pageSize={pageSize}
              onPageChange={setPage}
              onPageSizeChange={(newSize) => {
                setPageSize(newSize);
                setPage(1);
              }}
            />
          </div>
        )}
      </div>
    </div>
  );
};
