export interface ProductImageItem {
  id?: string;
  productId?: string;
  url: string;
  altText?: string;
  sortOrder?: number;
  isPrimary?: boolean;
}

export interface ProductCategoryItem {
  id: string;
  businessId?: string;
  name: string;
  slug: string;
  description?: string;
  sortOrder: number;
  isActive: boolean;
  productsCount?: number;
}

export interface ProductItem {
  id: string;
  businessId?: string;
  name: string;
  slug?: string;
  description: string;
  price: number;
  currency: string;
  sku?: string;
  categoryId?: string;
  categoryName?: string;
  images?: ProductImageItem[];
  imageUrl?: string;
  trackInventory?: boolean;
  stockQuantity: number;
  reservedQuantity: number;
  soldQuantity: number;
  availableQuantity: number;
  lowStockThreshold?: number;
  allowBackorders?: boolean;
  isInStock?: boolean;
  isLowStock?: boolean;
  isActive: boolean;
  isPurchasableOnline: boolean;
  isPublished?: boolean;
  inventoryStatus?: 'InStock' | 'LowStock' | 'OutOfStock' | 'Untracked' | string;
  commercialStatus?: 'ActiveInStock' | 'ActiveOutOfStock' | 'Inactive' | 'StoreOnly' | 'Draft' | string;
  createdAtUtc?: string;
  updatedAtUtc?: string;
}

export interface ProductFilterParams {
  categoryId?: string;
  status?: 'all' | 'in_stock' | 'low_stock' | 'out_of_stock' | 'store_only' | 'draft' | 'inactive' | string;
  includeInactive?: boolean;
  search?: string;
}

const API_BASE = '/api/v1/products';

export const productsApi = {
  async getProducts(
    token?: string,
    tenantId?: string,
    filters?: ProductFilterParams
  ): Promise<ProductItem[]> {
    const headers: Record<string, string> = {};
    if (token) headers['Authorization'] = `Bearer ${token}`;
    if (tenantId) headers['X-Tenant-Id'] = tenantId;

    const params = new URLSearchParams();
    if (filters?.categoryId) params.append('categoryId', filters.categoryId);
    if (filters?.status) params.append('status', filters.status);
    if (filters?.includeInactive) params.append('includeInactive', 'true');
    if (filters?.search) params.append('search', filters.search);

    const qs = params.toString();
    const url = qs ? `${API_BASE}?${qs}` : API_BASE;

    const res = await fetch(url, { headers });
    if (!res.ok) return [];
    return res.json();
  },

  async getProductById(id: string, token?: string, tenantId?: string): Promise<ProductItem | null> {
    const headers: Record<string, string> = {};
    if (token) headers['Authorization'] = `Bearer ${token}`;
    if (tenantId) headers['X-Tenant-Id'] = tenantId;

    const res = await fetch(`${API_BASE}/${id}`, { headers });
    if (!res.ok) return null;
    return res.json();
  },

  async createProduct(data: Partial<ProductItem>, token: string, tenantId: string): Promise<ProductItem> {
    const res = await fetch(API_BASE, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Failed to create product');
    }
    return res.json();
  },

  async updateProduct(id: string, data: Partial<ProductItem>, token: string, tenantId: string): Promise<ProductItem> {
    const res = await fetch(`${API_BASE}/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Failed to update product');
    }
    return res.json();
  },

  async deleteProduct(id: string, token: string, tenantId: string): Promise<void> {
    const res = await fetch(`${API_BASE}/${id}`, {
      method: 'DELETE',
      headers: {
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
    });
    if (!res.ok) throw new Error('Failed to delete product');
  },

  async adjustInventory(
    id: string,
    newStockQuantity: number,
    reason?: string,
    token?: string,
    tenantId?: string
  ): Promise<ProductItem> {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };
    if (token) headers['Authorization'] = `Bearer ${token}`;
    if (tenantId) headers['X-Tenant-Id'] = tenantId;

    const res = await fetch(`${API_BASE}/${id}/inventory`, {
      method: 'PATCH',
      headers,
      body: JSON.stringify({ newStockQuantity, reason }),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Failed to adjust inventory');
    }
    return res.json();
  },

  async setProductStatus(
    id: string,
    statusPayload: { isActive?: boolean; isPublished?: boolean; isPurchasableOnline?: boolean },
    token?: string,
    tenantId?: string
  ): Promise<ProductItem> {
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
    };
    if (token) headers['Authorization'] = `Bearer ${token}`;
    if (tenantId) headers['X-Tenant-Id'] = tenantId;

    const res = await fetch(`${API_BASE}/${id}/status`, {
      method: 'PATCH',
      headers,
      body: JSON.stringify(statusPayload),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Failed to update product status');
    }
    return res.json();
  },

  // --- Category APIs ---

  async getCategories(token?: string, tenantId?: string): Promise<ProductCategoryItem[]> {
    const headers: Record<string, string> = {};
    if (token) headers['Authorization'] = `Bearer ${token}`;
    if (tenantId) headers['X-Tenant-Id'] = tenantId;

    const res = await fetch(`${API_BASE}/categories`, { headers });
    if (!res.ok) return [];
    return res.json();
  },

  async createCategory(
    data: { name: string; slug?: string; description?: string; sortOrder?: number },
    token: string,
    tenantId: string
  ): Promise<ProductCategoryItem> {
    const res = await fetch(`${API_BASE}/categories`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
        'X-Tenant-Id': tenantId,
      },
      body: JSON.stringify(data),
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({}));
      throw new Error(err.message || 'Failed to create product category');
    }
    return res.json();
  },
};
