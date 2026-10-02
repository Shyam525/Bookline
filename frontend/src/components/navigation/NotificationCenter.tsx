import React, { useState, useRef, useEffect } from 'react';
import { Bell, Calendar, Lock, CheckCircle2, X } from 'lucide-react';
import { clsx } from 'clsx';

export const NotificationCenter: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const notifications = [
    {
      id: '1',
      title: 'New Appointment Booked',
      description: 'Priya Shah booked Haircut & Style for 10:00 AM today.',
      timestamp: '5m ago',
      type: 'booking',
      unread: true,
    },
    {
      id: '2',
      title: 'Slot Hold Active',
      description: 'Slot 10:30 AM reserved for guest intake hold (5m remaining).',
      timestamp: '12m ago',
      type: 'hold',
      unread: true,
    },
    {
      id: '3',
      title: 'Reminder Email Sent',
      description: '24-hour appointment reminder delivered to Rahul Mehta.',
      timestamp: '1h ago',
      type: 'system',
      unread: false,
    },
  ];

  const unreadCount = notifications.filter((n) => n.unread).length;

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <div ref={containerRef} className="relative">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="relative p-2 rounded-xl text-[#7E88A8] hover:text-white hover:bg-[#181D2C] transition-colors focus:outline-none"
        title="Notification Center"
      >
        <Bell className="w-5 h-5" />
        {unreadCount > 0 && (
          <span className="absolute top-1 right-1 w-2.5 h-2.5 rounded-full bg-[#E8546A] ring-2 ring-[#111520] animate-pulse" />
        )}
      </button>

      {isOpen && (
        <div className="absolute right-0 mt-2 w-80 sm:w-96 bg-[#111520] border border-[#212638] rounded-2xl shadow-2xl z-50 overflow-hidden animate-fade-in">
          <div className="flex items-center justify-between px-5 py-4 border-b border-[#212638]">
            <div className="flex items-center gap-2">
              <h4 className="font-heading font-bold text-white">Notifications</h4>
              <span className="text-xs px-2 py-0.5 rounded-full bg-[#E8546A]/10 text-[#E8546A] border border-[#E8546A]/20 font-mono">
                {unreadCount} unread
              </span>
            </div>
            <button onClick={() => setIsOpen(false)} className="text-[#7E88A8] hover:text-white">
              <X className="w-4 h-4" />
            </button>
          </div>

          <div className="divide-y divide-[#212638] max-h-80 overflow-y-auto">
            {notifications.map((n) => (
              <div
                key={n.id}
                className={clsx(
                  'p-4 flex gap-3 hover:bg-[#181D2C]/50 transition-colors cursor-pointer',
                  n.unread && 'bg-[#181D2C]/20'
                )}
              >
                <div className="shrink-0 mt-0.5">
                  {n.type === 'booking' && <Calendar className="w-4 h-4 text-[#34D399]" />}
                  {n.type === 'hold' && <Lock className="w-4 h-4 text-[#FBBF24]" />}
                  {n.type === 'system' && <CheckCircle2 className="w-4 h-4 text-[#E8546A]" />}
                </div>
                <div className="flex-1 space-y-1">
                  <div className="flex items-center justify-between">
                    <p className="text-xs font-semibold text-white">{n.title}</p>
                    <span className="text-[10px] text-[#7E88A8]">{n.timestamp}</span>
                  </div>
                  <p className="text-xs text-[#7E88A8] leading-relaxed">{n.description}</p>
                </div>
              </div>
            ))}
          </div>

          <div className="p-3 border-t border-[#212638] bg-[#0A0C13]/50 text-center">
            <button className="text-xs font-medium text-[#E8546A] hover:underline">
              Mark all as read
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
