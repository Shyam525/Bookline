import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Bell,
  Mail,
  MessageSquare,
  Send,
  CheckCircle2,
  AlertCircle,
  Clock,
  Settings,
  Filter,
  RefreshCw,
  Sliders,
  ShieldCheck,
  X
} from 'lucide-react';
import {
  notificationsApi,
  NotificationSettingItem,
  NotificationLogItem
} from '../../services/api/notifications';

export const NotificationsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<'logs' | 'settings'>('settings');

  // Logs state
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [channelFilter, setChannelFilter] = useState<string>('');

  // Test Modal State
  const [isTestModalOpen, setIsTestModalOpen] = useState(false);
  const [testEmail, setTestEmail] = useState('');
  const [testPhone, setTestPhone] = useState('');
  const [testChannel, setTestChannel] = useState<'Email' | 'SMS'>('Email');
  const [testMessage, setTestMessage] = useState<string | null>(null);

  // Queries
  const { data: settings } = useQuery({
    queryKey: ['notificationSettings'],
    queryFn: () => notificationsApi.getSettings('mock-token')
  });

  const { data: logsData, isLoading: isLogsLoading, refetch: refetchLogs } = useQuery({
    queryKey: ['notificationLogs', page, statusFilter, channelFilter],
    queryFn: () => notificationsApi.getLogs('mock-token', { pageNumber: page, pageSize: 10, status: statusFilter, channel: channelFilter }),
    enabled: activeTab === 'logs'
  });

  // Settings form local state initialized from query or defaults
  const [formState, setFormState] = useState<Partial<NotificationSettingItem>>({
    emailNotificationsEnabled: true,
    smsNotificationsEnabled: false,
    reminder24hEnabled: true,
    reminder1hEnabled: true,
    senderEmail: 'no-reply@bookline.io',
    senderName: 'Bookline Appointments'
  });
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);

  React.useEffect(() => {
    if (settings) {
      setFormState(settings);
    }
  }, [settings]);

  const updateSettingsMutation = useMutation({
    mutationFn: (payload: {
      emailNotificationsEnabled: boolean;
      smsNotificationsEnabled: boolean;
      reminder24hEnabled: boolean;
      reminder1hEnabled: boolean;
      senderEmail: string;
      senderName: string;
    }) => notificationsApi.updateSettings('mock-token', payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationSettings'] });
      setHasUnsavedChanges(false);
    }
  });

  const sendTestMutation = useMutation({
    mutationFn: (payload: { recipientEmail: string; recipientPhone?: string; channel: string }) =>
      notificationsApi.sendTestNotification('mock-token', payload),
    onSuccess: (res) => {
      setTestMessage(res.message || 'Test notification dispatched successfully!');
      queryClient.invalidateQueries({ queryKey: ['notificationLogs'] });
      setTimeout(() => {
        setIsTestModalOpen(false);
        setTestMessage(null);
      }, 2000);
    }
  });

  const handleToggle = (field: keyof NotificationSettingItem) => {
    setFormState((prev) => ({ ...prev, [field]: !prev[field] }));
    setHasUnsavedChanges(true);
  };

  const handleInputChange = (field: keyof NotificationSettingItem, value: string) => {
    setFormState((prev) => ({ ...prev, [field]: value }));
    setHasUnsavedChanges(true);
  };

  const handleSaveSettings = (e: React.FormEvent) => {
    e.preventDefault();
    if (!formState.senderEmail) return;
    updateSettingsMutation.mutate({
      emailNotificationsEnabled: !!formState.emailNotificationsEnabled,
      smsNotificationsEnabled: !!formState.smsNotificationsEnabled,
      reminder24hEnabled: !!formState.reminder24hEnabled,
      reminder1hEnabled: !!formState.reminder1hEnabled,
      senderEmail: formState.senderEmail,
      senderName: formState.senderName || 'Bookline Appointments'
    });
  };

  const handleSendTestSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!testEmail) return;
    sendTestMutation.mutate({
      recipientEmail: testEmail,
      recipientPhone: testPhone || undefined,
      channel: testChannel
    });
  };

  return (
    <div className="space-y-6 text-slate-100">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-800 pb-5">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-3">
            <Bell className="w-7 h-7 text-[#E8546A]" />
            Notifications & Reminders Engine
          </h1>
          <p className="text-sm text-slate-400 mt-1">
            Manage automated customer appointment reminders, email templates, and delivery logs.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={() => setIsTestModalOpen(true)}
            className="flex items-center gap-2 px-4 py-2 bg-slate-800 hover:bg-slate-700 text-slate-200 font-medium rounded-lg text-sm transition border border-slate-700 shadow-sm"
          >
            <Send className="w-4 h-4 text-[#E8546A]" />
            Send Test Notification
          </button>
        </div>
      </div>

      {/* Tabs Switcher */}
      <div className="flex border-b border-slate-800">
        <button
          onClick={() => setActiveTab('settings')}
          className={`flex items-center gap-2 px-6 py-3 font-medium text-sm transition border-b-2 ${
            activeTab === 'settings'
              ? 'border-[#E8546A] text-[#E8546A]'
              : 'border-transparent text-slate-400 hover:text-slate-200'
          }`}
        >
          <Settings className="w-4 h-4" />
          Notification Settings
        </button>

        <button
          onClick={() => setActiveTab('logs')}
          className={`flex items-center gap-2 px-6 py-3 font-medium text-sm transition border-b-2 ${
            activeTab === 'logs'
              ? 'border-[#E8546A] text-[#E8546A]'
              : 'border-transparent text-slate-400 hover:text-slate-200'
          }`}
        >
          <Clock className="w-4 h-4" />
          Delivery History Logs
        </button>
      </div>

      {/* SETTINGS TAB */}
      {activeTab === 'settings' && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-6">
            <form onSubmit={handleSaveSettings} className="bg-[#111520] border border-slate-800 rounded-xl p-6 space-y-6">
              <h2 className="text-lg font-semibold text-white flex items-center gap-2">
                <Sliders className="w-5 h-5 text-[#E8546A]" />
                Automated Channels & Schedule
              </h2>

              {/* Toggles List */}
              <div className="space-y-4">
                <div className="flex items-center justify-between p-4 bg-[#181D2C] rounded-lg border border-slate-800/80">
                  <div className="flex items-center gap-3">
                    <div className="p-2.5 bg-rose-500/10 text-[#E8546A] rounded-lg">
                      <Mail className="w-5 h-5" />
                    </div>
                    <div>
                      <div className="font-medium text-white">Email Notifications</div>
                      <div className="text-xs text-slate-400">Send confirmations, reschedules, and cancellations via Email.</div>
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={() => handleToggle('emailNotificationsEnabled')}
                    className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                      formState.emailNotificationsEnabled ? 'bg-[#E8546A]' : 'bg-slate-700'
                    }`}
                  >
                    <span
                      className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                        formState.emailNotificationsEnabled ? 'translate-x-6' : 'translate-x-1'
                      }`}
                    />
                  </button>
                </div>

                <div className="flex items-center justify-between p-4 bg-[#181D2C] rounded-lg border border-slate-800/80">
                  <div className="flex items-center gap-3">
                    <div className="p-2.5 bg-blue-500/10 text-blue-400 rounded-lg">
                      <MessageSquare className="w-5 h-5" />
                    </div>
                    <div>
                      <div className="font-medium text-white">SMS Notifications</div>
                      <div className="text-xs text-slate-400">Dispatch SMS alerts to customer phone numbers.</div>
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={() => handleToggle('smsNotificationsEnabled')}
                    className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                      formState.smsNotificationsEnabled ? 'bg-[#E8546A]' : 'bg-slate-700'
                    }`}
                  >
                    <span
                      className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                        formState.smsNotificationsEnabled ? 'translate-x-6' : 'translate-x-1'
                      }`}
                    />
                  </button>
                </div>

                <div className="flex items-center justify-between p-4 bg-[#181D2C] rounded-lg border border-slate-800/80">
                  <div className="flex items-center gap-3">
                    <div className="p-2.5 bg-emerald-500/10 text-emerald-400 rounded-lg">
                      <Clock className="w-5 h-5" />
                    </div>
                    <div>
                      <div className="font-medium text-white">24-Hour Appointment Reminder</div>
                      <div className="text-xs text-slate-400">Automatically send reminder 24 hours prior to appointment time.</div>
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={() => handleToggle('reminder24hEnabled')}
                    className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${
                      formState.reminder24hEnabled ? 'bg-[#E8546A]' : 'bg-slate-700'
                    }`}
                  >
                    <span
                      className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                        formState.reminder24hEnabled ? 'translate-x-6' : 'translate-x-1'
                      }`}
                    />
                  </button>
                </div>
              </div>

              {/* Sender Configuration */}
              <div className="pt-4 border-t border-slate-800 space-y-4">
                <h3 className="text-md font-medium text-white">Sender Information</h3>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-xs font-medium text-slate-300 mb-1">Sender Name</label>
                    <input
                      type="text"
                      value={formState.senderName || ''}
                      onChange={(e) => handleInputChange('senderName', e.target.value)}
                      className="w-full bg-[#181D2C] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-[#E8546A]"
                      placeholder="e.g. Bookline Salon"
                    />
                  </div>
                  <div>
                    <label className="block text-xs font-medium text-slate-300 mb-1">Sender Email Address</label>
                    <input
                      type="email"
                      value={formState.senderEmail || ''}
                      onChange={(e) => handleInputChange('senderEmail', e.target.value)}
                      className="w-full bg-[#181D2C] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-[#E8546A]"
                      placeholder="no-reply@bookline.io"
                    />
                  </div>
                </div>
              </div>

              <div className="flex justify-end pt-4 border-t border-slate-800">
                <button
                  type="submit"
                  disabled={!hasUnsavedChanges || updateSettingsMutation.isPending}
                  className="flex items-center gap-2 px-6 py-2.5 bg-[#E8546A] hover:bg-[#d44359] text-white font-medium rounded-lg text-sm transition disabled:opacity-50"
                >
                  {updateSettingsMutation.isPending ? 'Saving Preferences...' : 'Save Settings'}
                </button>
              </div>
            </form>
          </div>

          {/* Info Card */}
          <div className="bg-[#111520] border border-slate-800 rounded-xl p-6 h-fit space-y-4">
            <div className="p-3 bg-emerald-500/10 text-emerald-400 rounded-xl w-fit">
              <ShieldCheck className="w-6 h-6" />
            </div>
            <h3 className="text-lg font-semibold text-white">Notification Integrity</h3>
            <p className="text-xs text-slate-400 leading-relaxed">
              Bookline guarantees deliverability tracking and automated outbox logging for all email and SMS channels. 
              Reminders are evaluated dynamically every 15 minutes by the background scheduling engine.
            </p>
            <div className="p-4 bg-[#181D2C] rounded-lg text-xs space-y-2 text-slate-300">
              <div className="flex justify-between">
                <span className="text-slate-400">Worker Status:</span>
                <span className="text-emerald-400 font-mono font-medium">Active (15m interval)</span>
              </div>
              <div className="flex justify-between">
                <span className="text-slate-400">Outbox Retention:</span>
                <span className="text-slate-200">90 Days</span>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* LOGS TAB */}
      {activeTab === 'logs' && (
        <div className="bg-[#111520] border border-slate-800 rounded-xl overflow-hidden space-y-4 p-6">
          {/* Filters Bar */}
          <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
            <div className="flex items-center gap-3 w-full sm:w-auto">
              <Filter className="w-4 h-4 text-slate-400" />
              <select
                value={statusFilter}
                onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
                className="bg-[#181D2C] border border-slate-700 text-slate-200 text-sm rounded-lg px-3 py-2 focus:outline-none focus:border-[#E8546A]"
              >
                <option value="">All Statuses</option>
                <option value="Sent">Sent</option>
                <option value="Pending">Pending</option>
                <option value="Failed">Failed</option>
              </select>

              <select
                value={channelFilter}
                onChange={(e) => { setChannelFilter(e.target.value); setPage(1); }}
                className="bg-[#181D2C] border border-slate-700 text-slate-200 text-sm rounded-lg px-3 py-2 focus:outline-none focus:border-[#E8546A]"
              >
                <option value="">All Channels</option>
                <option value="Email">Email</option>
                <option value="SMS">SMS</option>
              </select>
            </div>

            <button
              onClick={() => refetchLogs()}
              className="flex items-center gap-2 text-xs text-slate-400 hover:text-white transition"
            >
              <RefreshCw className="w-3.5 h-3.5" />
              Refresh Logs
            </button>
          </div>

          {/* Table */}
          <div className="overflow-x-auto border border-slate-800 rounded-lg">
            <table className="w-full text-left text-sm text-slate-300">
              <thead className="bg-[#181D2C] text-xs uppercase text-slate-400 font-semibold border-b border-slate-800">
                <tr>
                  <th className="px-4 py-3">Recipient</th>
                  <th className="px-4 py-3">Type</th>
                  <th className="px-4 py-3">Channel</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Subject</th>
                  <th className="px-4 py-3">Created</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-800/60">
                {isLogsLoading ? (
                  <tr>
                    <td colSpan={6} className="text-center py-8 text-slate-500">Loading delivery history...</td>
                  </tr>
                ) : logsData?.items.length === 0 ? (
                  <tr>
                    <td colSpan={6} className="text-center py-8 text-slate-500">No notification logs recorded yet.</td>
                  </tr>
                ) : (
                  logsData?.items.map((log: NotificationLogItem) => (
                    <tr key={log.id} className="hover:bg-slate-800/40 transition">
                      <td className="px-4 py-3 font-medium text-white">{log.recipientEmail}</td>
                      <td className="px-4 py-3">
                        <span className="px-2 py-0.5 rounded text-xs bg-slate-800 border border-slate-700 text-slate-300">
                          {log.notificationType}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span className="flex items-center gap-1.5 text-xs text-slate-300">
                          {log.channel === 'Email' ? <Mail className="w-3.5 h-3.5 text-rose-400" /> : <MessageSquare className="w-3.5 h-3.5 text-blue-400" />}
                          {log.channel}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        {log.status === 'Sent' && (
                          <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                            <CheckCircle2 className="w-3 h-3" /> Sent
                          </span>
                        )}
                        {log.status === 'Pending' && (
                          <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded bg-amber-500/10 text-amber-400 border border-amber-500/20">
                            <Clock className="w-3 h-3" /> Pending
                          </span>
                        )}
                        {log.status === 'Failed' && (
                          <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded bg-rose-500/10 text-rose-400 border border-rose-500/20" title={log.errorMessage || 'Error'}>
                            <AlertCircle className="w-3 h-3" /> Failed
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-slate-300 truncate max-w-xs">{log.subject}</td>
                      <td className="px-4 py-3 text-xs text-slate-400">{new Date(log.createdAtUtc).toLocaleString()}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          {logsData && logsData.totalPages > 1 && (
            <div className="flex justify-between items-center pt-2 text-xs text-slate-400">
              <span>Page {logsData.page} of {logsData.totalPages}</span>
              <div className="flex gap-2">
                <button
                  disabled={page <= 1}
                  onClick={() => setPage((p) => p - 1)}
                  className="px-3 py-1 bg-slate-800 rounded border border-slate-700 disabled:opacity-50"
                >
                  Previous
                </button>
                <button
                  disabled={page >= logsData.totalPages}
                  onClick={() => setPage((p) => p + 1)}
                  className="px-3 py-1 bg-slate-800 rounded border border-slate-700 disabled:opacity-50"
                >
                  Next
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* SEND TEST NOTIFICATION MODAL */}
      {isTestModalOpen && (
        <div className="fixed inset-0 bg-black/70 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-[#111520] border border-slate-800 rounded-xl max-w-md w-full p-6 space-y-5 relative">
            <button
              onClick={() => setIsTestModalOpen(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-white"
            >
              <X className="w-5 h-5" />
            </button>

            <h3 className="text-lg font-bold text-white flex items-center gap-2">
              <Send className="w-5 h-5 text-[#E8546A]" />
              Send Test Notification
            </h3>

            {testMessage && (
              <div className="p-3 bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 text-xs rounded-lg">
                {testMessage}
              </div>
            )}

            <form onSubmit={handleSendTestSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Notification Channel</label>
                <div className="grid grid-cols-2 gap-3">
                  <button
                    type="button"
                    onClick={() => setTestChannel('Email')}
                    className={`py-2 text-xs font-medium rounded-lg border transition flex items-center justify-center gap-2 ${
                      testChannel === 'Email'
                        ? 'bg-rose-500/10 border-[#E8546A] text-[#E8546A]'
                        : 'bg-[#181D2C] border-slate-800 text-slate-400'
                    }`}
                  >
                    <Mail className="w-4 h-4" /> Email
                  </button>

                  <button
                    type="button"
                    onClick={() => setTestChannel('SMS')}
                    className={`py-2 text-xs font-medium rounded-lg border transition flex items-center justify-center gap-2 ${
                      testChannel === 'SMS'
                        ? 'bg-blue-500/10 border-blue-500 text-blue-400'
                        : 'bg-[#181D2C] border-slate-800 text-slate-400'
                    }`}
                  >
                    <MessageSquare className="w-4 h-4" /> SMS
                  </button>
                </div>
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Recipient Email *</label>
                <input
                  type="email"
                  required
                  value={testEmail}
                  onChange={(e) => setTestEmail(e.target.value)}
                  className="w-full bg-[#181D2C] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-[#E8546A]"
                  placeholder="test@example.com"
                />
              </div>

              {testChannel === 'SMS' && (
                <div>
                  <label className="block text-xs font-medium text-slate-300 mb-1">Phone Number</label>
                  <input
                    type="tel"
                    value={testPhone}
                    onChange={(e) => setTestPhone(e.target.value)}
                    className="w-full bg-[#181D2C] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-[#E8546A]"
                    placeholder="+1 (555) 019-2834"
                  />
                </div>
              )}

              <div className="flex justify-end gap-3 pt-3">
                <button
                  type="button"
                  onClick={() => setIsTestModalOpen(false)}
                  className="px-4 py-2 bg-slate-800 text-slate-300 text-sm rounded-lg hover:bg-slate-700 transition"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={sendTestMutation.isPending}
                  className="px-4 py-2 bg-[#E8546A] hover:bg-[#d44359] text-white text-sm font-medium rounded-lg transition disabled:opacity-50"
                >
                  {sendTestMutation.isPending ? 'Sending...' : 'Dispatch Test'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
