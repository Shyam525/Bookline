import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  CreditCard,
  DollarSign,
  ArrowUpRight,
  RotateCcw,
  PlusCircle,
  Filter,
  RefreshCw,
  CheckCircle2,
  Clock,
  AlertCircle,
  X,
  Receipt,
  ShoppingCart,
  Building2
} from 'lucide-react';
import { paymentsApi, PaymentItem } from '../../services/api/payments';

export const PaymentsPage: React.FC = () => {
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [statusFilter, setStatusFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');

  const [isPosModalOpen, setIsPosModalOpen] = useState(false);
  const [isRefundModalOpen, setIsRefundModalOpen] = useState(false);
  const [isPayoutModalOpen, setIsPayoutModalOpen] = useState(false);
  const [selectedPayment, setSelectedPayment] = useState<PaymentItem | null>(null);

  // Form states
  const [posAmount, setPosAmount] = useState('');
  const [posMethod, setPosMethod] = useState('Cash');
  const [posNotes, setPosNotes] = useState('');

  const [refundAmount, setRefundAmount] = useState('');
  const [refundReason, setRefundReason] = useState('');

  const [payoutAmount, setPayoutAmount] = useState('');
  const [payoutAccount, setPayoutAccount] = useState('Primary Connected Bank Account');

  // Queries
  const { data: summaryData } = useQuery({
    queryKey: ['paymentSummary'],
    queryFn: () => paymentsApi.getSummary('mock-token')
  });

  const { data: payoutData, refetch: refetchPayout } = useQuery({
    queryKey: ['payoutBalance'],
    queryFn: () => paymentsApi.getPayoutBalance('mock-token')
  });

  const { data: paymentsData, isLoading, refetch } = useQuery({
    queryKey: ['payments', page, statusFilter, typeFilter],
    queryFn: () => paymentsApi.getPayments('mock-token', { pageNumber: page, pageSize: 10, status: statusFilter, type: typeFilter })
  });

  const recordPosMutation = useMutation({
    mutationFn: (data: { amount: number; paymentMethod: string; notes?: string }) =>
      paymentsApi.recordInStorePayment('mock-token', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['payments'] });
      queryClient.invalidateQueries({ queryKey: ['paymentSummary'] });
      setIsPosModalOpen(false);
      setPosAmount('');
      setPosNotes('');
    }
  });

  const refundMutation = useMutation({
    mutationFn: (data: { paymentId: string; refundAmount: number; reason: string }) =>
      paymentsApi.processRefund('mock-token', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['payments'] });
      queryClient.invalidateQueries({ queryKey: ['paymentSummary'] });
      setIsRefundModalOpen(false);
      setSelectedPayment(null);
      setRefundAmount('');
      setRefundReason('');
    }
  });

  const payoutMutation = useMutation({
    mutationFn: (data: { amount: number; destinationAccount: string }) =>
      paymentsApi.requestPayout('mock-token', data),
    onSuccess: () => {
      refetchPayout();
      setIsPayoutModalOpen(false);
      setPayoutAmount('');
    }
  });

  const handlePosSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const numAmount = parseFloat(posAmount);
    if (isNaN(numAmount) || numAmount <= 0) return;
    recordPosMutation.mutate({
      amount: numAmount,
      paymentMethod: posMethod,
      notes: posNotes || undefined
    });
  };

  const handleRefundSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedPayment) return;
    const numAmount = parseFloat(refundAmount);
    if (isNaN(numAmount) || numAmount <= 0) return;
    refundMutation.mutate({
      paymentId: selectedPayment.id,
      refundAmount: numAmount,
      reason: refundReason || 'Customer refund requested'
    });
  };

  const handlePayoutSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const numAmount = parseFloat(payoutAmount);
    if (isNaN(numAmount) || numAmount <= 0) return;
    payoutMutation.mutate({
      amount: numAmount,
      destinationAccount: payoutAccount
    });
  };

  const openRefundModal = (payment: PaymentItem) => {
    setSelectedPayment(payment);
    setRefundAmount(payment.amount.toString());
    setIsRefundModalOpen(true);
  };

  return (
    <div className="space-y-6 text-slate-100">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-slate-800 pb-5">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-white flex items-center gap-3">
            <CreditCard className="w-7 h-7 text-[#E8546A]" />
            Payments & POS Financial Workspace
          </h1>
          <p className="text-sm text-slate-400 mt-1">
            Manage deposit checkouts, point-of-sale transactions, refunds, and financial reporting.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={() => setIsPosModalOpen(true)}
            className="flex items-center gap-2 px-4 py-2 bg-[#E8546A] hover:bg-[#d44359] text-white font-medium rounded-lg text-sm transition shadow-sm"
          >
            <PlusCircle className="w-4 h-4" />
            Record In-Store POS Payment
          </button>
        </div>
      </div>

      {/* SECTION 89: PROVIDER FINANCIAL VIEW (PENDING, AVAILABLE, PAID) */}
      <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[#212638] pb-4">
          <div>
            <span className="text-[10px] font-bold uppercase tracking-wider text-[#34D399] px-2.5 py-0.5 rounded-full bg-[#34D399]/10 border border-[#34D399]/20">
              Provider Treasury &amp; Disbursal Engine (Section 89)
            </span>
            <h2 className="font-heading font-bold text-lg text-white mt-1.5">
              Financial Balances &amp; Payout Abstraction
            </h2>
          </div>
          <button
            onClick={() => setIsPayoutModalOpen(true)}
            className="flex items-center gap-2 px-5 py-2.5 bg-[#34D399] hover:bg-[#2EB885] text-black font-bold rounded-xl text-xs transition shadow-lg shadow-[#34D399]/20 self-start sm:self-auto"
          >
            <DollarSign className="w-4 h-4" />
            <span>Disburse / Request Payout</span>
          </button>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-1">
          {/* 1. Pending */}
          <div className="p-4 rounded-xl bg-[#181D2C] border border-[#212638] space-y-1.5">
            <div className="flex items-center justify-between text-xs text-[#7E88A8]">
              <span className="font-semibold uppercase tracking-wider">Pending Payout</span>
              <Clock className="w-4 h-4 text-[#FBBF24]" />
            </div>
            <div className="text-2xl font-bold font-mono text-[#FBBF24]">
              ₹{(payoutData?.pendingPayoutBalance ?? 2400.0).toFixed(2)}
            </div>
            <p className="text-[11px] text-[#7E88A8]">
              Funds currently in clearing / escrow hold period
            </p>
          </div>

          {/* 2. Available */}
          <div className="p-4 rounded-xl bg-[#181D2C] border border-[#212638] space-y-1.5">
            <div className="flex items-center justify-between text-xs text-[#7E88A8]">
              <span className="font-semibold uppercase tracking-wider">Available For Payout</span>
              <CheckCircle2 className="w-4 h-4 text-[#34D399]" />
            </div>
            <div className="text-2xl font-bold font-mono text-[#34D399]">
              ₹{(payoutData?.availablePayoutBalance ?? 8650.0).toFixed(2)}
            </div>
            <p className="text-[11px] text-[#7E88A8]">
              Immediately ready for payout transfer
            </p>
          </div>

          {/* 3. Paid */}
          <div className="p-4 rounded-xl bg-[#181D2C] border border-[#212638] space-y-1.5">
            <div className="flex items-center justify-between text-xs text-[#7E88A8]">
              <span className="font-semibold uppercase tracking-wider">Total Paid Out</span>
              <Building2 className="w-4 h-4 text-slate-300" />
            </div>
            <div className="text-2xl font-bold font-mono text-white">
              ₹{(payoutData?.paidOutBalance ?? 34500.0).toFixed(2)}
            </div>
            <p className="text-[11px] text-[#7E88A8]">
              Lifetime disbursed to connected bank accounts
            </p>
          </div>
        </div>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        <div className="bg-[#111520] border border-slate-800 rounded-xl p-5 space-y-2">
          <div className="flex justify-between items-center text-slate-400 text-xs font-medium uppercase tracking-wider">
            <span>Total Revenue</span>
            <div className="p-2 bg-emerald-500/10 text-emerald-400 rounded-lg">
              <DollarSign className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-bold text-white">${summaryData?.totalRevenue?.toFixed(2) || '0.00'}</div>
          <div className="text-xs text-slate-400 flex items-center gap-1">
            <ArrowUpRight className="w-3.5 h-3.5 text-emerald-400" />
            <span>Completed sales volume</span>
          </div>
        </div>

        <div className="bg-[#111520] border border-slate-800 rounded-xl p-5 space-y-2">
          <div className="flex justify-between items-center text-slate-400 text-xs font-medium uppercase tracking-wider">
            <span>Total Deposits</span>
            <div className="p-2 bg-blue-500/10 text-blue-400 rounded-lg">
              <ShoppingCart className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-bold text-white">${summaryData?.totalDeposits?.toFixed(2) || '0.00'}</div>
          <div className="text-xs text-slate-400">Advance customer holds</div>
        </div>

        <div className="bg-[#111520] border border-slate-800 rounded-xl p-5 space-y-2">
          <div className="flex justify-between items-center text-slate-400 text-xs font-medium uppercase tracking-wider">
            <span>Refunds Issued</span>
            <div className="p-2 bg-rose-500/10 text-rose-400 rounded-lg">
              <RotateCcw className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-bold text-white">${summaryData?.totalRefunds?.toFixed(2) || '0.00'}</div>
          <div className="text-xs text-slate-400">{summaryData?.refundedCount || 0} total refund records</div>
        </div>

        <div className="bg-[#111520] border border-slate-800 rounded-xl p-5 space-y-2">
          <div className="flex justify-between items-center text-slate-400 text-xs font-medium uppercase tracking-wider">
            <span>Total Transactions</span>
            <div className="p-2 bg-amber-500/10 text-amber-400 rounded-lg">
              <Receipt className="w-4 h-4" />
            </div>
          </div>
          <div className="text-2xl font-bold text-white">{summaryData?.totalTransactionsCount || 0}</div>
          <div className="text-xs text-slate-400">{summaryData?.completedCount || 0} completed successfully</div>
        </div>
      </div>

      {/* Main Transactions Data Table */}
      <div className="bg-[#111520] border border-slate-800 rounded-xl overflow-hidden p-6 space-y-4">
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
              <option value="Completed">Completed</option>
              <option value="Pending">Pending</option>
              <option value="Refunded">Refunded</option>
              <option value="Failed">Failed</option>
            </select>

            <select
              value={typeFilter}
              onChange={(e) => { setTypeFilter(e.target.value); setPage(1); }}
              className="bg-[#181D2C] border border-slate-700 text-slate-200 text-sm rounded-lg px-3 py-2 focus:outline-none focus:border-[#E8546A]"
            >
              <option value="">All Payment Types</option>
              <option value="Deposit">Deposit</option>
              <option value="FullPayment">Full Payment</option>
              <option value="InStorePOS">In-Store POS</option>
              <option value="Refund">Refund</option>
            </select>
          </div>

          <button
            onClick={() => refetch()}
            className="flex items-center gap-2 text-xs text-slate-400 hover:text-white transition"
          >
            <RefreshCw className="w-3.5 h-3.5" />
            Refresh Transactions
          </button>
        </div>

        {/* Table */}
        <div className="overflow-x-auto border border-slate-800 rounded-lg">
          <table className="w-full text-left text-sm text-slate-300">
            <thead className="bg-[#181D2C] text-xs uppercase text-slate-400 font-semibold border-b border-slate-800">
              <tr>
                <th className="px-4 py-3">Customer</th>
                <th className="px-4 py-3">Type</th>
                <th className="px-4 py-3">Method</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Amount</th>
                <th className="px-4 py-3">Date</th>
                <th className="px-4 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-800/60">
              {isLoading ? (
                <tr>
                  <td colSpan={7} className="text-center py-8 text-slate-500">Loading transactions...</td>
                </tr>
              ) : paymentsData?.items.length === 0 ? (
                <tr>
                  <td colSpan={7} className="text-center py-8 text-slate-500">No payment transactions recorded yet.</td>
                </tr>
              ) : (
                paymentsData?.items.map((payment) => (
                  <tr key={payment.id} className="hover:bg-slate-800/40 transition">
                    <td className="px-4 py-3 font-medium text-white">{payment.customerName || 'Walk-in Guest'}</td>
                    <td className="px-4 py-3">
                      <span className="px-2 py-0.5 rounded text-xs bg-slate-800 border border-slate-700 text-slate-300">
                        {payment.paymentType}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-xs text-slate-300 flex items-center gap-1.5 pt-4">
                      {payment.paymentMethod === 'Cash' ? (
                        <Building2 className="w-3.5 h-3.5 text-amber-400" />
                      ) : (
                        <CreditCard className="w-3.5 h-3.5 text-blue-400" />
                      )}
                      {payment.paymentMethod}
                    </td>
                    <td className="px-4 py-3">
                      {payment.status === 'Completed' && (
                        <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                          <CheckCircle2 className="w-3 h-3" /> Completed
                        </span>
                      )}
                      {payment.status === 'Pending' && (
                        <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded bg-amber-500/10 text-amber-400 border border-amber-500/20">
                          <Clock className="w-3 h-3" /> Pending
                        </span>
                      )}
                      {payment.status === 'Refunded' && (
                        <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded bg-rose-500/10 text-rose-400 border border-rose-500/20">
                          <RotateCcw className="w-3 h-3" /> Refunded
                        </span>
                      )}
                      {payment.status === 'Failed' && (
                        <span className="inline-flex items-center gap-1 text-xs px-2 py-0.5 rounded bg-rose-500/10 text-rose-400 border border-rose-500/20">
                          <AlertCircle className="w-3 h-3" /> Failed
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-3 font-semibold text-white">
                      ${payment.amount.toFixed(2)}
                    </td>
                    <td className="px-4 py-3 text-xs text-slate-400">{new Date(payment.createdAtUtc).toLocaleString()}</td>
                    <td className="px-4 py-3 text-right">
                      {payment.status === 'Completed' && payment.paymentType !== 'Refund' && (
                        <button
                          onClick={() => openRefundModal(payment)}
                          className="px-2.5 py-1 text-xs font-medium bg-slate-800 hover:bg-slate-700 text-rose-400 rounded border border-slate-700 transition"
                        >
                          Refund
                        </button>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {paymentsData && paymentsData.totalPages > 1 && (
          <div className="flex justify-between items-center pt-2 text-xs text-slate-400">
            <span>Page {paymentsData.page} of {paymentsData.totalPages}</span>
            <div className="flex gap-2">
              <button
                disabled={page <= 1}
                onClick={() => setPage((p) => p - 1)}
                className="px-3 py-1 bg-slate-800 rounded border border-slate-700 disabled:opacity-50"
              >
                Previous
              </button>
              <button
                disabled={page >= paymentsData.totalPages}
                onClick={() => setPage((p) => p + 1)}
                className="px-3 py-1 bg-slate-800 rounded border border-slate-700 disabled:opacity-50"
              >
                Next
              </button>
            </div>
          </div>
        )}
      </div>

      {/* RECORD IN-STORE POS MODAL */}
      {isPosModalOpen && (
        <div className="fixed inset-0 bg-black/70 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-[#111520] border border-slate-800 rounded-xl max-w-md w-full p-6 space-y-5 relative">
            <button
              onClick={() => setIsPosModalOpen(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-white"
            >
              <X className="w-5 h-5" />
            </button>

            <h3 className="text-lg font-bold text-white flex items-center gap-2">
              <PlusCircle className="w-5 h-5 text-[#E8546A]" />
              Record In-Store POS Payment
            </h3>

            <form onSubmit={handlePosSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Amount ($ USD) *</label>
                <input
                  type="number"
                  step="0.01"
                  required
                  value={posAmount}
                  onChange={(e) => setPosAmount(e.target.value)}
                  className="w-full bg-[#181D2C] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-[#E8546A]"
                  placeholder="e.g. 75.00"
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Payment Method</label>
                <select
                  value={posMethod}
                  onChange={(e) => setPosMethod(e.target.value)}
                  className="w-full bg-[#181D2C] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-[#E8546A]"
                >
                  <option value="Cash">Cash</option>
                  <option value="CreditCard">Credit / Debit Card</option>
                  <option value="POS">Terminal POS</option>
                  <option value="ApplePay">Apple Pay</option>
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Notes / Item Description</label>
                <textarea
                  value={posNotes}
                  onChange={(e) => setPosNotes(e.target.value)}
                  className="w-full bg-[#181D2C] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-[#E8546A]"
                  placeholder="e.g. Walk-in haircut + product purchase"
                  rows={3}
                />
              </div>

              <div className="flex justify-end gap-3 pt-3">
                <button
                  type="button"
                  onClick={() => setIsPosModalOpen(false)}
                  className="px-4 py-2 bg-slate-800 text-slate-300 text-sm rounded-lg hover:bg-slate-700 transition"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={recordPosMutation.isPending}
                  className="px-4 py-2 bg-[#E8546A] hover:bg-[#d44359] text-white text-sm font-medium rounded-lg transition disabled:opacity-50"
                >
                  {recordPosMutation.isPending ? 'Recording...' : 'Record Payment'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* PROCESS REFUND MODAL */}
      {isRefundModalOpen && selectedPayment && (
        <div className="fixed inset-0 bg-black/70 backdrop-blur-sm z-50 flex items-center justify-center p-4">
          <div className="bg-[#111520] border border-slate-800 rounded-xl max-w-md w-full p-6 space-y-5 relative">
            <button
              onClick={() => setIsRefundModalOpen(false)}
              className="absolute top-4 right-4 text-slate-400 hover:text-white"
            >
              <X className="w-5 h-5" />
            </button>

            <h3 className="text-lg font-bold text-white flex items-center gap-2">
              <RotateCcw className="w-5 h-5 text-rose-400" />
              Process Refund
            </h3>

            <div className="p-3 bg-slate-800/60 rounded-lg text-xs space-y-1 text-slate-300 border border-slate-700">
              <div><span className="text-slate-400">Transaction ID:</span> {selectedPayment.id}</div>
              <div><span className="text-slate-400">Original Amount:</span> ${selectedPayment.amount.toFixed(2)}</div>
            </div>

            <form onSubmit={handleRefundSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Refund Amount ($ USD) *</label>
                <input
                  type="number"
                  step="0.01"
                  required
                  value={refundAmount}
                  onChange={(e) => setRefundAmount(e.target.value)}
                  className="w-full bg-[#181D2C] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-[#E8546A]"
                />
              </div>

              <div>
                <label className="block text-xs font-medium text-slate-300 mb-1">Reason for Refund</label>
                <textarea
                  required
                  value={refundReason}
                  onChange={(e) => setRefundReason(e.target.value)}
                  className="w-full bg-[#181D2C] border border-slate-700 rounded-lg px-3 py-2 text-sm text-white focus:outline-none focus:border-[#E8546A]"
                  placeholder="Reason for processing refund..."
                  rows={3}
                />
              </div>

              <div className="flex justify-end gap-3 pt-3">
                <button
                  type="button"
                  onClick={() => setIsRefundModalOpen(false)}
                  className="px-4 py-2 bg-slate-800 text-slate-300 text-sm rounded-lg hover:bg-slate-700 transition"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={refundMutation.isPending}
                  className="px-4 py-2 bg-rose-600 hover:bg-rose-500 text-white text-sm font-medium rounded-lg transition disabled:opacity-50"
                >
                  {refundMutation.isPending ? 'Processing...' : 'Confirm Refund'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* DISBURSE PAYOUT MODAL (Section 89) */}
      {isPayoutModalOpen && (
        <div className="fixed inset-0 bg-black/75 backdrop-blur-sm z-50 flex items-center justify-center p-4 animate-fadeIn">
          <div className="bg-[#111520] border border-[#212638] rounded-2xl max-w-md w-full p-6 space-y-5 relative shadow-2xl">
            <button
              onClick={() => setIsPayoutModalOpen(false)}
              className="absolute top-4 right-4 text-[#7E88A8] hover:text-white"
            >
              <X className="w-5 h-5" />
            </button>

            <div>
              <span className="text-[10px] font-bold uppercase tracking-wider text-[#34D399] px-2 py-0.5 rounded-md bg-[#34D399]/10 border border-[#34D399]/20">
                IPayoutProvider Disbursal
              </span>
              <h3 className="text-lg font-heading font-bold text-white mt-1 flex items-center gap-2">
                <DollarSign className="w-5 h-5 text-[#34D399]" />
                Request Payout Disbursement
              </h3>
            </div>

            <div className="p-3.5 bg-[#181D2C] border border-[#212638] rounded-xl text-xs space-y-1.5">
              <div className="flex justify-between text-[#7E88A8]">
                <span>Available Balance:</span>
                <span className="font-mono text-[#34D399] font-bold">
                  ₹{(payoutData?.availablePayoutBalance ?? 8650.0).toFixed(2)}
                </span>
              </div>
              <div className="flex justify-between text-[#7E88A8]">
                <span>Pending Clearance:</span>
                <span className="font-mono text-[#FBBF24]">
                  ₹{(payoutData?.pendingPayoutBalance ?? 2400.0).toFixed(2)}
                </span>
              </div>
            </div>

            <form onSubmit={handlePayoutSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Disbursement Amount (₹ INR) *
                </label>
                <input
                  type="number"
                  step="0.01"
                  required
                  min="1"
                  max={payoutData?.availablePayoutBalance || 999999}
                  value={payoutAmount}
                  onChange={(e) => setPayoutAmount(e.target.value)}
                  placeholder={`Max ₹${(payoutData?.availablePayoutBalance ?? 8650).toFixed(2)}`}
                  className="w-full bg-[#181D2C] border border-[#212638] rounded-xl px-4 py-2.5 text-sm font-mono text-white focus:outline-none focus:border-[#34D399]"
                />
              </div>

              <div>
                <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Destination Connected Account
                </label>
                <input
                  type="text"
                  required
                  value={payoutAccount}
                  onChange={(e) => setPayoutAccount(e.target.value)}
                  className="w-full bg-[#181D2C] border border-[#212638] rounded-xl px-4 py-2.5 text-xs text-white focus:outline-none focus:border-[#34D399]"
                />
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setIsPayoutModalOpen(false)}
                  className="px-4 py-2.5 bg-[#181D2C] text-[#7E88A8] hover:text-white text-xs font-semibold rounded-xl transition"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={payoutMutation.isPending}
                  className="px-5 py-2.5 bg-[#34D399] hover:bg-[#2EB885] text-black text-xs font-bold rounded-xl transition shadow-lg shadow-[#34D399]/20 disabled:opacity-50 flex items-center gap-1.5"
                >
                  {payoutMutation.isPending ? (
                    <span>Disbursing Funds...</span>
                  ) : (
                    <>
                      <CheckCircle2 className="w-4 h-4" />
                      <span>Authorize Disbursal</span>
                    </>
                  )}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
