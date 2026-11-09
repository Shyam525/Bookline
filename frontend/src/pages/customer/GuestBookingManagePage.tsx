import React, { useState, useEffect } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { bookingsApi } from '../../services/api/bookings';
import {
  Calendar,
  Clock,
  MapPin,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  RotateCcw,
  Download,
  Building2,
  User,
  ArrowRight,
  ShieldCheck,
  Share2,
  ExternalLink,
  ChevronLeft
} from 'lucide-react';
import {
  formatBusinessDate,
  formatBusinessTime,
  parseDateSafe,
  ScheduleErrorDefense,
} from '../../utils/timeFormatters';

export const GuestBookingManagePage: React.FC = () => {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token');

  const [booking, setBooking] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Reschedule state
  const [isRescheduling, setIsRescheduling] = useState(false);
  const [newDate, setNewDate] = useState('');
  const [newTime, setNewTime] = useState('11:00');
  const [rescheduleLoading, setRescheduleLoading] = useState(false);
  const [rescheduleError, setRescheduleError] = useState<string | null>(null);
  const [rescheduleSuccess, setRescheduleSuccess] = useState(false);

  // Cancel state
  const [isCancelling, setIsCancelling] = useState(false);
  const [cancelReason, setCancelReason] = useState('');
  const [cancelLoading, setCancelLoading] = useState(false);
  const [cancelError, setCancelError] = useState<string | null>(null);
  const [cancelSuccess, setCancelSuccess] = useState(false);

  useEffect(() => {
    if (!token) {
      setError('No booking management token was provided. Please use the secure link sent in your confirmation.');
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    bookingsApi
      .getGuestBooking(token)
      .then((data) => {
        setBooking(data);
        if (data.startUtc) {
          const d = new Date(data.startUtc);
          setNewDate(d.toISOString().split('T')[0]);
        }
      })
      .catch((err) => {
        setError(err.message || 'The booking link is invalid, tampered with, or expired.');
      })
      .finally(() => setLoading(false));
  }, [token]);

  const handleDownloadIcs = () => {
    if (!booking) return;

    const startFormatted = new Date(booking.startUtc).toISOString().replace(/-|:|\.\d\d\d/g, '');
    const endFormatted = new Date(booking.endUtc).toISOString().replace(/-|:|\.\d\d\d/g, '');

    const icsContent = `BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Bookline//Customer Booking//EN
BEGIN:VEVENT
UID:${booking.id}@bookline.app
DTSTAMP:${new Date().toISOString().replace(/-|:|\.\d\d\d/g, '')}
DTSTART:${startFormatted}
DTEND:${endFormatted}
SUMMARY:${booking.serviceName} - ${booking.providerName}
DESCRIPTION:Booking Reference: ${booking.bookingReference}\\nSpecialist: ${booking.staffName}\\nLocation: ${booking.locationName}
LOCATION:${booking.locationName}
STATUS:CONFIRMED
END:VEVENT
END:VCALENDAR`;

    const blob = new Blob([icsContent], { type: 'text/calendar;charset=utf-8' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `${booking.bookingReference || 'appointment'}.ics`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const handleCancel = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;

    setCancelLoading(true);
    setCancelError(null);

    try {
      await bookingsApi.cancelGuestBooking(token, cancelReason || 'Customer requested cancellation');
      setBooking((prev: any) => prev ? { ...prev, status: 'Cancelled' } : prev);
      setCancelSuccess(true);
      setIsCancelling(false);
    } catch (err: any) {
      setCancelError(err.message || 'Failed to cancel appointment.');
    } finally {
      setCancelLoading(false);
    }
  };

  const handleReschedule = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;

    setRescheduleLoading(true);
    setRescheduleError(null);

    try {
      const [hours, minutes] = newTime.split(':');
      const start = new Date(newDate);
      start.setHours(parseInt(hours, 10), parseInt(minutes, 10), 0, 0);

      const updated = await bookingsApi.rescheduleGuestBooking(token, start.toISOString());
      setBooking((prev: any) => prev ? {
        ...prev,
        startUtc: updated.startUtc,
        endUtc: updated.endUtc,
        status: updated.status || 'Confirmed'
      } : prev);
      setRescheduleSuccess(true);
      setIsRescheduling(false);
    } catch (err: any) {
      setRescheduleError(err.message || 'Failed to reschedule. Selected slot is unavailable.');
    } finally {
      setRescheduleLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-[#0E131F] flex items-center justify-center p-4">
        <div className="text-center space-y-4">
          <div className="w-12 h-12 border-4 border-[#E8546A] border-t-transparent rounded-full animate-spin mx-auto" />
          <p className="text-[#A9B4D0] text-sm">Verifying secure appointment access...</p>
        </div>
      </div>
    );
  }

  if (error || !booking) {
    return (
      <div className="min-h-screen bg-[#0E131F] flex items-center justify-center p-4">
        <div className="max-w-md w-full bg-[#141A29] border border-[#212A3E] rounded-2xl p-6 sm:p-8 text-center space-y-4 shadow-xl">
          <div className="w-16 h-16 bg-[#E8546A]/10 border border-[#E8546A]/20 rounded-full flex items-center justify-center mx-auto text-[#E8546A]">
            <AlertTriangle className="w-8 h-8" />
          </div>
          <h2 className="text-xl font-bold text-[#ECEFFE]">Access Restricted</h2>
          <p className="text-sm text-[#7E88A8]">
            {error || 'Unable to retrieve this appointment. The link may have expired or been modified.'}
          </p>
          <div className="pt-2">
            <Link
              to="/discover"
              className="inline-flex items-center gap-2 px-5 py-2.5 bg-[#E8546A] text-white rounded-xl text-sm font-semibold hover:bg-[#D44359] transition"
            >
              Browse Services
            </Link>
          </div>
        </div>
      </div>
    );
  }

  const isCancelled = booking.status === 'Cancelled';
  const isPast = new Date(booking.endUtc) < new Date();

  return (
    <div className="min-h-screen bg-[#0E131F] text-[#ECEFFE] py-10 px-4 sm:px-6 lg:px-8">
      <div className="max-w-2xl mx-auto space-y-6">
        {/* Navigation Breadcrumb */}
        <div className="flex items-center justify-between">
          <Link
            to={booking.providerSlug ? `/business/${booking.providerSlug}` : '/discover'}
            className="inline-flex items-center gap-1.5 text-xs text-[#7E88A8] hover:text-[#ECEFFE] transition"
          >
            <ChevronLeft className="w-4 h-4" />
            Back to {booking.providerName || 'Storefront'}
          </Link>
          <div className="inline-flex items-center gap-1 text-xs text-[#34D399] bg-[#34D399]/10 border border-[#34D399]/20 px-2.5 py-1 rounded-full">
            <ShieldCheck className="w-3.5 h-3.5" />
            Verified Customer Access
          </div>
        </div>

        {/* Appointment Card */}
        <div className="bg-[#141A29] border border-[#212A3E] rounded-2xl overflow-hidden shadow-2xl">
          {/* Header Status Bar */}
          <div className="px-6 py-4 bg-[#182033] border-b border-[#212A3E] flex items-center justify-between">
            <div>
              <span className="text-xs uppercase tracking-wider text-[#7E88A8]">Booking Reference</span>
              <p className="text-base font-mono font-bold text-[#ECEFFE]">{booking.bookingReference}</p>
            </div>
            <div>
              <span
                className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-semibold ${
                  isCancelled
                    ? 'bg-[#EF4444]/15 text-[#EF4444] border border-[#EF4444]/30'
                    : isPast
                    ? 'bg-[#7E88A8]/20 text-[#A9B4D0] border border-[#7E88A8]/30'
                    : 'bg-[#34D399]/15 text-[#34D399] border border-[#34D399]/30'
                }`}
              >
                {isCancelled ? <XCircle className="w-3.5 h-3.5" /> : <CheckCircle2 className="w-3.5 h-3.5" />}
                {booking.status}
              </span>
            </div>
          </div>

          <div className="p-6 sm:p-8 space-y-6">
            {/* Service & Provider */}
            <div>
              <h1 className="text-2xl font-serif font-bold text-[#ECEFFE]">{booking.serviceName}</h1>
              <p className="text-sm text-[#E8546A] font-medium mt-1">{booking.providerName}</p>
            </div>

            {/* Date & Time Highlights */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="bg-[#182033] border border-[#212A3E] rounded-xl p-4 flex items-center gap-3">
                <Calendar className="w-5 h-5 text-[#E8546A]" />
                <div>
                  <div className="text-xs text-[#7E88A8]">Scheduled Date</div>
                  <div className="text-sm font-semibold text-[#ECEFFE]">
                    {formatBusinessDate(booking.startUtc, 'full')}
                  </div>
                </div>
              </div>

              <div className="bg-[#182033] border border-[#212A3E] rounded-xl p-4 flex items-center gap-3">
                <Clock className="w-5 h-5 text-[#38BDF8]" />
                <div>
                  <div className="text-xs text-[#7E88A8]">Session Time</div>
                  <div className="text-sm font-semibold text-[#ECEFFE]">
                    {formatBusinessTime(booking.startUtc)} – {formatBusinessTime(booking.endUtc)}
                  </div>
                </div>
              </div>
            </div>

            {/* Details List */}
            <div className="space-y-3 pt-2 border-t border-[#212A3E]">
              <div className="flex items-center justify-between text-sm">
                <span className="text-[#7E88A8] flex items-center gap-2">
                  <User className="w-4 h-4 text-[#7E88A8]" />
                  Assigned Specialist
                </span>
                <span className="font-medium text-[#ECEFFE]">{booking.staffName || 'Specialist'}</span>
              </div>

              <div className="flex items-center justify-between text-sm">
                <span className="text-[#7E88A8] flex items-center gap-2">
                  <MapPin className="w-4 h-4 text-[#7E88A8]" />
                  Location
                </span>
                <span className="font-medium text-[#ECEFFE] text-right">{booking.locationName || 'Main Studio'}</span>
              </div>

              <div className="flex items-center justify-between text-sm">
                <span className="text-[#7E88A8]">Client Name</span>
                <span className="font-medium text-[#ECEFFE]">{booking.customerName}</span>
              </div>

              <div className="flex items-center justify-between text-sm">
                <span className="text-[#7E88A8]">Total Amount</span>
                <span className="font-semibold text-lg text-[#ECEFFE]">
                  {booking.currency === 'INR' ? '₹' : booking.currency || '₹'} {booking.totalPrice}
                </span>
              </div>
            </div>

            {/* Notice / Policy Banner */}
            <div className="bg-[#182033]/60 border border-[#212A3E] rounded-xl p-4 text-xs text-[#7E88A8] space-y-1">
              <span className="font-semibold text-[#A9B4D0]">Cancellation Policy:</span>
              <p>
                {booking.cancellationPolicy || `Cancellations permitted up to ${booking.minimumNoticeHours || 2} hours before the appointment.`}
              </p>
            </div>

            {/* Feedback Alerts */}
            {cancelSuccess && (
              <div className="p-4 bg-[#EF4444]/10 border border-[#EF4444]/30 rounded-xl text-xs text-[#EF4444]">
                Your appointment has been cancelled successfully.
              </div>
            )}
            {rescheduleSuccess && (
              <div className="p-4 bg-[#34D399]/10 border border-[#34D399]/30 rounded-xl text-xs text-[#34D399]">
                Your appointment has been rescheduled successfully!
              </div>
            )}

            {/* Actions */}
            {!isCancelled && !isPast && (
              <div className="flex flex-wrap items-center gap-3 pt-2">
                <button
                  type="button"
                  onClick={handleDownloadIcs}
                  className="flex-1 inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-[#182033] hover:bg-[#212A3E] border border-[#212A3E] rounded-xl text-xs font-semibold text-[#ECEFFE] transition"
                >
                  <Download className="w-4 h-4 text-[#38BDF8]" />
                  Add to Calendar (.ics)
                </button>

                {booking.canReschedule && (
                  <button
                    type="button"
                    onClick={() => {
                      setIsRescheduling(!isRescheduling);
                      setIsCancelling(false);
                    }}
                    className="flex-1 inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-[#182033] hover:bg-[#212A3E] border border-[#212A3E] rounded-xl text-xs font-semibold text-[#ECEFFE] transition"
                  >
                    <RotateCcw className="w-4 h-4 text-[#E8546A]" />
                    Reschedule
                  </button>
                )}

                {booking.canCancel && (
                  <button
                    type="button"
                    onClick={() => {
                      setIsCancelling(!isCancelling);
                      setIsRescheduling(false);
                    }}
                    className="inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-[#EF4444]/10 hover:bg-[#EF4444]/20 border border-[#EF4444]/30 rounded-xl text-xs font-semibold text-[#EF4444] transition"
                  >
                    Cancel
                  </button>
                )}
              </div>
            )}

            {/* Reschedule Form */}
            {isRescheduling && (
              <form onSubmit={handleReschedule} className="mt-4 p-5 bg-[#182033] border border-[#212A3E] rounded-xl space-y-4">
                <h3 className="text-sm font-bold text-[#ECEFFE]">Reschedule Appointment</h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <div>
                    <label className="block text-xs text-[#7E88A8] mb-1">New Date</label>
                    <input
                      type="date"
                      value={newDate}
                      onChange={(e) => setNewDate(e.target.value)}
                      className="w-full bg-[#141A29] border border-[#212A3E] rounded-lg px-3 py-2 text-sm text-[#ECEFFE] focus:outline-none focus:border-[#E8546A]"
                      required
                    />
                  </div>
                  <div>
                    <label className="block text-xs text-[#7E88A8] mb-1">New Time</label>
                    <input
                      type="time"
                      value={newTime}
                      onChange={(e) => setNewTime(e.target.value)}
                      className="w-full bg-[#141A29] border border-[#212A3E] rounded-lg px-3 py-2 text-sm text-[#ECEFFE] focus:outline-none focus:border-[#E8546A]"
                      required
                    />
                  </div>
                </div>

                {rescheduleError && (
                  <p className="text-xs text-[#EF4444]">{rescheduleError}</p>
                )}

                <div className="flex justify-end gap-2">
                  <button
                    type="button"
                    onClick={() => setIsRescheduling(false)}
                    className="px-3 py-1.5 text-xs text-[#7E88A8] hover:text-[#ECEFFE]"
                  >
                    Cancel
                  </button>
                  <button
                    type="submit"
                    disabled={rescheduleLoading}
                    className="px-4 py-1.5 bg-[#E8546A] text-white text-xs font-semibold rounded-lg hover:bg-[#D44359] transition disabled:opacity-50"
                  >
                    {rescheduleLoading ? 'Confirming...' : 'Save New Slot'}
                  </button>
                </div>
              </form>
            )}

            {/* Cancel Confirmation Form */}
            {isCancelling && (
              <form onSubmit={handleCancel} className="mt-4 p-5 bg-[#EF4444]/5 border border-[#EF4444]/20 rounded-xl space-y-3">
                <h3 className="text-sm font-bold text-[#EF4444]">Confirm Cancellation</h3>
                <p className="text-xs text-[#7E88A8]">
                  Are you sure you want to cancel this booking? This slot will immediately be released to other customers.
                </p>
                <div>
                  <label className="block text-xs text-[#7E88A8] mb-1">Reason (Optional)</label>
                  <input
                    type="text"
                    value={cancelReason}
                    onChange={(e) => setCancelReason(e.target.value)}
                    placeholder="e.g. Schedule conflict, feeling unwell"
                    className="w-full bg-[#141A29] border border-[#212A3E] rounded-lg px-3 py-2 text-sm text-[#ECEFFE] focus:outline-none focus:border-[#EF4444]"
                  />
                </div>

                {cancelError && (
                  <p className="text-xs text-[#EF4444]">{cancelError}</p>
                )}

                <div className="flex justify-end gap-2">
                  <button
                    type="button"
                    onClick={() => setIsCancelling(false)}
                    className="px-3 py-1.5 text-xs text-[#7E88A8] hover:text-[#ECEFFE]"
                  >
                    Keep Appointment
                  </button>
                  <button
                    type="submit"
                    disabled={cancelLoading}
                    className="px-4 py-1.5 bg-[#EF4444] text-white text-xs font-semibold rounded-lg hover:bg-[#DC2626] transition disabled:opacity-50"
                  >
                    {cancelLoading ? 'Cancelling...' : 'Confirm Cancellation'}
                  </button>
                </div>
              </form>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

