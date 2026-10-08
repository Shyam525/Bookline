export interface ProductItem {
  id: string;
  name: string;
  description: string;
  price: number;
  currency: string;
  sku?: string;
  imageUrl?: string;
  stockQuantity: number;
  reservedQuantity: number;
  soldQuantity: number;
  availableQuantity: number;
  isActive: boolean;
  isPurchasableOnline: boolean;
}

const API_BASE = '/api/v1/products';

export const productsApi = {
  async getProducts(token?: string, tenantId?: string): Promise<ProductItem[]> {
    const headers: Record<string, string> = {};
    if (token) headers['Authorization'] = `Bearer ${token}`;
    if (tenantId) headers['X-Tenant-Id'] = tenantId;

    const res = await fetch(API_BASE, { headers });
    if (!res.ok) return [];
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
    if (!res.ok) throw new Error('Failed to create product');
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
    if (!res.ok) throw new Error('Failed to update product');
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
};
