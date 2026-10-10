import React, { useState } from 'react';
import {
  ShoppingBag,
  Plus,
  Minus,
  Trash2,
  Check,
  CheckCircle2,
  Clock,
  Truck,
  Package,
  CreditCard,
  AlertCircle,
  ShieldCheck,
  ArrowRight,
  ExternalLink,
  Copy,
  Receipt,
  Sparkles,
  AlertTriangle,
} from 'lucide-react';
import { clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';
import { ProviderImage } from '../common/ProviderImage';

/* =========================================================================
 * 1. PRODUCT CARD (Section 105, 109, 110)
 * ========================================================================= */

export interface ProductData {
  id: string;
  name: string;
  description?: string;
  price: number;
  originalPrice?: number;
  currency?: string;
  imageUrl?: string | null;
  stock: number;
  category?: string;
  isFeatured?: boolean;
  unit?: string;
}

export interface ProductCardProps {
  product: ProductData;
  inCartQuantity?: number;
  onAddToCart?: (product: ProductData) => void;
  onUpdateQuantity?: (productId: string, quantity: number) => void;
  isLoading?: boolean;
  disabled?: boolean;
  isSelected?: boolean;
  className?: string;
}

export const ProductCard: React.FC<ProductCardProps> = ({
  product,
  inCartQuantity = 0,
  onAddToCart,
  onUpdateQuantity,
  isLoading = false,
  disabled = false,
  isSelected = false,
  className = '',
}) => {
  const p = product;
  const curr = p.currency || '$';
  const isOutOfStock = p.stock <= 0;
  const isLowStock = p.stock > 0 && p.stock <= 3;
  const isActionDisabled = disabled || isOutOfStock || isLoading;

  return (
    <div
      role="article"
      aria-label={p.name}
      className={twMerge(
        clsx(
          'group relative flex flex-col rounded-2xl border transition-all duration-200 overflow-hidden select-none',
          'bg-[#111520] hover:bg-[#151B27]',
          isSelected
            ? 'border-[#E8546A] ring-1 ring-[#E8546A]/50 shadow-lg shadow-[#E8546A]/10'
            : 'border-[#212638] hover:border-[#313A52]',
          disabled && 'opacity-60 pointer-events-none',
          className
        )
      )}
    >
      {/* Product Image */}
      <div className="relative aspect-square w-full bg-[#181D2C] overflow-hidden">
        <ProviderImage
          src={p.imageUrl}
          alt={p.name}
          type="product"
          category={p.category || 'Retail Product'}
          className="w-full h-full object-cover transition-transform duration-300 group-hover:scale-105"
          loading="lazy"
        />

        {/* Stock status badge */}
        <div className="absolute top-2.5 left-2.5 z-10">
          {isOutOfStock ? (
            <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[10px] font-bold bg-[#EF4444]/20 border border-[#EF4444]/40 text-[#EF4444] backdrop-blur-md">
              Out of Stock
            </span>
          ) : isLowStock ? (
            <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[10px] font-bold bg-[#FBBF24]/20 border border-[#FBBF24]/40 text-[#FBBF24] backdrop-blur-md">
              Only {p.stock} left
            </span>
          ) : (
            <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[10px] font-bold bg-[#34D399]/20 border border-[#34D399]/40 text-[#34D399] backdrop-blur-md">
              In Stock ({p.stock})
            </span>
          )}
        </div>

        {/* Featured pill */}
        {p.isFeatured && (
          <div className="absolute top-2.5 right-2.5 z-10">
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold bg-[#E8546A]/20 border border-[#E8546A]/40 text-[#E8546A] backdrop-blur-md">
              <Sparkles className="w-2.5 h-2.5" />
              Featured
            </span>
          </div>
        )}
      </div>

      {/* Product Content */}
      <div className="flex flex-col flex-1 p-4 justify-between gap-3">
        <div>
          {p.category && (
            <p className="text-[11px] font-medium text-[#7E88A8] uppercase tracking-wider mb-1">
              {p.category}
            </p>
          )}
          <h4 className="text-sm font-semibold text-white group-hover:text-[#F4F6FA] line-clamp-1">
            {p.name}
          </h4>
          {p.description && (
            <p className="text-xs text-[#7E88A8] line-clamp-2 mt-1 leading-relaxed">
              {p.description}
            </p>
          )}
        </div>

        {/* Pricing and Action */}
        <div className="pt-2 border-t border-[#212638] flex items-center justify-between gap-2">
          <div>
            <div className="flex items-baseline gap-1.5">
              <span className="text-base font-bold text-white">
                {curr}{p.price.toFixed(2)}
              </span>
              {p.originalPrice && p.originalPrice > p.price && (
                <span className="text-xs text-[#7E88A8] line-through">
                  {curr}{p.originalPrice.toFixed(2)}
                </span>
              )}
            </div>
            {p.unit && <p className="text-[10px] text-[#7E88A8]">per {p.unit}</p>}
          </div>

          {/* Cart Stepper or Add Button */}
          {inCartQuantity > 0 ? (
            <div className="flex items-center gap-1 bg-[#181D2C] border border-[#212638] rounded-xl p-1">
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  onUpdateQuantity?.(p.id, inCartQuantity - 1);
                }}
                className="w-7 h-7 flex items-center justify-center rounded-lg text-[#7E88A8] hover:text-white hover:bg-[#212638] active:scale-95 transition-all"
                aria-label="Decrease quantity"
              >
                <Minus className="w-3.5 h-3.5" />
              </button>
              <span className="min-w-[20px] text-center text-xs font-bold text-white">
                {inCartQuantity}
              </span>
              <button
                type="button"
                disabled={inCartQuantity >= p.stock}
                onClick={(e) => {
                  e.stopPropagation();
                  onUpdateQuantity?.(p.id, inCartQuantity + 1);
                }}
                className="w-7 h-7 flex items-center justify-center rounded-lg text-[#7E88A8] hover:text-white hover:bg-[#212638] disabled:opacity-40 disabled:hover:bg-transparent active:scale-95 transition-all"
                aria-label="Increase quantity"
              >
                <Plus className="w-3.5 h-3.5" />
              </button>
            </div>
          ) : (
            <button
              type="button"
              disabled={isActionDisabled}
              onClick={(e) => {
                e.stopPropagation();
                onAddToCart?.(p);
              }}
              className={twMerge(
                clsx(
                  'h-9 px-3.5 rounded-xl font-medium text-xs flex items-center gap-1.5 transition-all duration-150 active:scale-95',
                  isOutOfStock
                    ? 'bg-[#181D2C] text-[#7E88A8] cursor-not-allowed border border-[#212638]'
                    : 'bg-[#E8546A] hover:bg-[#F26279] text-white shadow-md shadow-[#E8546A]/20'
                )
              )}
            >
              <ShoppingBag className="w-3.5 h-3.5" />
              <span>{isOutOfStock ? 'Sold Out' : 'Add'}</span>
            </button>
          )}
        </div>
      </div>
    </div>
  );
};

/* =========================================================================
 * 2. CART ITEM (Section 109, 110)
 * ========================================================================= */

export interface CartItemProps {
  id: string;
  name: string;
  price: number;
  quantity: number;
  maxStock?: number;
  imageUrl?: string | null;
  variantName?: string;
  currency?: string;
  disabled?: boolean;
  isLoading?: boolean;
  onUpdateQuantity?: (id: string, newQty: number) => void;
  onRemove?: (id: string) => void;
  className?: string;
}

export const CartItem: React.FC<CartItemProps> = ({
  id,
  name,
  price,
  quantity,
  maxStock = 99,
  imageUrl,
  variantName,
  currency = '$',
  disabled = false,
  isLoading = false,
  onUpdateQuantity,
  onRemove,
  className = '',
}) => {
  const lineTotal = price * quantity;

  return (
    <div
      className={twMerge(
        clsx(
          'p-3.5 sm:p-4 rounded-xl bg-[#181D2C] border border-[#212638] flex items-center gap-3.5 transition-all',
          disabled && 'opacity-60 pointer-events-none',
          className
        )
      )}
    >
      {/* Thumbnail */}
      <div className="w-14 h-14 sm:w-16 sm:h-16 rounded-lg bg-[#111520] border border-[#212638] overflow-hidden shrink-0">
        <ProviderImage
          src={imageUrl}
          alt={name}
          type="product"
          className="w-full h-full object-cover"
        />
      </div>

      {/* Info */}
      <div className="flex-1 min-w-0">
        <h4 className="text-sm font-semibold text-white truncate">{name}</h4>
        {variantName && (
          <p className="text-[11px] text-[#7E88A8] truncate">{variantName}</p>
        )}
        <div className="flex items-center gap-2 mt-1">
          <span className="text-xs font-bold text-[#E8546A]">
            {currency}{price.toFixed(2)}
          </span>
          <span className="text-[10px] text-[#7E88A8]">× {quantity}</span>
        </div>
      </div>

      {/* Total & Controls */}
      <div className="flex flex-col items-end gap-2 shrink-0">
        <div className="flex items-center gap-2">
          <span className="text-sm font-bold text-white">
            {currency}{lineTotal.toFixed(2)}
          </span>
          <button
            type="button"
            onClick={() => onRemove?.(id)}
            disabled={disabled || isLoading}
            className="p-1 text-[#7E88A8] hover:text-[#EF4444] rounded transition-colors"
            title="Remove from cart"
            aria-label={`Remove ${name} from cart`}
          >
            <Trash2 className="w-4 h-4" />
          </button>
        </div>

        {/* Stepper */}
        <div className="flex items-center gap-1 bg-[#111520] border border-[#212638] rounded-lg px-2 py-0.5">
          <button
            type="button"
            onClick={() => onUpdateQuantity?.(id, quantity - 1)}
            disabled={disabled || isLoading || quantity <= 1}
            className="text-[#7E88A8] hover:text-white disabled:opacity-30 p-1"
            aria-label="Decrease quantity"
          >
            <Minus className="w-3 h-3" />
          </button>
          <span className="text-xs font-bold text-white min-w-[16px] text-center">
            {quantity}
          </span>
          <button
            type="button"
            onClick={() => onUpdateQuantity?.(id, quantity + 1)}
            disabled={disabled || isLoading || quantity >= maxStock}
            className="text-[#7E88A8] hover:text-white disabled:opacity-30 p-1"
            aria-label="Increase quantity"
          >
            <Plus className="w-3 h-3" />
          </button>
        </div>
      </div>
    </div>
  );
};

/* =========================================================================
 * 3. CART SUMMARY (Section 109, 110)
 * ========================================================================= */

export interface CartSummaryProps {
  subtotal: number;
  tax?: number;
  discount?: number;
  depositRequired?: number;
  platformFee?: number;
  currency?: string;
  itemCount: number;
  isLoading?: boolean;
  disabled?: boolean;
  onApplyPromo?: (code: string) => Promise<boolean> | boolean | void;
  onCheckout?: () => void;
  checkoutLabel?: string;
  className?: string;
}

export const CartSummary: React.FC<CartSummaryProps> = ({
  subtotal,
  tax = 0,
  discount = 0,
  depositRequired,
  platformFee = 0,
  currency = '$',
  itemCount,
  isLoading = false,
  disabled = false,
  onApplyPromo,
  onCheckout,
  checkoutLabel = 'Proceed to Checkout',
  className = '',
}) => {
  const [promoInput, setPromoInput] = useState('');
  const [appliedPromo, setAppliedPromo] = useState<string | null>(null);

  const total = Math.max(0, subtotal + tax + platformFee - discount);

  const handleApplyPromo = (e: React.FormEvent) => {
    e.preventDefault();
    if (!promoInput.trim()) return;
    onApplyPromo?.(promoInput.trim());
    setAppliedPromo(promoInput.trim().toUpperCase());
    setPromoInput('');
  };

  return (
    <div
      className={twMerge(
        clsx(
          'p-5 sm:p-6 rounded-2xl bg-[#111520] border border-[#212638] space-y-4 shadow-xl',
          className
        )
      )}
    >
      <div className="flex items-center justify-between pb-3 border-b border-[#212638]">
        <h3 className="font-heading text-base font-bold text-white">Order Summary</h3>
        <span className="text-xs text-[#7E88A8] font-medium">
          {itemCount} {itemCount === 1 ? 'item' : 'items'}
        </span>
      </div>

      {/* Cost Breakdown */}
      <div className="space-y-2.5 text-xs text-[#7E88A8]">
        <div className="flex justify-between">
          <span>Subtotal</span>
          <span className="text-white font-medium">{currency}{subtotal.toFixed(2)}</span>
        </div>

        {platformFee > 0 && (
          <div className="flex justify-between">
            <span>Platform Service Fee</span>
            <span className="text-white font-medium">{currency}{platformFee.toFixed(2)}</span>
          </div>
        )}

        {tax > 0 && (
          <div className="flex justify-between">
            <span>Estimated Tax (GST / VAT)</span>
            <span className="text-white font-medium">{currency}{tax.toFixed(2)}</span>
          </div>
        )}

        {discount > 0 && (
          <div className="flex justify-between text-[#34D399]">
            <span>Discount {appliedPromo ? `(${appliedPromo})` : ''}</span>
            <span className="font-bold">-{currency}{discount.toFixed(2)}</span>
          </div>
        )}

        {depositRequired != null && depositRequired > 0 && (
          <div className="pt-2 border-t border-[#212638]/70 flex justify-between text-[#FBBF24]">
            <span className="font-medium">Online Deposit Due Now</span>
            <span className="font-bold">{currency}{depositRequired.toFixed(2)}</span>
          </div>
        )}
      </div>

      {/* Promo Code Input */}
      {onApplyPromo && !appliedPromo && (
        <form onSubmit={handleApplyPromo} className="flex gap-2 pt-1">
          <input
            type="text"
            placeholder="Promo code (e.g. WELCOME10)"
            value={promoInput}
            onChange={(e) => setPromoInput(e.target.value)}
            className="flex-1 bg-[#181D2C] border border-[#212638] rounded-xl px-3 py-2 text-xs text-white placeholder-[#7E88A8] focus:outline-none focus:border-[#E8546A]"
          />
          <button
            type="submit"
            disabled={!promoInput.trim()}
            className="px-3 py-2 bg-[#212638] hover:bg-[#2C344A] text-white text-xs font-semibold rounded-xl disabled:opacity-40 transition-colors"
          >
            Apply
          </button>
        </form>
      )}

      {/* Total Due */}
      <div className="pt-3 border-t border-[#212638] flex items-baseline justify-between">
        <div>
          <span className="text-sm font-bold text-white">Estimated Total</span>
          {depositRequired != null && depositRequired < total && (
            <p className="text-[10px] text-[#7E88A8]">Remaining due at service venue</p>
          )}
        </div>
        <div className="text-right">
          <span className="text-xl font-heading font-extrabold text-[#E8546A]">
            {currency}{total.toFixed(2)}
          </span>
        </div>
      </div>

      {/* Secure guarantee */}
      <div className="flex items-center gap-2 p-2.5 rounded-xl bg-[#181D2C]/70 border border-[#212638] text-[11px] text-[#7E88A8]">
        <ShieldCheck className="w-4 h-4 text-[#34D399] shrink-0" />
        <span>End-to-end encrypted checkout with instant hold protection</span>
      </div>

      {/* Checkout CTA */}
      <button
        type="button"
        disabled={disabled || isLoading || itemCount === 0}
        onClick={onCheckout}
        className={twMerge(
          clsx(
            'w-full py-3.5 px-4 rounded-xl font-semibold text-sm flex items-center justify-center gap-2 transition-all duration-150 select-none shadow-lg shadow-[#E8546A]/20 active:scale-[0.99]',
            disabled || itemCount === 0 || isLoading
              ? 'bg-[#212638] text-[#7E88A8] cursor-not-allowed'
              : 'bg-[#E8546A] hover:bg-[#F26279] text-white'
          )
        )}
      >
        {isLoading ? (
          <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
        ) : (
          <>
            <span>{checkoutLabel}</span>
            <ArrowRight className="w-4 h-4" />
          </>
        )}
      </button>
    </div>
  );
};

/* =========================================================================
 * 4. ORDER STATUS (Section 109, 110)
 * ========================================================================= */

export type OrderStatusStep =
  | 'PLACED'
  | 'PAID'
  | 'PROCESSING'
  | 'SHIPPED'
  | 'DELIVERED'
  | 'CANCELLED';

export interface OrderStatusProps {
  currentStatus: OrderStatusStep;
  orderNumber: string;
  orderDate?: string;
  estimatedDelivery?: string;
  trackingNumber?: string;
  carrierName?: string;
  cancellationReason?: string;
  onTrackOrder?: () => void;
  className?: string;
}

const STEPS: { key: OrderStatusStep; label: string; icon: React.ElementType }[] = [
  { key: 'PLACED', label: 'Order Placed', icon: Clock },
  { key: 'PAID', label: 'Payment Confirmed', icon: CreditCard },
  { key: 'PROCESSING', label: 'Processing', icon: Package },
  { key: 'SHIPPED', label: 'Dispatched', icon: Truck },
  { key: 'DELIVERED', label: 'Delivered', icon: CheckCircle2 },
];

export const OrderStatus: React.FC<OrderStatusProps> = ({
  currentStatus,
  orderNumber,
  orderDate,
  estimatedDelivery,
  trackingNumber,
  carrierName,
  cancellationReason,
  onTrackOrder,
  className = '',
}) => {
  const isCancelled = currentStatus === 'CANCELLED';
  const currentIndex = STEPS.findIndex((s) => s.key === currentStatus);

  return (
    <div
      className={twMerge(
        clsx(
          'p-6 rounded-2xl bg-[#111520] border border-[#212638] space-y-6',
          className
        )
      )}
    >
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-[#212638]">
        <div>
          <div className="flex items-center gap-2">
            <span className="text-xs font-mono uppercase tracking-wider text-[#7E88A8]">
              Order #{orderNumber}
            </span>
            {isCancelled && (
              <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-[#EF4444]/20 border border-[#EF4444]/40 text-[#EF4444]">
                CANCELLED
              </span>
            )}
          </div>
          <h3 className="font-heading text-lg font-bold text-white mt-0.5">
            {isCancelled
              ? 'Order Has Been Cancelled'
              : currentStatus === 'DELIVERED'
              ? 'Delivered Successfully'
              : 'Order is in Progress'}
          </h3>
        </div>

        {orderDate && (
          <div className="text-xs text-[#7E88A8]">
            <span>Placed on </span>
            <span className="text-white font-medium">{orderDate}</span>
          </div>
        )}
      </div>

      {/* Cancelled Banner */}
      {isCancelled ? (
        <div className="p-4 rounded-xl bg-[#EF4444]/10 border border-[#EF4444]/30 flex items-start gap-3 text-xs text-[#EF4444]">
          <AlertCircle className="w-5 h-5 shrink-0 mt-0.5" />
          <div>
            <h5 className="font-bold">Cancellation Notice</h5>
            <p className="mt-0.5 text-[#F87171]">
              {cancellationReason ||
                'This order was cancelled and a full refund has been credited back to your original payment method.'}
            </p>
          </div>
        </div>
      ) : (
        /* Stepper */
        <div className="relative py-2">
          {/* Track line behind */}
          <div className="absolute top-5 left-4 right-4 h-0.5 bg-[#212638] hidden sm:block -z-0" />

          <div className="grid grid-cols-2 sm:grid-cols-5 gap-4 sm:gap-2 relative z-10">
            {STEPS.map((step, idx) => {
              const Icon = step.icon;
              const isDone = currentIndex >= idx;
              const isCurrent = currentIndex === idx;

              return (
                <div
                  key={step.key}
                  className="flex flex-col sm:items-center text-left sm:text-center space-y-2"
                >
                  <div
                    className={twMerge(
                      clsx(
                        'w-10 h-10 rounded-xl flex items-center justify-center transition-all',
                        isDone
                          ? 'bg-[#34D399]/20 border border-[#34D399]/50 text-[#34D399]'
                          : 'bg-[#181D2C] border border-[#212638] text-[#7E88A8]',
                        isCurrent &&
                          'ring-2 ring-[#34D399] shadow-lg shadow-[#34D399]/20 animate-pulse'
                      )
                    )}
                  >
                    {isDone && !isCurrent ? (
                      <Check className="w-4 h-4 text-[#34D399]" />
                    ) : (
                      <Icon className="w-4 h-4" />
                    )}
                  </div>
                  <div>
                    <p
                      className={twMerge(
                        clsx(
                          'text-xs font-semibold',
                          isDone ? 'text-white' : 'text-[#7E88A8]'
                        )
                      )}
                    >
                      {step.label}
                    </p>
                    {isCurrent && (
                      <span className="text-[10px] font-bold text-[#34D399]">
                        Current Stage
                      </span>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Delivery / Tracking Details */}
      {(estimatedDelivery || trackingNumber) && !isCancelled && (
        <div className="p-4 rounded-xl bg-[#181D2C] border border-[#212638] flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs">
          <div className="space-y-1">
            {estimatedDelivery && (
              <p className="text-[#7E88A8]">
                Estimated Delivery:{' '}
                <strong className="text-white font-semibold">{estimatedDelivery}</strong>
              </p>
            )}
            {trackingNumber && (
              <p className="text-[#7E88A8]">
                Tracking Number ({carrierName || 'Courier'}):{' '}
                <span className="font-mono text-white font-bold">{trackingNumber}</span>
              </p>
            )}
          </div>
          {onTrackOrder && (
            <button
              type="button"
              onClick={onTrackOrder}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[#212638] hover:bg-[#2C344A] text-white text-xs font-medium transition-colors self-start sm:self-auto"
            >
              <span>Track Live</span>
              <ExternalLink className="w-3 h-3" />
            </button>
          )}
        </div>
      )}
    </div>
  );
};

/* =========================================================================
 * 5. PAYMENT SUMMARY (Section 109, 110)
 * ========================================================================= */

export interface PaymentSummaryProps {
  depositPaid: number;
  balanceDue: number;
  totalAmount: number;
  currency?: string;
  paymentMethod?: {
    type: 'CARD' | 'UPI' | 'CASH' | 'WALLET' | 'BANK_TRANSFER';
    last4?: string;
    brand?: string;
  };
  transactionId?: string;
  paidAt?: string;
  dueAt?: string;
  receiptUrl?: string;
  onDownloadReceipt?: () => void;
  className?: string;
}

export const PaymentSummary: React.FC<PaymentSummaryProps> = ({
  depositPaid,
  balanceDue,
  totalAmount,
  currency = '$',
  paymentMethod,
  transactionId,
  paidAt,
  dueAt,
  receiptUrl,
  onDownloadReceipt,
  className = '',
}) => {
  const [copied, setCopied] = useState(false);

  const handleCopyTx = () => {
    if (!transactionId) return;
    navigator.clipboard?.writeText(transactionId);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  return (
    <div
      className={twMerge(
        clsx(
          'p-6 rounded-2xl bg-[#111520] border border-[#212638] space-y-5',
          className
        )
      )}
    >
      {/* Header */}
      <div className="flex items-center justify-between pb-4 border-b border-[#212638]">
        <div className="flex items-center gap-2.5">
          <div className="w-9 h-9 rounded-xl bg-[#34D399]/10 text-[#34D399] flex items-center justify-center">
            <CreditCard className="w-5 h-5" />
          </div>
          <div>
            <h3 className="font-heading text-base font-bold text-white">Payment Split & Receipt</h3>
            <p className="text-xs text-[#7E88A8]">Verified multi-stage escrow ledger</p>
          </div>
        </div>

        {onDownloadReceipt && (
          <button
            type="button"
            onClick={onDownloadReceipt}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] border border-[#212638] text-xs font-semibold text-white transition-colors"
          >
            <Receipt className="w-3.5 h-3.5 text-[#E8546A]" />
            <span>Download Invoice</span>
          </button>
        )}
      </div>

      {/* Two-Column Split: Deposit Paid vs Balance Due */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        {/* Deposit Paid */}
        <div className="p-4 rounded-xl bg-[#181D2C] border border-[#212638] space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs text-[#7E88A8] font-medium">Deposit Paid (Online)</span>
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-bold bg-[#34D399]/20 text-[#34D399]">
              <Check className="w-3 h-3" /> Paid
            </span>
          </div>
          <div className="text-2xl font-heading font-extrabold text-white">
            {currency}{depositPaid.toFixed(2)}
          </div>
          {paidAt && <p className="text-[10px] text-[#7E88A8]">Settled on {paidAt}</p>}
        </div>

        {/* Balance Due */}
        <div className="p-4 rounded-xl bg-[#181D2C] border border-[#212638] space-y-2">
          <div className="flex items-center justify-between">
            <span className="text-xs text-[#7E88A8] font-medium">Balance Due</span>
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-bold bg-[#FBBF24]/20 text-[#FBBF24]">
              Pending
            </span>
          </div>
          <div className="text-2xl font-heading font-extrabold text-[#FBBF24]">
            {currency}{balanceDue.toFixed(2)}
          </div>
          <p className="text-[10px] text-[#7E88A8]">
            {dueAt ? `Payable by ${dueAt}` : 'Pay at venue upon service completion'}
          </p>
        </div>
      </div>

      {/* Payment details meta */}
      <div className="p-4 rounded-xl bg-[#0E121B] border border-[#212638] space-y-2 text-xs">
        <div className="flex justify-between items-center text-[#7E88A8]">
          <span>Total Service Value</span>
          <span className="text-white font-bold">{currency}{totalAmount.toFixed(2)}</span>
        </div>

        {paymentMethod && (
          <div className="flex justify-between items-center text-[#7E88A8]">
            <span>Payment Method</span>
            <span className="text-white font-medium flex items-center gap-1.5">
              <span>{paymentMethod.brand || paymentMethod.type}</span>
              {paymentMethod.last4 && <span>•••• {paymentMethod.last4}</span>}
            </span>
          </div>
        )}

        {transactionId && (
          <div className="flex justify-between items-center text-[#7E88A8] pt-1">
            <span>Transaction Ref</span>
            <button
              type="button"
              onClick={handleCopyTx}
              className="inline-flex items-center gap-1 font-mono text-white hover:text-[#E8546A] transition-colors"
              title="Copy Transaction ID"
            >
              <span>{transactionId}</span>
              {copied ? (
                <Check className="w-3 h-3 text-[#34D399]" />
              ) : (
                <Copy className="w-3 h-3 text-[#7E88A8]" />
              )}
            </button>
          </div>
        )}
      </div>
    </div>
  );
};
