import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Bell, CheckCircle2, Calendar, ShoppingBag, X } from 'lucide-react';

export const NotificationDropdown: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const [notifications, setNotifications] = useState([
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

  const unreadCount = notifications.filter((n) => !n.read).length;

  const markAllRead = () => {
    setNotifications((prev) => prev.map((n) => ({ ...n, read: true })));
  };

  return (
    <div className="relative">
      <button
        onClick={() => setIsOpen((prev) => !prev)}
        className="relative p-2.5 rounded-xl bg-[#111520] hover:bg-[#181D2C] border border-[#212638] text-[#ECEFFE] transition-colors"
        aria-label="Notifications"
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

            <div className="max-h-80 overflow-y-auto divide-y divide-[#212638]/50">
              {notifications.map((n) => (
                <Link
                  key={n.id}
                  to={n.link}
                  onClick={() => setIsOpen(false)}
                  className={`p-4 flex gap-3 hover:bg-[#181D2C] transition-colors block ${
                    !n.read ? 'bg-[#181D2C]/40' : ''
                  }`}
                >
                  <div
                    className={`w-8 h-8 rounded-lg flex items-center justify-center flex-shrink-0 ${
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
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-1 mb-0.5">
                      <p className="text-xs font-semibold text-white truncate">{n.title}</p>
                      <span className="text-[10px] text-[#7E88A8] whitespace-nowrap">{n.time}</span>
                    </div>
                    <p className="text-[11px] text-[#7E88A8] line-clamp-2 leading-relaxed">{n.message}</p>
                  </div>
                </Link>
              ))}
            </div>

            <div className="p-2.5 border-t border-[#212638] bg-[#0E121B] text-center">
              <Link
                to="/notifications"
                onClick={() => setIsOpen(false)}
                className="text-xs font-semibold text-[#E8546A] hover:underline"
              >
                View all notifications
              </Link>
            </div>
          </div>
        </>
      )}
    </div>
  );
};
