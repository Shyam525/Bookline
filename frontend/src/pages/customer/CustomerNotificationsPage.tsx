import React, { useState } from 'react';
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
} from 'lucide-react';

interface NotificationItem {
  id: string;
  title: string;
  message: string;
  timestamp: string;
  read: boolean;
  type: 'booking' | 'order' | 'reminder' | 'system';
  link: string;
  referenceId?: string;
}

export const CustomerNotificationsPage: React.FC = () => {
  const [filter, setFilter] = useState<'all' | 'unread' | 'booking' | 'order' | 'reminder'>('all');

  const [notifications, setNotifications] = useState<NotificationItem[]>([
    {
      id: 'notif-1',
      title: 'Appointment Confirmed & Guaranteed',
      message: 'Your Signature Aromatherapy session with Elena Vance at Aura Wellness & Spa is confirmed for Saturday, 10:00 AM.',
      timestamp: '1 hour ago',
      read: false,
      type: 'booking',
      link: '/appointments/apt-seed-1',
      referenceId: 'BL-849201',
    },
    {
      id: 'notif-2',
      title: 'Retail Order Dispatch Scheduled',
      message: 'Retail Order ORD-948210 (Botanical Keratin Restorative Hair Mask) has been prepared and packed for courier dispatch.',
      timestamp: '5 hours ago',
      read: false,
      type: 'order',
      link: '/orders/ord-seed-1',
      referenceId: 'ORD-948210',
    },
    {
      id: 'notif-3',
      title: '24-Hour Upcoming Service Reminder',
      message: 'Reminder: You have an upcoming Balayage & Conditioning appointment tomorrow at Glow Hair Lounge with Marcus Brody.',
      timestamp: '1 day ago',
      read: true,
      type: 'reminder',
      link: '/appointments/apt-seed-2',
      referenceId: 'BL-938102',
    },
    {
      id: 'notif-4',
      title: 'Post-Care Review Invitation',
      message: 'How was your Deep Hydration Facial experience? Leave an honest verified review to assist future clients on Bookline.',
      timestamp: '3 days ago',
      read: true,
      type: 'booking',
      link: '/business/aura-wellness-ahmedabad',
      referenceId: 'BL-721094',
    },
    {
      id: 'notif-5',
      title: 'Welcome to Bookline Marketplace',
      message: 'Your profile has been created with customer access. Discover verified specialists, view live calendar openings, and shop retail essentials.',
      timestamp: '5 days ago',
      read: true,
      type: 'system',
      link: '/discover',
    },
  ]);

  const markAllAsRead = () => {
    setNotifications((prev) => prev.map((item) => ({ ...item, read: true })));
  };

  const markAsRead = (id: string) => {
    setNotifications((prev) =>
      prev.map((item) => (item.id === id ? { ...item, read: true } : item))
    );
  };

  const clearAllNotifications = () => {
    setNotifications([]);
  };

  const filteredNotifications = notifications.filter((item) => {
    if (filter === 'unread') return !item.read;
    if (filter === 'booking') return item.type === 'booking';
    if (filter === 'order') return item.type === 'order';
    if (filter === 'reminder') return item.type === 'reminder';
    return true;
  });

  const unreadCount = notifications.filter((n) => !n.read).length;

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

          {notifications.length > 0 && (
            <button
              onClick={clearAllNotifications}
              className="px-3 py-1.5 rounded-[8px] bg-[#1A2130] hover:bg-[#273142] border border-[#273142] text-xs font-semibold text-[#8F9AAF] hover:text-[#F87171] transition-colors flex items-center gap-1.5"
            >
              <Trash2 className="w-3.5 h-3.5" />
              <span>Clear</span>
            </button>
          )}
        </div>
      </div>

      {/* Filter Tabs */}
      <div className="flex items-center gap-2 overflow-x-auto pb-1">
        {[
          { key: 'all', label: `All (${notifications.length})` },
          { key: 'unread', label: `Unread (${unreadCount})` },
          { key: 'booking', label: 'Bookings' },
          { key: 'order', label: 'Orders' },
          { key: 'reminder', label: 'Reminders' },
        ].map((tab) => (
          <button
            key={tab.key}
            onClick={() => setFilter(tab.key as any)}
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
        {filteredNotifications.length === 0 ? (
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
          filteredNotifications.map((notif) => {
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
                      {notif.referenceId && (
                        <span className="px-2 py-0.5 rounded-[4px] bg-[#1A2130] border border-[#273142] text-[10px] font-mono font-medium text-[#C3CAD6]">
                          {notif.referenceId}
                        </span>
                      )}
                    </div>

                    <p className="text-xs text-[#C3CAD6] leading-relaxed">
                      {notif.message}
                    </p>

                    <div className="flex items-center gap-2 text-[11px] text-[#8F9AAF] pt-1">
                      <Clock className="w-3 h-3" />
                      <span>{notif.timestamp}</span>
                    </div>
                  </div>
                </div>

                {/* Direct Action Link */}
                <div className="flex items-center gap-3 w-full sm:w-auto justify-end flex-shrink-0 pt-2 sm:pt-0 border-t sm:border-t-0 border-[#273142]">
                  <Link
                    to={notif.link}
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
      </div>
    </div>
  );
};
