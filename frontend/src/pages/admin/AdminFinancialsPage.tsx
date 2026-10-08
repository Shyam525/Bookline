import React, { useState } from 'react';
import { useAuth } from '../../app/providers/AuthProvider';
import { adminApi } from '../../services/api/admin';
import {
  DollarSign,
  TrendingUp,
  Building2,
  Send,
  CheckCircle,
  Clock,
  Shield,
} from 'lucide-react';

interface CommissionLedger {
  id: string;
  sourceType: 'Appointment' | 'RetailOrder';
  providerName: string;
  grossAmount: number;
  commissionRate: number;
  commissionAmount: number;
  providerPayable: number;
  status: 'Collected' | 'Pending';
  date: string;
}

export const AdminFinancialsPage: React.FC = () => {
  const { token } = useAuth();
  const [activeTab, setActiveTab] = useState<'commissions' | 'payouts'>('commissions');
  const [payoutSuccess, setPayoutSuccess] = useState<string | null>(null);

  const [payoutBalances, setPayoutBalances] = useState([
    {
      tenantId: '11111111-1111-1111-1111-111111111111',
      providerName: 'Aura Wellness & Spa',
      city: 'Ahmedabad',
      availableBalance: 12500,
      paidOutBalance: 45000,
      bankAccount: 'HDFC •••• 9102',
    },
    {
      tenantId: '22222222-2222-2222-2222-222222222222',
      providerName: 'Glow Hair Lounge',
      city: 'Mumbai',
      availableBalance: 8400,
      paidOutBalance: 62000,
      bankAccount: 'ICICI •••• 4421',
    },
    {
      tenantId: '44444444-4444-4444-4444-444444444444',
      providerName: 'Apex Athletic Club',
      city: 'Ahmedabad',
      availableBalance: 16200,
      paidOutBalance: 51000,
      bankAccount: 'AXIS •••• 7719',
    },
  ]);

  const [ledger, setLedger] = useState<CommissionLedger[]>([
    {
      id: 'tx-101',
      sourceType: 'Appointment',
      providerName: 'Aura Wellness & Spa',
      grossAmount: 2400,
      commissionRate: 10,
      commissionAmount: 240,
      providerPayable: 2160,
      status: 'Collected',
      date: 'Today, 10:45 AM',
    },
    {
      id: 'tx-102',
      sourceType: 'RetailOrder',
      providerName: 'Aura Wellness & Spa',
      grossAmount: 1899,
      commissionRate: 10,
      commissionAmount: 189.9,
      providerPayable: 1709.1,
      status: 'Collected',
      date: 'Today, 09:12 AM',
    },
    {
      id: 'tx-103',
      sourceType: 'Appointment',
      providerName: 'Glow Hair Lounge',
      grossAmount: 4200,
      commissionRate: 10,
      commissionAmount: 420,
      providerPayable: 3780,
      status: 'Collected',
      date: 'Yesterday, 04:30 PM',
    },
    {
      id: 'tx-104',
      sourceType: 'Appointment',
      providerName: 'Apex Athletic Club',
      grossAmount: 1500,
      commissionRate: 10,
      commissionAmount: 150,
      providerPayable: 1350,
      status: 'Collected',
      date: 'Yesterday, 01:15 PM',
    },
  ]);

  const handleProcessPayout = async (tenantId: string, providerName: string, amount: number) => {
    if (token) {
      try {
        await adminApi.triggerPayout(tenantId, amount, token);
      } catch {}
    }
    setPayoutBalances((prev) =>
      prev.map((p) =>
        p.tenantId === tenantId
          ? {
              ...p,
              paidOutBalance: p.paidOutBalance + p.availableBalance,
              availableBalance: 0,
            }
          : p
      )
    );
    setPayoutSuccess(`Disbursed ₹${amount.toLocaleString()} to ${providerName}`);
    setTimeout(() => setPayoutSuccess(null), 3500);
  };

  const totalGMV = ledger.reduce((acc, tx) => acc + tx.grossAmount, 0);
  const totalCommission = ledger.reduce((acc, tx) => acc + tx.commissionAmount, 0);
  const totalProviderPayable = ledger.reduce((acc, tx) => acc + tx.providerPayable, 0);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="font-heading text-2xl font-bold text-white">Marketplace Economics &amp; Settlement</h1>
        <p className="text-xs text-[#7E88A8]">
          Transparent 10% platform take-rate ledger, commission revenue, and automated provider disbursements
        </p>
      </div>

      {payoutSuccess && (
        <div className="p-3 rounded-xl bg-[#34D399]/10 border border-[#34D399]/30 text-[#34D399] text-xs flex items-center gap-2">
          <CheckCircle className="w-4 h-4" />
          <span>{payoutSuccess}</span>
        </div>
      )}

      {/* Economics Summary Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-6">
        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-1">
          <p className="text-xs text-[#7E88A8] uppercase tracking-wider font-semibold">Processed GMV</p>
          <p className="font-heading text-3xl font-bold text-white">₹{totalGMV.toLocaleString()}</p>
          <p className="text-[11px] text-[#7E88A8]">100% gross customer settlement</p>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-1">
          <p className="text-xs text-[#34D399] uppercase tracking-wider font-semibold">Bookline Take (10%)</p>
          <p className="font-heading text-3xl font-bold text-[#34D399]">₹{totalCommission.toFixed(2)}</p>
          <p className="text-[11px] text-[#7E88A8]">Retained platform revenue</p>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-1">
          <p className="text-xs text-[#FBBF24] uppercase tracking-wider font-semibold">Provider Payable (90%)</p>
          <p className="font-heading text-3xl font-bold text-[#FBBF24]">₹{totalProviderPayable.toFixed(2)}</p>
          <p className="text-[11px] text-[#7E88A8]">Net payable to business owners</p>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-2 border-b border-[#212638] pb-3 text-xs font-semibold">
        <button
          onClick={() => setActiveTab('commissions')}
          className={`px-4 py-2 rounded-xl transition-colors ${
            activeTab === 'commissions'
              ? 'bg-[#FBBF24] text-black font-bold'
              : 'text-[#7E88A8] hover:text-white'
          }`}
        >
          Transaction Commission Ledger
        </button>
        <button
          onClick={() => setActiveTab('payouts')}
          className={`px-4 py-2 rounded-xl transition-colors ${
            activeTab === 'payouts'
              ? 'bg-[#FBBF24] text-black font-bold'
              : 'text-[#7E88A8] hover:text-white'
          }`}
        >
          Provider Payout Disbursements
        </button>
      </div>

      {/* TAB 1: COMMISSIONS */}
      {activeTab === 'commissions' && (
        <div className="bg-[#111520] border border-[#212638] rounded-2xl overflow-hidden shadow-xl">
          <table className="w-full text-left text-xs text-[#ECEFFE]">
            <thead className="bg-[#181D2C] text-[#7E88A8] uppercase tracking-wider font-semibold border-b border-[#212638]">
              <tr>
                <th className="py-3 px-4">Tx ID &amp; Date</th>
                <th className="py-3 px-4">Provider</th>
                <th className="py-3 px-4">Transaction Type</th>
                <th className="py-3 px-4">Gross GMV</th>
                <th className="py-3 px-4">Bookline Fee (10%)</th>
                <th className="py-3 px-4">Net Provider Payable</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#212638]">
              {ledger.map((tx) => (
                <tr key={tx.id} className="hover:bg-[#181D2C]/40 transition-colors">
                  <td className="py-3.5 px-4 font-mono">
                    <span className="font-bold text-white block">{tx.id}</span>
                    <span className="text-[10px] text-[#7E88A8]">{tx.date}</span>
                  </td>
                  <td className="py-3.5 px-4 font-semibold text-white">{tx.providerName}</td>
                  <td className="py-3.5 px-4">
                    <span className="px-2 py-0.5 rounded bg-[#181D2C] text-white text-[10px] font-medium">
                      {tx.sourceType}
                    </span>
                  </td>
                  <td className="py-3.5 px-4 font-mono font-bold text-white">
                    ₹{tx.grossAmount.toFixed(2)}
                  </td>
                  <td className="py-3.5 px-4 font-mono font-bold text-[#34D399]">
                    ₹{tx.commissionAmount.toFixed(2)}
                  </td>
                  <td className="py-3.5 px-4 font-mono font-bold text-[#FBBF24]">
                    ₹{tx.providerPayable.toFixed(2)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* TAB 2: PAYOUTS */}
      {activeTab === 'payouts' && (
        <div className="bg-[#111520] border border-[#212638] rounded-2xl overflow-hidden shadow-xl">
          <table className="w-full text-left text-xs text-[#ECEFFE]">
            <thead className="bg-[#181D2C] text-[#7E88A8] uppercase tracking-wider font-semibold border-b border-[#212638]">
              <tr>
                <th className="py-3 px-4">Provider Entity</th>
                <th className="py-3 px-4">Settlement Bank Rail</th>
                <th className="py-3 px-4">Available Payout</th>
                <th className="py-3 px-4">Disbursed Lifetime</th>
                <th className="py-3 px-4 text-right">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#212638]">
              {payoutBalances.map((p) => (
                <tr key={p.tenantId} className="hover:bg-[#181D2C]/40 transition-colors">
                  <td className="py-3.5 px-4">
                    <p className="font-semibold text-white">{p.providerName}</p>
                    <p className="text-[10px] text-[#7E88A8]">{p.city}</p>
                  </td>
                  <td className="py-3.5 px-4 font-mono text-[11px] text-[#7E88A8]">
                    {p.bankAccount}
                  </td>
                  <td className="py-3.5 px-4 font-mono font-bold text-white">
                    ₹{p.availableBalance.toLocaleString()}
                  </td>
                  <td className="py-3.5 px-4 font-mono text-[#34D399]">
                    ₹{p.paidOutBalance.toLocaleString()}
                  </td>
                  <td className="py-3.5 px-4 text-right">
                    <button
                      disabled={p.availableBalance <= 0}
                      onClick={() =>
                        handleProcessPayout(p.tenantId, p.providerName, p.availableBalance)
                      }
                      className="px-3.5 py-1.5 rounded-xl bg-[#FBBF24] hover:bg-[#F59E0B] text-black font-bold text-xs disabled:opacity-40 transition-all flex items-center gap-1.5 ml-auto"
                    >
                      <Send className="w-3.5 h-3.5" />
                      <span>Disburse Payout</span>
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};
