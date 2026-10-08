import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useCart } from '../../app/providers/CartContext';
import { useAuth } from '../../app/providers/AuthProvider';
import { ordersApi } from '../../services/api/orders';
import {
  CreditCard,
  Lock,
  Building2,
  CheckCircle,
  AlertCircle,
  Package,
  ShoppingBag,
  ArrowLeft,
  Truck,
} from 'lucide-react';

export const CheckoutPage: React.FC = () => {
  const { items, activeTenantId, activeProviderName, subtotal, clearCart } = useCart();
  const { user, token } = useAuth();
  const navigate = useNavigate();

  const [customerName, setCustomerName] = useState(
    user?.fullName || `${user?.firstName || ''} ${user?.lastName || ''}`.trim() || 'Aarav Patel'
  );
  const [customerEmail, setCustomerEmail] = useState(user?.email || 'customer@bookline.local');
  const [customerPhone, setCustomerPhone] = useState(user?.phone || '+91 98250 12345');
  const [shippingAddress, setShippingAddress] = useState('42 Satellite Road, Ahmedabad, Gujarat 380015');

  // Payment Simulation
  const [cardNumber, setCardNumber] = useState('4242 •••• •••• 4242');
  const [cardExpiry, setCardExpiry] = useState('12/28');
  const [cardCvc, setCardCvc] = useState('888');
  const [processing, setProcessing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Platform fee calculation (10% transparent take-rate)
  const platformFee = Math.round(subtotal * 0.1 * 100) / 100;
  const totalAmount = subtotal + platformFee;

  const handlePlaceOrder = async (e: React.FormEvent) => {
    e.preventDefault();
    if (items.length === 0 || !activeTenantId) return;

    setProcessing(true);
    setError(null);

    try {
      await ordersApi.createOrder(
        {
          tenantId: activeTenantId,
          customerName,
          customerEmail,
          customerPhone,
          shippingAddress,
          items: items.map((i) => ({ productId: i.productId, quantity: i.quantity })),
        },
        token || undefined
      );

      clearCart();
      navigate('/orders');
    } catch (err: any) {
      setError(err.message || 'Payment or order authorization failed.');
      setProcessing(false);
    }
  };

  if (items.length === 0) {
    return (
      <div className="max-w-2xl mx-auto px-6 py-24 text-center space-y-6">
        <div className="w-16 h-16 rounded-2xl bg-[#181D2C] border border-[#212638] text-[#7E88A8] mx-auto flex items-center justify-center">
          <ShoppingBag className="w-8 h-8" />
        </div>
        <h2 className="font-heading text-2xl font-bold text-white">Your Cart is Empty</h2>
        <p className="text-xs text-[#7E88A8]">
          Add boutique products from any provider storefront to begin checkout.
        </p>
        <Link
          to="/discover"
          className="inline-flex items-center gap-2 px-6 py-3 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white text-xs font-bold transition-all shadow-lg"
        >
          Explore Retail Products
        </Link>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-10 space-y-8">
      {/* Top back button */}
      <div>
        <Link
          to="/discover"
          className="inline-flex items-center gap-1.5 text-xs text-[#7E88A8] hover:text-white transition-colors"
        >
          <ArrowLeft className="w-3.5 h-3.5" /> Continue Shopping
        </Link>
        <h1 className="font-heading text-3xl font-bold text-white mt-2">Order Checkout</h1>
        <p className="text-xs text-[#7E88A8]">
          Fulfilling directly from{' '}
          <strong className="text-white">{activeProviderName || 'Verified Provider'}</strong>
        </p>
      </div>

      {error && (
        <div className="p-4 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 text-xs flex items-center gap-2">
          <AlertCircle className="w-4 h-4 flex-shrink-0" />
          <span>{error}</span>
        </div>
      )}

      <form onSubmit={handlePlaceOrder} className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Left: Contact & Shipping + Demo Payment */}
        <div className="lg:col-span-2 space-y-6">
          {/* Customer & Shipping Details Card */}
          <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-4">
            <div className="flex items-center gap-2 border-b border-[#212638] pb-3">
              <Truck className="w-4 h-4 text-[#34D399]" />
              <h2 className="font-heading font-bold text-base text-white">Delivery & Contact Details</h2>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Recipient Full Name
                </label>
                <input
                  type="text"
                  required
                  value={customerName}
                  onChange={(e) => setCustomerName(e.target.value)}
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
                />
              </div>

              <div>
                <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Email Address
                </label>
                <input
                  type="email"
                  required
                  value={customerEmail}
                  onChange={(e) => setCustomerEmail(e.target.value)}
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
                />
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Phone Number
                </label>
                <input
                  type="tel"
                  required
                  value={customerPhone}
                  onChange={(e) => setCustomerPhone(e.target.value)}
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
                />
              </div>

              <div>
                <label className="block text-xs font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Delivery Address / Area
                </label>
                <input
                  type="text"
                  required
                  value={shippingAddress}
                  onChange={(e) => setShippingAddress(e.target.value)}
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
                />
              </div>
            </div>
          </div>

          {/* Secure Payment Simulator Card */}
          <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-4">
            <div className="flex items-center justify-between border-b border-[#212638] pb-3">
              <div className="flex items-center gap-2">
                <CreditCard className="w-4 h-4 text-[#FBBF24]" />
                <h2 className="font-heading font-bold text-base text-white">Payment Method</h2>
              </div>
              <div className="flex items-center gap-1 text-[11px] text-[#34D399]">
                <Lock className="w-3 h-3" />
                <span>256-Bit Encrypted</span>
              </div>
            </div>

            <div className="p-4 rounded-xl bg-[#181D2C] border border-[#212638] space-y-3">
              <div>
                <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Card Number (Test Mode)
                </label>
                <input
                  type="text"
                  value={cardNumber}
                  onChange={(e) => setCardNumber(e.target.value)}
                  className="w-full px-4 py-2 rounded-lg bg-[#111520] border border-[#212638] text-white text-xs font-mono focus:outline-none focus:border-[#FBBF24]"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    Expiry
                  </label>
                  <input
                    type="text"
                    value={cardExpiry}
                    onChange={(e) => setCardExpiry(e.target.value)}
                    className="w-full px-4 py-2 rounded-lg bg-[#111520] border border-[#212638] text-white text-xs font-mono focus:outline-none focus:border-[#FBBF24]"
                  />
                </div>
                <div>
                  <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    CVC
                  </label>
                  <input
                    type="text"
                    value={cardCvc}
                    onChange={(e) => setCardCvc(e.target.value)}
                    className="w-full px-4 py-2 rounded-lg bg-[#111520] border border-[#212638] text-white text-xs font-mono focus:outline-none focus:border-[#FBBF24]"
                  />
                </div>
              </div>

              <p className="text-[10px] text-[#7E88A8]">
                Deterministic sandbox payments active. Authorized upon dispatch.
              </p>
            </div>
          </div>
        </div>

        {/* Right: Order Summary */}
        <div className="space-y-6">
          <div className="bg-[#111520] border border-[#212638] rounded-2xl p-6 space-y-4 shadow-xl">
            <h2 className="font-heading font-bold text-base text-white border-b border-[#212638] pb-3">
              Order Summary
            </h2>

            {/* Provider Pill */}
            <div className="flex items-center gap-2 px-3 py-2 rounded-xl bg-[#181D2C] border border-[#212638] text-xs text-white">
              <Building2 className="w-3.5 h-3.5 text-[#E8546A]" />
              <span className="font-semibold truncate">{activeProviderName}</span>
            </div>

            {/* Items List */}
            <div className="space-y-3 max-h-60 overflow-y-auto pr-1">
              {items.map((item) => (
                <div key={item.productId} className="flex items-center justify-between text-xs">
                  <div className="space-y-0.5">
                    <p className="font-medium text-white truncate max-w-[170px]">{item.productName || item.name}</p>
                    <p className="text-[10px] text-[#7E88A8]">
                      Qty: {item.quantity} &times; {item.currency || '₹'}{item.unitPrice || item.price || 0}
                    </p>
                  </div>
                  <span className="font-mono text-white font-bold">
                    {item.currency || '₹'}{item.quantity * (item.unitPrice || item.price || 0)}
                  </span>
                </div>
              ))}
            </div>

            {/* Calculations */}
            <div className="border-t border-[#212638] pt-3 space-y-2 text-xs">
              <div className="flex justify-between text-[#7E88A8]">
                <span>Items Subtotal</span>
                <span className="font-mono text-white">₹{subtotal.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-[#7E88A8]">
                <span>Marketplace Fee (10%)</span>
                <span className="font-mono text-white">₹{platformFee.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-[#7E88A8]">
                <span>Fulfillment Dispatch</span>
                <span className="text-[#34D399] font-semibold">Free Local Delivery</span>
              </div>
              <div className="border-t border-[#212638] pt-2 flex justify-between font-bold text-sm text-white">
                <span>Total Payable</span>
                <span className="font-heading text-lg text-[#FBBF24]">
                  ₹{totalAmount.toFixed(2)}
                </span>
              </div>
            </div>

            {/* Submit Action */}
            <button
              type="submit"
              disabled={processing}
              className="w-full py-3.5 rounded-xl bg-[#FBBF24] hover:bg-[#F59E0B] text-black text-xs font-bold transition-all shadow-lg shadow-[#FBBF24]/20 flex items-center justify-center gap-2 disabled:opacity-50 hover:scale-[1.02]"
            >
              {processing ? (
                <span>Authorizing & Allocating Stock...</span>
              ) : (
                <>
                  <CheckCircle className="w-4 h-4" />
                  <span>Authorize & Place Order</span>
                </>
              )}
            </button>
          </div>
        </div>
      </form>
    </div>
  );
};
