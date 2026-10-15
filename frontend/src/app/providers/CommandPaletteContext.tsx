import React, { createContext, useContext, useState, useEffect, useRef, useCallback } from 'react';
import { useLocation } from 'react-router-dom';
import { useAuth } from './AuthProvider';

interface CommandPaletteContextType {
  isOpen: boolean;
  activeScope: 'customer' | 'provider';
  open: (scope?: 'customer' | 'provider') => void;
  close: () => void;
  toggle: (scope?: 'customer' | 'provider') => void;
  setScope: (scope: 'customer' | 'provider') => void;
}

const CommandPaletteContext = createContext<CommandPaletteContextType | undefined>(undefined);

export const CommandPaletteProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [isOpen, setIsOpen] = useState(false);
  const location = useLocation();
  const { user } = useAuth();
  const previousActiveElement = useRef<HTMLElement | null>(null);

  // Auto-detect default scope based on location and user role
  const isProviderLocation =
    location.pathname.startsWith('/app') ||
    location.pathname.startsWith('/provider') ||
    location.pathname.startsWith('/admin') ||
    user?.role === 'Owner' ||
    user?.role === 'Admin' ||
    user?.role === 'Staff' ||
    user?.role === 'Manager';

  const [activeScope, setActiveScope] = useState<'customer' | 'provider'>(
    isProviderLocation ? 'provider' : 'customer'
  );

  // Update scope default when navigation or user changes, if closed
  useEffect(() => {
    if (!isOpen) {
      setActiveScope(isProviderLocation ? 'provider' : 'customer');
    }
  }, [isProviderLocation, isOpen]);

  const open = useCallback((scope?: 'customer' | 'provider') => {
    previousActiveElement.current = document.activeElement as HTMLElement;
    if (scope) {
      setActiveScope(scope);
    } else {
      setActiveScope(isProviderLocation ? 'provider' : 'customer');
    }
    setIsOpen(true);
  }, [isProviderLocation]);

  const close = useCallback(() => {
    setIsOpen(false);
    if (previousActiveElement.current && typeof previousActiveElement.current.focus === 'function') {
      setTimeout(() => {
        previousActiveElement.current?.focus();
      }, 50);
    }
  }, []);

  const toggle = useCallback((scope?: 'customer' | 'provider') => {
    if (isOpen) {
      close();
    } else {
      open(scope);
    }
  }, [isOpen, open, close]);

  const setScope = useCallback((scope: 'customer' | 'provider') => {
    setActiveScope(scope);
  }, []);

  // Global Keyboard Listener for Ctrl + K and ⌘ + K (macOS)
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && (e.key === 'k' || e.key === 'K')) {
        e.preventDefault();
        toggle();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [toggle]);

  return (
    <CommandPaletteContext.Provider
      value={{
        isOpen,
        activeScope,
        open,
        close,
        toggle,
        setScope,
      }}
    >
      {children}
    </CommandPaletteContext.Provider>
  );
};

export const useCommandPalette = (): CommandPaletteContextType => {
  const context = useContext(CommandPaletteContext);
  if (!context) {
    throw new Error('useCommandPalette must be used within a CommandPaletteProvider');
  }
  return context;
};
