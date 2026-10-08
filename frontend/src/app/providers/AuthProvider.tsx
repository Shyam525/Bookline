import React, { createContext, useContext, useState, useEffect } from 'react';
import { authApi, AuthUser, AuthResponse, BusinessSummary } from '../../services/api/auth';

interface AuthContextType {
  user: AuthUser | null;
  tokens: { accessToken: string; refreshToken: string } | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  activeTenantId: string;
  businesses: BusinessSummary[];
  activeBusiness: BusinessSummary | null;
  login: (email: string, password: string) => Promise<void>;
  register: (data: any) => Promise<void>;
  registerCustomer: (data: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    phone?: string;
  }) => Promise<void>;
  registerTenant: (data: {
    tenantName: string;
    tenantSlug: string;
    ownerEmail: string;
    password: string;
    firstName: string;
    lastName: string;
  }) => Promise<void>;
  switchBusiness: (tenantId: string) => Promise<void>;
  quickLoginAs: (role: 'customer' | 'provider' | 'admin') => Promise<void>;
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

  const [activeTenantId, setActiveTenantId] = useState<string>(() => {
    const savedTenant = localStorage.getItem('bookline_active_tenant');
    if (savedTenant) return savedTenant;
    const savedUser = localStorage.getItem('bookline_user');
    return savedUser ? JSON.parse(savedUser).tenantId || '' : '';
  });

  const [businesses, setBusinesses] = useState<BusinessSummary[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  const saveAuthSession = (authData: AuthResponse) => {
    const userObj: AuthUser = {
      id: authData.userId,
      tenantId: authData.tenantId,
      email: authData.email,
      firstName: authData.email.split('@')[0],
      lastName: '',
      role: authData.role,
    };

    const tokenObj = {
      accessToken: authData.accessToken,
      refreshToken: authData.refreshToken,
    };

    setUser(userObj);
    setTokens(tokenObj);
    setActiveTenantId(authData.tenantId);
    localStorage.setItem('bookline_user', JSON.stringify(userObj));
    localStorage.setItem('bookline_tokens', JSON.stringify(tokenObj));
    localStorage.setItem('bookline_active_tenant', authData.tenantId);
  };

  const refreshBusinesses = async (token: string) => {
    try {
      const list = await authApi.getMyBusinesses(token);
      setBusinesses(list);
    } catch {
      setBusinesses([]);
    }
  };

  useEffect(() => {
    if (tokens?.accessToken && (user?.role === 'Owner' || user?.role === 'Admin' || user?.role === 'Staff')) {
      refreshBusinesses(tokens.accessToken);
    }
  }, [tokens?.accessToken, user?.role]);

  const login = async (email: string, password: string) => {
    setIsLoading(true);
    try {
      const res = await authApi.login(email, password);
      saveAuthSession(res);
      if (res.role !== 'Customer') {
        await refreshBusinesses(res.accessToken);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const registerCustomer = async (data: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    phone?: string;
  }) => {
    setIsLoading(true);
    try {
      const res = await authApi.registerCustomer(data);
      saveAuthSession(res);
    } finally {
      setIsLoading(false);
    }
  };

  const registerTenant = async (data: {
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
      await refreshBusinesses(res.accessToken);
    } finally {
      setIsLoading(false);
    }
  };

  const switchBusiness = async (tenantId: string) => {
    if (!tokens?.accessToken) return;
    setIsLoading(true);
    try {
      const res = await authApi.switchBusiness(tenantId, tokens.accessToken);
      saveAuthSession(res);
    } finally {
      setIsLoading(false);
    }
  };

  const quickLoginAs = async (role: 'customer' | 'provider' | 'admin') => {
    const creds = {
      customer: { email: 'customer@bookline.local', pass: 'Customer123!' },
      provider: { email: 'provider@bookline.local', pass: 'Provider123!' },
      admin: { email: 'admin@bookline.local', pass: 'Admin123!' },
    }[role];

    await login(creds.email, creds.pass);
  };

  const logout = () => {
    setUser(null);
    setTokens(null);
    setActiveTenantId('');
    setBusinesses([]);
    localStorage.removeItem('bookline_user');
    localStorage.removeItem('bookline_tokens');
    localStorage.removeItem('bookline_active_tenant');
  };

  const activeBusiness = businesses.find((b) => b.id === activeTenantId) || businesses[0] || null;

  return (
    <AuthContext.Provider
      value={{
        user,
        tokens,
        token: tokens?.accessToken || null,
        isAuthenticated: !!user && !!tokens,
        isLoading,
        activeTenantId,
        businesses,
        activeBusiness,
        login,
        register: registerTenant,
        registerCustomer,
        registerTenant,
        switchBusiness,
        quickLoginAs,
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
