import React, { useState } from 'react';
import {
  Link as LinkIcon,
  Copy,
  Check,
  Share2,
  ExternalLink,
  QrCode,
  Smartphone,
  Sparkles,
  X
} from 'lucide-react';

interface DirectBookingLinkBarProps {
  businessName: string;
  slug: string;
  className?: string;
  compact?: boolean;
}

export const DirectBookingLinkBar: React.FC<DirectBookingLinkBarProps> = ({
  businessName,
  slug,
  className = '',
  compact = false,
}) => {
  const [copied, setCopied] = useState(false);
  const [isQrOpen, setIsQrOpen] = useState(false);
  const [isShareModalOpen, setIsShareModalOpen] = useState(false);

  const origin = typeof window !== 'undefined' ? window.location.origin : 'https://bookline.app';
  const storefrontUrl = `${origin}/business/${slug}`;
  const directBookUrl = `${origin}/book/${slug}`;

  const handleCopy = (url: string) => {
    navigator.clipboard.writeText(url);
    setCopied(true);
    setTimeout(() => setCopied(false), 2500);
  };

  const shareText = encodeURIComponent(
    `Book your appointment online with ${businessName} on Bookline: ${directBookUrl}`
  );
  const whatsappUrl = `https://wa.me/?text=${shareText}`;

  // Simple clean SVG QR code placeholder / generator
  const qrSvgUrl = `https://api.qrserver.com/v1/create-qr-code/?size=240x240&data=${encodeURIComponent(directBookUrl)}`;

  if (compact) {
    return (
      <div className={`flex items-center gap-2 ${className}`}>
        <button
          type="button"
          onClick={() => handleCopy(directBookUrl)}
          className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-[#182033] hover:bg-[#212A3E] border border-[#212A3E] rounded-lg text-xs font-medium text-[#ECEFFE] transition"
          title="Copy Direct Booking Link"
        >
          {copied ? <Check className="w-3.5 h-3.5 text-[#34D399]" /> : <Copy className="w-3.5 h-3.5 text-[#E8546A]" />}
          <span>{copied ? 'Copied Link' : 'Copy Link'}</span>
        </button>

        <a
          href={whatsappUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-[#25D366]/10 hover:bg-[#25D366]/20 border border-[#25D366]/30 rounded-lg text-xs font-medium text-[#25D366] transition"
          title="Share via WhatsApp"
        >
          <Share2 className="w-3.5 h-3.5" />
          <span>WhatsApp</span>
        </a>

        <button
          type="button"
          onClick={() => setIsQrOpen(true)}
          className="p-1.5 bg-[#182033] hover:bg-[#212A3E] border border-[#212A3E] rounded-lg text-[#7E88A8] hover:text-[#ECEFFE] transition"
          title="Show QR Code"
        >
          <QrCode className="w-4 h-4" />
        </button>
      </div>
    );
  }

  return (
    <>
      <div className={`bg-gradient-to-r from-[#182033] via-[#141A29] to-[#182033] border border-[#212A3E] rounded-2xl p-5 sm:p-6 shadow-xl ${className}`}>
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-5">
          <div className="space-y-1">
            <div className="inline-flex items-center gap-1.5 text-xs font-semibold text-[#E8546A] uppercase tracking-wider">
              <Sparkles className="w-3.5 h-3.5" />
              Direct Customer Booking Link
            </div>
            <h3 className="text-lg font-serif font-bold text-[#ECEFFE]">
              Share your verified booking page
            </h3>
            <p className="text-xs text-[#7E88A8]">
              Clients can open this link from WhatsApp, Instagram bio, or QR codes to book real-time appointments.
            </p>
          </div>

          <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2.5">
            {/* Link Input with Copy Button */}
            <div className="flex items-center bg-[#0E131F] border border-[#212A3E] rounded-xl px-3 py-2 flex-1 max-w-md">
              <LinkIcon className="w-4 h-4 text-[#7E88A8] shrink-0 mr-2" />
              <input
                type="text"
                readOnly
                value={directBookUrl}
                className="bg-transparent text-xs text-[#ECEFFE] font-mono focus:outline-none w-full truncate"
              />
              <button
                type="button"
                onClick={() => handleCopy(directBookUrl)}
                className="ml-2 inline-flex items-center gap-1 text-xs font-semibold text-[#E8546A] hover:text-[#D44359] shrink-0 transition"
              >
                {copied ? <Check className="w-3.5 h-3.5 text-[#34D399]" /> : <Copy className="w-3.5 h-3.5" />}
                {copied ? 'Copied!' : 'Copy'}
              </button>
            </div>

            {/* Quick Actions */}
            <div className="flex items-center gap-2 shrink-0">
              <a
                href={whatsappUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-[#25D366]/15 hover:bg-[#25D366]/25 border border-[#25D366]/30 text-[#25D366] rounded-xl text-xs font-semibold transition"
              >
                <Share2 className="w-3.5 h-3.5" />
                WhatsApp
              </a>

              <button
                type="button"
                onClick={() => setIsQrOpen(true)}
                className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-[#182033] hover:bg-[#212A3E] border border-[#212A3E] text-[#ECEFFE] rounded-xl text-xs font-semibold transition"
              >
                <QrCode className="w-3.5 h-3.5 text-[#38BDF8]" />
                QR Code
              </button>

              <a
                href={`/business/${slug}`}
                target="_blank"
                rel="noopener noreferrer"
                className="p-2 bg-[#182033] hover:bg-[#212A3E] border border-[#212A3E] text-[#7E88A8] hover:text-[#ECEFFE] rounded-xl transition"
                title="Preview Storefront"
              >
                <ExternalLink className="w-4 h-4" />
              </a>
            </div>
          </div>
        </div>
      </div>

      {/* QR Code Modal */}
      {isQrOpen && (
        <div className="fixed inset-0 z-50 bg-black/75 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-[#141A29] border border-[#212A3E] rounded-2xl max-w-sm w-full p-6 text-center space-y-4 shadow-2xl relative">
            <button
              onClick={() => setIsQrOpen(false)}
              className="absolute top-4 right-4 p-1 text-[#7E88A8] hover:text-[#ECEFFE] rounded-lg"
            >
              <X className="w-5 h-5" />
            </button>

            <div className="space-y-1">
              <h3 className="text-base font-bold text-[#ECEFFE]">Booking QR Code</h3>
              <p className="text-xs text-[#7E88A8]">{businessName}</p>
            </div>

            <div className="bg-white p-4 rounded-xl inline-block shadow-inner">
              <img
                src={qrSvgUrl}
                alt={`QR code for ${businessName}`}
                className="w-48 h-48 mx-auto"
              />
            </div>

            <p className="text-xs text-[#7E88A8]">
              Scan with any mobile camera to open the instant booking page. Print and display at your front desk or on business cards.
            </p>

            <div className="pt-2 flex gap-2">
              <a
                href={qrSvgUrl}
                download={`${slug}-booking-qr.png`}
                target="_blank"
                rel="noopener noreferrer"
                className="flex-1 px-4 py-2 bg-[#E8546A] hover:bg-[#D44359] text-white rounded-xl text-xs font-semibold transition text-center"
              >
                Download QR Code
              </a>
              <button
                type="button"
                onClick={() => handleCopy(directBookUrl)}
                className="px-4 py-2 bg-[#182033] hover:bg-[#212A3E] border border-[#212A3E] text-[#ECEFFE] rounded-xl text-xs font-semibold transition"
              >
                {copied ? 'Copied' : 'Copy URL'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
};

