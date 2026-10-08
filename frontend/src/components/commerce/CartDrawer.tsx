import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useCart } from '../../app/providers/CartContext';
import { X, ShoppingBag, Trash2, Plus, Minus, ArrowRight } from 'lucide-react';

export const CartDrawer: React.FC = () => {
  const { items, isOpen, closeCart, removeFromCart, updateQuantity, clearCart, totalCount, subtotal, activeProviderName } = useCart();
  const navigate = useNavigate();

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex justify-end animate-fadeIn">
      {/* Backdrop */}
      <div className="fixed inset-0 bg-black/70 backdrop-blur-sm" onClick={closeCart} />

      {/* Drawer content */}
      <div className="relative w-full max-w-md bg-[#111520] border-l border-[#212638] h-full flex flex-col z-10 shadow-2xl">
        {/* Header */}
        <div className="p-6 border-b border-[#212638] flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-[#E8546A]/10 text-[#E8546A] flex items-center justify-center">
              <ShoppingBag className="w-5 h-5" />
            </div>
            <div>
              <h2 className="font-heading text-lg font-bold text-white">Shopping Cart</h2>
              <p className="text-xs text-[#7E88A8]">
                {activeProviderName ? `From ${activeProviderName}` : `${totalCount} item(s)`}
              </p>
            </div>
          </div>
          <button
            onClick={closeCart}
            className="p-2 text-[#7E88A8] hover:text-white rounded-lg hover:bg-[#181D2C] transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Items List */}
        <div className="flex-1 overflow-y-auto p-6 space-y-4">
          {items.length === 0 ? (
            <div className="text-center py-16">
              <div className="w-16 h-16 rounded-2xl bg-[#181D2C] flex items-center justify-center mx-auto mb-4 text-[#7E88A8]">
                <ShoppingBag className="w-8 h-8" />
              </div>
              <p className="text-base font-medium text-white mb-1">Your cart is empty</p>
              <p className="text-xs text-[#7E88A8] max-w-xs mx-auto">
                Explore local providers and add retail care products to order online.
              </p>
            </div>
          ) : (
            items.map((item) => (
              <div
                key={item.productId}
                className="p-4 rounded-xl bg-[#181D2C] border border-[#212638] flex items-center gap-4"
              >
                <div className="w-16 h-16 rounded-lg bg-[#111520] border border-[#212638] flex items-center justify-center flex-shrink-0 text-[#E8546A] font-heading font-bold text-lg">
                  {item.productName.substring(0, 2).toUpperCase()}
                </div>
                <div className="flex-1 min-w-0">
                  <h4 className="text-sm font-semibold text-white truncate">{item.productName}</h4>
                  <p className="text-xs text-[#E8546A] font-bold mt-0.5">${item.unitPrice.toFixed(2)}</p>
                  <p className="text-[10px] text-[#7E88A8]">{item.maxStock} in stock</p>
                </div>
                <div className="flex flex-col items-end gap-2">
                  <button
                    onClick={() => removeFromCart(item.productId)}
                    className="text-[#7E88A8] hover:text-red-400 p-1"
                    title="Remove item"
                  >
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                  <div className="flex items-center gap-1.5 bg-[#111520] border border-[#212638] rounded-lg px-2 py-1">
                    <button
                      onClick={() => updateQuantity(item.productId, item.quantity - 1)}
                      className="text-[#7E88A8] hover:text-white"
                    >
                      <Minus className="w-3 h-3" />
                    </button>
                    <span className="text-xs font-bold text-white px-1">{item.quantity}</span>
                    <button
                      onClick={() => updateQuantity(item.productId, item.quantity + 1)}
                      disabled={item.quantity >= item.maxStock}
                      className="text-[#7E88A8] hover:text-white disabled:opacity-40"
                    >
                      <Plus className="w-3 h-3" />
                    </button>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>

        {/* Footer */}
        {items.length > 0 && (
          <div className="p-6 border-t border-[#212638] bg-[#0E121B] space-y-4">
            <div className="space-y-1.5">
              <div className="flex justify-between text-xs text-[#7E88A8]">
                <span>Subtotal</span>
                <span>${subtotal.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-xs text-[#7E88A8]">
                <span>Estimated Tax (5%)</span>
                <span>${(subtotal * 0.05).toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-sm font-bold text-white pt-2 border-t border-[#212638]">
                <span>Total</span>
                <span className="text-[#34D399]">${(subtotal * 1.05).toFixed(2)}</span>
              </div>
            </div>

            <div className="flex gap-2">
              <button
                onClick={clearCart}
                className="px-3 py-2.5 text-xs font-semibold text-[#7E88A8] hover:text-white border border-[#212638] rounded-xl hover:bg-[#181D2C] transition-colors"
              >
                Clear
              </button>
              <button
                onClick={() => {
                  closeCart();
                  navigate('/checkout');
                }}
                className="flex-1 py-3 px-4 bg-[#E8546A] hover:bg-[#D44359] text-white text-sm font-bold rounded-xl flex items-center justify-center gap-2 shadow-lg shadow-[#E8546A]/20 transition-all hover:translate-y-[-1px]"
              >
                Checkout <ArrowRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
