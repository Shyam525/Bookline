import React, { createContext, useContext, useState } from 'react';
import { authApi, AuthUser, AuthResponse } from '../../services/api/auth';

interface AuthContextType {
  user: AuthUser | null;
  tokens: { accessToken: string; refreshToken: string } | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (data: {
    tenantName: string;
    tenantSlug: string;
    ownerEmail: string;
    password: string;
    firstName: string;
    lastName: string;
  }) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const saved = localStorage.getItem('bookline_user');
    return saved ? JSON.parse(saved) : null;
  });

  const [tokens, setTokens] = useState<{ accessToken: string; refreshToken: string } | null>(() => {
    const saved = localStorage.getItem('bookline_tokens');
    return saved ? JSON.parse(saved) : null;
  });

  const [isLoading, setIsLoading] = useState(false);

  const saveAuthSession = (authData: AuthResponse) => {
    const userObj: AuthUser = {
      id: authData.userId,
      tenantId: authData.tenantId,
      email: authData.email,
      firstName: 'Demo',
      lastName: 'Owner',
      role: authData.role,
    };

    const tokenObj = {
      accessToken: authData.accessToken,
      refreshToken: authData.refreshToken,
    };

    setUser(userObj);
    setTokens(tokenObj);
    localStorage.setItem('bookline_user', JSON.stringify(userObj));
    localStorage.setItem('bookline_tokens', JSON.stringify(tokenObj));
  };

  const login = async (email: string, password: string) => {
    setIsLoading(true);
    try {
      const res = await authApi.login(email, password);
      saveAuthSession(res);
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (data: {
    tenantName: string;
    tenantSlug: string;
    ownerEmail: string;
    password: string;
    firstName: string;
    lastName: string;
  }) => {
    setIsLoading(true);
    try {
      const res = await authApi.registerTenant(data);
      saveAuthSession(res);
    } finally {
      setIsLoading(false);
    }
  };

  const logout = () => {
    setUser(null);
    setTokens(null);
    localStorage.removeItem('bookline_user');
    localStorage.removeItem('bookline_tokens');
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        tokens,
        isAuthenticated: !!user && !!tokens,
        isLoading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
