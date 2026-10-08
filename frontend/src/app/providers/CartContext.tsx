import React, { createContext, useContext, useState, useEffect } from 'react';

export interface CartItem {
  productId: string;
  productName: string;
  name?: string;
  unitPrice: number;
  price?: number;
  currency?: string;
  quantity: number;
  maxStock: number;
  imageUrl?: string;
  tenantId: string;
  providerName: string;
}

interface CartContextType {
  items: CartItem[];
  isOpen: boolean;
  activeTenantId: string | null;
  activeProviderName: string | null;
  openCart: () => void;
  closeCart: () => void;
  toggleCart: () => void;
  addToCart: (item: Omit<CartItem, 'quantity'>, quantity?: number) => void;
  addItem: (item: any, quantity?: number) => void;
  removeFromCart: (productId: string) => void;
  updateQuantity: (productId: string, quantity: number) => void;
  clearCart: () => void;
  totalCount: number;
  subtotal: number;
}

const CartContext = createContext<CartContextType | undefined>(undefined);

export const CartProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [items, setItems] = useState<CartItem[]>(() => {
    const saved = localStorage.getItem('bookline_cart');
    return saved ? JSON.parse(saved) : [];
  });
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    localStorage.setItem('bookline_cart', JSON.stringify(items));
  }, [items]);

  const activeTenantId = items.length > 0 ? items[0].tenantId : null;
  const activeProviderName = items.length > 0 ? items[0].providerName : null;

  const addToCart = (item: Omit<CartItem, 'quantity'>, quantity = 1) => {
    setItems((prev) => {
      // If adding from a different provider, reset cart for single-provider fulfillment (Point 50)
      if (prev.length > 0 && prev[0].tenantId !== item.tenantId) {
        if (!confirm(`Your cart currently contains items from ${prev[0].providerName}. Clear cart to add products from ${item.providerName}?`)) {
          return prev;
        }
        return [{ ...item, quantity: Math.min(quantity, item.maxStock) }];
      }

      const existingIndex = prev.findIndex((i) => i.productId === item.productId);
      if (existingIndex > -1) {
        const updated = [...prev];
        const newQty = Math.min(updated[existingIndex].quantity + quantity, item.maxStock);
        updated[existingIndex] = { ...updated[existingIndex], quantity: newQty };
        return updated;
      }

      return [...prev, { ...item, quantity: Math.min(quantity, item.maxStock) }];
    });
    setIsOpen(true);
  };

  const addItem = (item: any, quantity = 1) => {
    addToCart({
      productId: item.productId,
      productName: item.productName || item.name || 'Product',
      name: item.name || item.productName,
      unitPrice: item.unitPrice ?? item.price ?? 0,
      price: item.price ?? item.unitPrice,
      currency: item.currency || '₹',
      maxStock: item.maxStock || 10,
      imageUrl: item.imageUrl,
      tenantId: item.tenantId,
      providerName: item.providerName,
    }, quantity);
  };

  const removeFromCart = (productId: string) => {
    setItems((prev) => prev.filter((i) => i.productId !== productId));
  };

  const updateQuantity = (productId: string, quantity: number) => {
    if (quantity <= 0) {
      removeFromCart(productId);
      return;
    }
    setItems((prev) =>
      prev.map((i) => (i.productId === productId ? { ...i, quantity: Math.min(quantity, i.maxStock) } : i))
    );
  };

  const clearCart = () => {
    setItems([]);
  };

  const totalCount = items.reduce((sum, item) => sum + item.quantity, 0);
  const subtotal = items.reduce((sum, item) => sum + (item.unitPrice || item.price || 0) * item.quantity, 0);

  return (
    <CartContext.Provider
      value={{
        items,
        isOpen,
        activeTenantId,
        activeProviderName,
        openCart: () => setIsOpen(true),
        closeCart: () => setIsOpen(false),
        toggleCart: () => setIsOpen((prev) => !prev),
        addToCart,
        addItem,
        removeFromCart,
        updateQuantity,
        clearCart,
        totalCount,
        subtotal,
      }}
    >
      {children}
    </CartContext.Provider>
  );
};

export const useCart = (): CartContextType => {
  const context = useContext(CartContext);
  if (!context) throw new Error('useCart must be used within CartProvider');
  return context;
};
