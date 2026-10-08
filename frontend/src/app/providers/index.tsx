import React from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from '../query/client';
import { AuthProvider } from './AuthProvider';
import { CartProvider } from './CartContext';

interface AppProvidersProps {
  children: React.ReactNode;
}

export const AppProviders: React.FC<AppProvidersProps> = ({ children }) => {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <CartProvider>
          {children}
        </CartProvider>
      </AuthProvider>
    </QueryClientProvider>
  );
};
