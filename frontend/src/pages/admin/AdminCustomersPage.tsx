import React, { useState, useEffect } from 'react';
import { useAuth } from '../../app/providers/AuthProvider';
import { adminApi } from '../../services/api/admin';
import {
  Users,
  Search,
  Calendar,
  ShoppingBag,
  Mail,
  Phone,
  Shield,
} from 'lucide-react';

interface AdminCustomer {
  id: string;
  name: string;
  email: string;
  phone?: string;
  totalBookings: number;
  totalOrders: number;
  createdAt: string;
}

export const AdminCustomersPage: React.FC = () => {
  const { token } = useAuth();
  const [customers, setCustomers] = useState<AdminCustomer[]>([
    {
      id: 'c-1',
      name: 'Aarav Patel',
      email: 'customer@bookline.local',
      phone: '+91 98250 12345',
      totalBookings: 6,
      totalOrders: 3,
      createdAt: '2026-09-12',
    },
    {
      id: 'c-2',
      name: 'Priya Sharma',
      email: 'priya.s@example.com',
      phone: '+91 98765 11223',
      totalBookings: 4,
      totalOrders: 1,
      createdAt: '2026-09-15',
    },
    {
      id: 'c-3',
      name: 'Rohan Mehta',
      email: 'rohan.m@example.com',
      phone: '+91 98111 22334',
      totalBookings: 2,
      totalOrders: 2,
      createdAt: '2026-09-20',
    },
    {
      id: 'c-4',
      name: 'Ananya Desai',
      email: 'ananya.d@example.com',
      phone: '+91 98765 99881',
      totalBookings: 5,
      totalOrders: 0,
      createdAt: '2026-09-22',
    },
  ]);
  const [searchQuery, setSearchQuery] = useState('');

  const filtered = customers.filter(
    (c) =>
      c.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      c.email.toLowerCase().includes(searchQuery.toLowerCase())
  );

  return (
    <div className="space-y-6">
      <div>
        <h1 className="font-heading text-2xl font-bold text-white">Universal Customer Directory</h1>
        <p className="text-xs text-[#7E88A8]">
          Review client accounts participating across multi-vendor Bookline storefronts
        </p>
      </div>

      <div className="relative">
        <Search className="w-4 h-4 text-[#7E88A8] absolute left-4 top-3" />
        <input
          type="text"
          placeholder="Search by customer name or email..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="w-full pl-11 pr-4 py-2.5 rounded-xl bg-[#111520] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#FBBF24]"
        />
      </div>

      <div className="bg-[#111520] border border-[#212638] rounded-2xl overflow-hidden shadow-xl">
        <table className="w-full text-left text-xs text-[#ECEFFE]">
          <thead className="bg-[#181D2C] text-[#7E88A8] uppercase tracking-wider font-semibold border-b border-[#212638]">
            <tr>
              <th className="py-3 px-4">Client Profile</th>
              <th className="py-3 px-4">Contact</th>
              <th className="py-3 px-4">Bookings Activity</th>
              <th className="py-3 px-4">Orders Placed</th>
              <th className="py-3 px-4">Registered Date</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-[#212638]">
            {filtered.map((c) => (
              <tr key={c.id} className="hover:bg-[#181D2C]/40 transition-colors">
                <td className="py-3.5 px-4">
                  <div className="flex items-center gap-3">
                    <div className="w-8 h-8 rounded-xl bg-[#181D2C] border border-[#212638] flex items-center justify-center font-bold text-xs text-white">
                      {c.name.substring(0, 1)}
                    </div>
                    <div>
                      <p className="font-semibold text-white">{c.name}</p>
                      <p className="text-[10px] text-[#7E88A8]">{c.email}</p>
                    </div>
                  </div>
                </td>

                <td className="py-3.5 px-4 font-mono text-[11px] text-[#34D399]">
                  {c.phone || 'No phone recorded'}
                </td>

                <td className="py-3.5 px-4">
                  <span className="font-mono font-bold text-white">{c.totalBookings}</span>{' '}
                  <span className="text-[#7E88A8]">appointments</span>
                </td>

                <td className="py-3.5 px-4">
                  <span className="font-mono font-bold text-white">{c.totalOrders}</span>{' '}
                  <span className="text-[#7E88A8]">retail orders</span>
                </td>

                <td className="py-3.5 px-4 text-[#7E88A8]">{c.createdAt}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
