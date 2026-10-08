import React, { useState } from 'react';
import { useAuth } from '../../app/providers/AuthProvider';
import { reviewsApi } from '../../services/api/reviews';
import {
  MessageSquare,
  Star,
  CheckCircle,
  XCircle,
  AlertTriangle,
  Building2,
} from 'lucide-react';

interface ModerationReview {
  id: string;
  customerName: string;
  providerName: string;
  rating: number;
  title?: string;
  comment: string;
  moderationStatus: 'Pending' | 'Approved' | 'Rejected';
  createdAt: string;
}

export const AdminReviewsPage: React.FC = () => {
  const { token } = useAuth();
  const [reviews, setReviews] = useState<ModerationReview[]>([
    {
      id: 'rev-mod-1',
      customerName: 'Aarav Patel',
      providerName: 'Aura Wellness & Spa',
      rating: 5,
      title: 'Impeccable calm and precision',
      comment: 'Elena is exceptional. The facility maintains highest sanitation and genuine tranquility. Booked again.',
      moderationStatus: 'Approved',
      createdAt: '2026-10-02',
    },
    {
      id: 'rev-mod-2',
      customerName: 'Priya Sharma',
      providerName: 'Glow Hair Lounge',
      rating: 5,
      title: 'Master-level Balayage colorist',
      comment: 'Marcus transformed my hair tone without any damage. Beautiful modern studio in Bandra.',
      moderationStatus: 'Approved',
      createdAt: '2026-10-04',
    },
    {
      id: 'rev-mod-3',
      customerName: 'Anonymous Client',
      providerName: 'Apex Athletic Club',
      rating: 4,
      title: 'High energy reformer classes',
      comment: 'Top equipment and expert trainers. Parking can be busy during morning rush.',
      moderationStatus: 'Pending',
      createdAt: '2026-10-07',
    },
  ]);

  const handleModerate = async (id: string, status: 'Approved' | 'Rejected') => {
    if (token) {
      try {
        await reviewsApi.moderateReview(id, status, token);
      } catch {}
    }
    setReviews((prev) =>
      prev.map((r) => (r.id === id ? { ...r, moderationStatus: status } : r))
    );
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="font-heading text-2xl font-bold text-white">Review Moderation Queue</h1>
        <p className="text-xs text-[#7E88A8]">
          Review client feedback, protect provider trust, and eliminate spam or abusive content
        </p>
      </div>

      <div className="space-y-4">
        {reviews.map((rev) => (
          <div
            key={rev.id}
            className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-3 shadow-xl"
          >
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-[#212638] pb-3">
              <div className="flex items-center gap-3">
                <span className="font-bold text-white text-sm">{rev.customerName}</span>
                <span className="text-xs text-[#7E88A8] flex items-center gap-1">
                  reviewed <strong className="text-white">{rev.providerName}</strong>
                </span>
                <span className="text-xs text-[#7E88A8]">&bull; {rev.createdAt}</span>
              </div>

              <div className="flex items-center gap-3">
                <div className="flex items-center gap-1 text-[#FBBF24] text-xs">
                  {[...Array(rev.rating)].map((_, i) => (
                    <Star key={i} className="w-3.5 h-3.5 fill-current" />
                  ))}
                </div>
                <span
                  className={`px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider ${
                    rev.moderationStatus === 'Approved'
                      ? 'bg-[#34D399]/15 text-[#34D399] border border-[#34D399]/30'
                      : rev.moderationStatus === 'Rejected'
                      ? 'bg-red-500/15 text-red-400 border border-red-500/30'
                      : 'bg-[#FBBF24]/15 text-[#FBBF24] border border-[#FBBF24]/30'
                  }`}
                >
                  {rev.moderationStatus}
                </span>
              </div>
            </div>

            {rev.title && <h4 className="font-bold text-sm text-white">{rev.title}</h4>}
            <p className="text-xs text-[#7E88A8] leading-relaxed">{rev.comment}</p>

            <div className="pt-2 flex justify-end gap-2">
              {rev.moderationStatus !== 'Approved' && (
                <button
                  onClick={() => handleModerate(rev.id, 'Approved')}
                  className="px-3 py-1.5 rounded-xl bg-[#34D399] hover:bg-[#2ebc87] text-black font-bold text-xs flex items-center gap-1.5"
                >
                  <CheckCircle className="w-3.5 h-3.5" />
                  <span>Approve &amp; Publish</span>
                </button>
              )}
              {rev.moderationStatus !== 'Rejected' && (
                <button
                  onClick={() => handleModerate(rev.id, 'Rejected')}
                  className="px-3 py-1.5 rounded-xl bg-[#181D2C] hover:bg-red-500/20 text-red-400 font-semibold text-xs flex items-center gap-1.5"
                >
                  <XCircle className="w-3.5 h-3.5" />
                  <span>Reject / Flag Spam</span>
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
