import React from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { queryClient } from '../query/client';
import { AuthProvider } from './AuthProvider';

interface AppProvidersProps {
  children: React.ReactNode;
}

export const AppProviders: React.FC<AppProvidersProps> = ({ children }) => {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        {children}
      </AuthProvider>
    </QueryClientProvider>
  );
};
