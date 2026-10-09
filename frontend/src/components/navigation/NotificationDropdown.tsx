import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Bell, CheckCircle2, Calendar, ShoppingBag, X, Check, Eye, EyeOff } from 'lucide-react';
import { useAuth } from '../../app/providers/AuthProvider';

export interface NotificationItem {
  id: string;
  title: string;
  message: string;
  time: string;
  read: boolean;
  link: string;
  type: string;
}

export const NotificationDropdown: React.FC = () => {
  const { token } = useAuth();
  const [isOpen, setIsOpen] = useState(false);
  const [notifications, setNotifications] = useState<NotificationItem[]>([
    {
      id: '1',
      title: 'Appointment Confirmed',
      message: 'Your Signature Aromatherapy session with Aura Wellness is confirmed for Saturday at 10:00 AM.',
      time: '1h ago',
      read: false,
      link: '/appointments',
      type: 'booking',
    },
    {
      id: '2',
      title: 'Order Processing',
      message: 'Retail order ORD-839210 is being prepared for dispatch.',
      time: '1d ago',
      read: false,
      link: '/orders',
      type: 'order',
    },
    {
      id: '3',
      title: '24-Hour Reminder',
      message: 'Reminder: Upcoming appointment tomorrow with Dr. Priya Sharma.',
      time: '2d ago',
      read: true,
      link: '/appointments',
      type: 'reminder',
    },
  ]);

  useEffect(() => {
    if (!token) return;
    fetch('/api/v1/notifications/my', {
      headers: { Authorization: `Bearer ${token}` },
    })
      .then((res) => (res.ok ? res.json() : null))
      .then((data) => {
        if (data?.notifications && data.notifications.length > 0) {
          setNotifications(data.notifications);
        }
      })
      .catch(() => {});
  }, [token]);

  const unreadCount = notifications.filter((n) => !n.read).length;

  const markAllRead = async () => {
    setNotifications((prev) => prev.map((n) => ({ ...n, read: true })));
    if (token) {
      try {
        await fetch('/api/v1/notifications/read-all', {
          method: 'PUT',
          headers: { Authorization: `Bearer ${token}` },
        });
      } catch {}
    }
  };

  const toggleRead = async (e: React.MouseEvent, id: string) => {
    e.preventDefault();
    e.stopPropagation();

    const target = notifications.find((n) => n.id === id);
    if (!target) return;
    const nextRead = !target.read;

    setNotifications((prev) =>
      prev.map((n) => (n.id === id ? { ...n, read: nextRead } : n))
    );

    if (token) {
      try {
        await fetch(`/api/v1/notifications/${id}/read`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({ read: nextRead }),
        });
      } catch {}
    }
  };

  return (
    <div className="relative">
      <button
        onClick={() => setIsOpen((prev) => !prev)}
        className="relative p-2.5 rounded-xl bg-[#111520] hover:bg-[#181D2C] border border-[#212638] text-[#ECEFFE] transition-colors"
        aria-label="Notifications"
        id="customer-notifications-bell"
      >
        <Bell className="w-4 h-4" />
        {unreadCount > 0 && (
          <span className="absolute -top-1 -right-1 w-4 h-4 rounded-full bg-[#E8546A] text-white text-[10px] font-bold flex items-center justify-center animate-pulse">
            {unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <>
          <div className="fixed inset-0 z-30" onClick={() => setIsOpen(false)} />
          <div className="absolute right-0 mt-2 w-80 sm:w-96 bg-[#111520] border border-[#212638] rounded-2xl shadow-2xl z-40 overflow-hidden animate-fadeIn">
            {/* Header */}
            <div className="p-4 border-b border-[#212638] flex items-center justify-between bg-[#0E121B]">
              <div className="flex items-center gap-2">
                <h3 className="text-sm font-bold text-white">Notifications</h3>
                {unreadCount > 0 && (
                  <span className="px-1.5 py-0.5 rounded-full text-[10px] font-bold bg-[#E8546A]/20 text-[#E8546A]">
                    {unreadCount} new
                  </span>
                )}
              </div>
              <div className="flex items-center gap-2">
                {unreadCount > 0 && (
                  <button
                    onClick={markAllRead}
                    className="text-[11px] text-[#7E88A8] hover:text-white transition-colors"
                  >
                    Mark all read
                  </button>
                )}
                <button
                  onClick={() => setIsOpen(false)}
                  className="text-[#7E88A8] hover:text-white p-1"
                >
                  <X className="w-3.5 h-3.5" />
                </button>
              </div>
            </div>

            {/* Notification List (Section 93) */}
            <div className="max-h-80 overflow-y-auto divide-y divide-[#212638]/50">
              {notifications.map((n) => (
                <div
                  key={n.id}
                  className={`p-3.5 flex items-start gap-3 hover:bg-[#181D2C] transition-colors ${
                    !n.read ? 'bg-[#181D2C]/40 border-l-2 border-l-[#E8546A]' : ''
                  }`}
                >
                  <Link
                    to={n.link}
                    onClick={() => setIsOpen(false)}
                    className="flex-shrink-0 mt-0.5"
                  >
                    <div
                      className={`w-8 h-8 rounded-lg flex items-center justify-center ${
                        n.type === 'booking'
                          ? 'bg-[#34D399]/10 text-[#34D399]'
                          : n.type === 'order'
                          ? 'bg-[#E8546A]/10 text-[#E8546A]'
                          : 'bg-[#FBBF24]/10 text-[#FBBF24]'
                      }`}
                    >
                      {n.type === 'booking' && <Calendar className="w-4 h-4" />}
                      {n.type === 'order' && <ShoppingBag className="w-4 h-4" />}
                      {n.type === 'reminder' && <CheckCircle2 className="w-4 h-4" />}
                    </div>
                  </Link>

                  <Link
                    to={n.link}
                    onClick={() => setIsOpen(false)}
                    className="flex-1 min-w-0"
                  >
                    <div className="flex items-center justify-between gap-1 mb-0.5">
                      <p className="text-xs font-semibold text-white truncate">{n.title}</p>
                      <span className="text-[10px] text-[#7E88A8] whitespace-nowrap">{n.time}</span>
                    </div>
                    <p className="text-[11px] text-[#7E88A8] line-clamp-2 leading-relaxed">{n.message}</p>
                  </Link>

                  {/* Read / Unread toggle control (Section 93) */}
                  <button
                    onClick={(e) => toggleRead(e, n.id)}
                    title={n.read ? 'Mark as unread' : 'Mark as read'}
                    className={`flex-shrink-0 p-1.5 rounded-lg border transition-colors mt-0.5 ${
                      n.read
                        ? 'border-transparent text-[#7E88A8] hover:text-white hover:bg-[#212638]'
                        : 'border-[#E8546A]/30 text-[#E8546A] hover:bg-[#E8546A]/10'
                    }`}
                  >
                    {n.read ? <EyeOff className="w-3.5 h-3.5" /> : <Check className="w-3.5 h-3.5" />}
                  </button>
                </div>
              ))}
            </div>

            {/* Deep link footer */}
            <div className="p-2.5 border-t border-[#212638] bg-[#0E121B] text-center">
              <Link
                to="/dashboard"
                onClick={() => setIsOpen(false)}
                className="text-xs font-semibold text-[#E8546A] hover:underline"
              >
                Open Notification Center
              </Link>
            </div>
          </div>
        </>
      )}
    </div>
  );
};
