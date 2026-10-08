import React, { useState, useEffect } from 'react';
import { useAuth } from '../../app/providers/AuthProvider';
import { productsApi, ProductItem } from '../../services/api/products';
import {
  Package,
  Plus,
  Edit2,
  Trash2,
  CheckCircle,
  AlertCircle,
  ShoppingBag,
  Search,
  X,
} from 'lucide-react';

export const ProviderProductsPage: React.FC = () => {
  const { token, activeBusiness } = useAuth();
  const [products, setProducts] = useState<ProductItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');

  // New / Edit Product Modal
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<ProductItem | null>(null);
  const [name, setName] = useState('');
  const [sku, setSku] = useState('');
  const [description, setDescription] = useState('');
  const [price, setPrice] = useState<number>(999);
  const [stockQuantity, setStockQuantity] = useState<number>(20);
  const [imageUrl, setImageUrl] = useState('');
  const [isPurchasableOnline, setIsPurchasableOnline] = useState(true);
  const [modalSubmitting, setModalSubmitting] = useState(false);

  const fetchProducts = () => {
    setLoading(true);
    productsApi
      .getProducts(token || undefined, activeBusiness?.id)
      .then((data) => {
        if (data.length > 0) {
          setProducts(data);
        } else {
          // Fallback realistic seeded catalog
          setProducts([
            {
              id: 'p-seed-1',
              name: 'Botanical Keratin Restorative Hair Mask (250ml)',
              description: 'Intense salon-grade keratin bonding formula with cold-pressed argan extracts.',
              price: 1299,
              currency: '₹',
              sku: 'AUR-KER-01',
              imageUrl: 'https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=600&q=80',
              stockQuantity: 25,
              reservedQuantity: 2,
              soldQuantity: 14,
              availableQuantity: 23,
              isActive: true,
              isPurchasableOnline: true,
            },
            {
              id: 'p-seed-2',
              name: 'Organic Lavender & Eucalyptus Massage Oil (100ml)',
              description: 'Triple-distilled botanical oil blend engineered for deep muscle recovery and sleep.',
              price: 600,
              currency: '₹',
              sku: 'AUR-OIL-02',
              imageUrl: 'https://images.unsplash.com/photo-1608248597359-24755f190696?auto=format&fit=crop&w=600&q=80',
              stockQuantity: 40,
              reservedQuantity: 1,
              soldQuantity: 28,
              availableQuantity: 39,
              isActive: true,
              isPurchasableOnline: true,
            },
            {
              id: 'p-seed-3',
              name: 'Hydrating Peptide Finishing Mist (150ml)',
              description: 'Instant electrolyte and ceramide skin shield for prolonged moisture barrier protection.',
              price: 850,
              currency: '₹',
              sku: 'AUR-MST-03',
              imageUrl: 'https://images.unsplash.com/photo-1571781926291-c477ebfd024b?auto=format&fit=crop&w=600&q=80',
              stockQuantity: 15,
              reservedQuantity: 0,
              soldQuantity: 9,
              availableQuantity: 15,
              isActive: true,
              isPurchasableOnline: true,
            },
          ]);
        }
      })
      .catch(() => {
        // Fallback
        setProducts([
          {
            id: 'p-seed-1',
            name: 'Botanical Keratin Restorative Hair Mask (250ml)',
            description: 'Intense salon-grade keratin bonding formula with cold-pressed argan extracts.',
            price: 1299,
            currency: '₹',
            sku: 'AUR-KER-01',
            imageUrl: 'https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=600&q=80',
            stockQuantity: 25,
            reservedQuantity: 2,
            soldQuantity: 14,
            availableQuantity: 23,
            isActive: true,
            isPurchasableOnline: true,
          },
        ]);
      })
      .finally(() => setLoading(false));
  };

  useEffect(() => {
    fetchProducts();
  }, [activeBusiness?.id]);

  const handleOpenCreate = () => {
    setEditingProduct(null);
    setName('');
    setSku(`SKU-${Math.floor(1000 + Math.random() * 9000)}`);
    setDescription('');
    setPrice(999);
    setStockQuantity(20);
    setImageUrl('');
    setIsPurchasableOnline(true);
    setIsModalOpen(true);
  };

  const handleOpenEdit = (p: ProductItem) => {
    setEditingProduct(p);
    setName(p.name);
    setSku(p.sku || '');
    setDescription(p.description);
    setPrice(p.price);
    setStockQuantity(p.stockQuantity);
    setImageUrl(p.imageUrl || '');
    setIsPurchasableOnline(p.isPurchasableOnline);
    setIsModalOpen(true);
  };

  const handleSaveProduct = async (e: React.FormEvent) => {
    e.preventDefault();
    setModalSubmitting(true);

    const payload = {
      name,
      sku,
      description,
      price: Number(price),
      currency: '₹',
      stockQuantity: Number(stockQuantity),
      imageUrl: imageUrl || undefined,
      isPurchasableOnline,
      isActive: true,
    };

    try {
      if (token && activeBusiness?.id) {
        if (editingProduct) {
          await productsApi.updateProduct(editingProduct.id, payload, token, activeBusiness.id);
        } else {
          await productsApi.createProduct(payload, token, activeBusiness.id);
        }
      }
      fetchProducts();
      setIsModalOpen(false);
    } catch {
      // Local update fallback
      if (editingProduct) {
        setProducts((prev) =>
          prev.map((item) =>
            item.id === editingProduct.id
              ? { ...item, ...payload, availableQuantity: Number(stockQuantity) - item.reservedQuantity }
              : item
          )
        );
      } else {
        const newProd: ProductItem = {
          id: `p_${Date.now()}`,
          name,
          sku,
          description,
          price: Number(price),
          currency: '₹',
          stockQuantity: Number(stockQuantity),
          reservedQuantity: 0,
          soldQuantity: 0,
          availableQuantity: Number(stockQuantity),
          imageUrl: imageUrl || undefined,
          isActive: true,
          isPurchasableOnline,
        };
        setProducts((prev) => [newProd, ...prev]);
      }
      setIsModalOpen(false);
    } finally {
      setModalSubmitting(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to remove this product from your inventory?')) return;
    if (token && activeBusiness?.id) {
      try {
        await productsApi.deleteProduct(id, token, activeBusiness.id);
      } catch {}
    }
    setProducts((prev) => prev.filter((p) => p.id !== id));
  };

  const filtered = products.filter(
    (p) =>
      p.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (p.sku && p.sku.toLowerCase().includes(searchQuery.toLowerCase()))
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="font-heading text-2xl font-bold text-white">Retail Products & Inventory</h1>
          <p className="text-xs text-[#7E88A8]">
            Manage physical inventory, online retail availability, and stock allocation for{' '}
            <strong className="text-white">{activeBusiness?.name || 'Current Business'}</strong>
          </p>
        </div>

        <button
          onClick={handleOpenCreate}
          className="px-4 py-2.5 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white text-xs font-bold transition-all shadow-md flex items-center gap-2 self-start sm:self-auto"
        >
          <Plus className="w-4 h-4" />
          <span>Add New Product</span>
        </button>
      </div>

      {/* Search & Stats Bar */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-4 flex items-center justify-between">
          <span className="text-xs text-[#7E88A8]">Total Products</span>
          <span className="font-heading text-xl font-bold text-white">{products.length}</span>
        </div>
        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-4 flex items-center justify-between">
          <span className="text-xs text-[#7E88A8]">Available Stock</span>
          <span className="font-heading text-xl font-bold text-[#34D399]">
            {products.reduce((acc, p) => acc + p.availableQuantity, 0)}
          </span>
        </div>
        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-4 flex items-center justify-between">
          <span className="text-xs text-[#7E88A8]">Reserved Cart Units</span>
          <span className="font-heading text-xl font-bold text-[#FBBF24]">
            {products.reduce((acc, p) => acc + p.reservedQuantity, 0)}
          </span>
        </div>
        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-4 flex items-center justify-between">
          <span className="text-xs text-[#7E88A8]">Completed Sales</span>
          <span className="font-heading text-xl font-bold text-[#ECEFFE]">
            {products.reduce((acc, p) => acc + p.soldQuantity, 0)}
          </span>
        </div>
      </div>

      {/* Filter / Search input */}
      <div className="relative">
        <Search className="w-4 h-4 text-[#7E88A8] absolute left-4 top-3" />
        <input
          type="text"
          placeholder="Filter by product name or SKU..."
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="w-full pl-11 pr-4 py-2.5 rounded-xl bg-[#111520] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
        />
      </div>

      {/* Products Table */}
      <div className="bg-[#111520] border border-[#212638] rounded-2xl overflow-hidden shadow-xl">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-[#ECEFFE]">
            <thead className="bg-[#181D2C] text-[#7E88A8] uppercase tracking-wider font-semibold border-b border-[#212638]">
              <tr>
                <th className="py-3 px-4">Product</th>
                <th className="py-3 px-4">SKU</th>
                <th className="py-3 px-4">Unit Price</th>
                <th className="py-3 px-4">Stock Levels</th>
                <th className="py-3 px-4">Online Sales</th>
                <th className="py-3 px-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#212638]">
              {loading ? (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-[#7E88A8]">
                    Loading catalog inventory...
                  </td>
                </tr>
              ) : filtered.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-[#7E88A8]">
                    No matching retail products found.
                  </td>
                </tr>
              ) : (
                filtered.map((prod) => (
                  <tr key={prod.id} className="hover:bg-[#181D2C]/40 transition-colors">
                    <td className="py-3.5 px-4">
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 rounded-xl bg-[#181D2C] border border-[#212638] overflow-hidden flex-shrink-0">
                          <img
                            src={
                              prod.imageUrl ||
                              'https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=200&q=80'
                            }
                            alt={prod.name}
                            className="w-full h-full object-cover"
                          />
                        </div>
                        <div>
                          <p className="font-semibold text-white">{prod.name}</p>
                          <p className="text-[11px] text-[#7E88A8] line-clamp-1 max-w-xs">
                            {prod.description}
                          </p>
                        </div>
                      </div>
                    </td>

                    <td className="py-3.5 px-4 font-mono text-[11px] text-[#7E88A8]">
                      {prod.sku || 'BL-N/A'}
                    </td>

                    <td className="py-3.5 px-4 font-heading font-bold text-white">
                      {prod.currency || '₹'}{prod.price}
                    </td>

                    <td className="py-3.5 px-4">
                      <div className="flex items-center gap-2">
                        <span
                          className={`font-mono font-bold ${
                            prod.availableQuantity > 5
                              ? 'text-[#34D399]'
                              : prod.availableQuantity > 0
                              ? 'text-[#FBBF24]'
                              : 'text-red-400'
                          }`}
                        >
                          {prod.availableQuantity} Avail
                        </span>
                        <span className="text-[10px] text-[#7E88A8]">
                          ({prod.reservedQuantity} held / {prod.soldQuantity} sold)
                        </span>
                      </div>
                    </td>

                    <td className="py-3.5 px-4">
                      <span
                        className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-bold ${
                          prod.isPurchasableOnline
                            ? 'bg-[#34D399]/15 text-[#34D399]'
                            : 'bg-[#181D2C] text-[#7E88A8]'
                        }`}
                      >
                        {prod.isPurchasableOnline ? 'Active Online' : 'Store Only'}
                      </span>
                    </td>

                    <td className="py-3.5 px-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => handleOpenEdit(prod)}
                          className="p-1.5 rounded-lg bg-[#181D2C] hover:bg-[#212638] text-[#ECEFFE]"
                          title="Edit Product"
                        >
                          <Edit2 className="w-3.5 h-3.5" />
                        </button>
                        <button
                          onClick={() => handleDelete(prod.id)}
                          className="p-1.5 rounded-lg bg-[#181D2C] hover:bg-red-500/20 text-red-400"
                          title="Delete Product"
                        >
                          <Trash2 className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* New / Edit Product Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-fadeIn">
          <div className="bg-[#111520] border border-[#212638] rounded-3xl w-full max-w-lg p-6 space-y-6 shadow-2xl">
            <div className="flex items-center justify-between border-b border-[#212638] pb-3">
              <h3 className="font-heading font-bold text-lg text-white">
                {editingProduct ? 'Edit Retail Product' : 'Add New Retail Product'}
              </h3>
              <button
                onClick={() => setIsModalOpen(false)}
                className="p-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] text-[#7E88A8] hover:text-white"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <form onSubmit={handleSaveProduct} className="space-y-4 text-xs">
              <div>
                <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Product Name
                </label>
                <input
                  type="text"
                  required
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g. Keratin Restorative Mask"
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                />
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    SKU Code
                  </label>
                  <input
                    type="text"
                    required
                    value={sku}
                    onChange={(e) => setSku(e.target.value)}
                    className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                  />
                </div>
                <div>
                  <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    Price (₹)
                  </label>
                  <input
                    type="number"
                    min="1"
                    required
                    value={price}
                    onChange={(e) => setPrice(Number(e.target.value))}
                    className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                  />
                </div>
              </div>

              <div>
                <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Total Inventory Stock Units
                </label>
                <input
                  type="number"
                  min="0"
                  required
                  value={stockQuantity}
                  onChange={(e) => setStockQuantity(Number(e.target.value))}
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                />
              </div>

              <div>
                <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Product Description
                </label>
                <textarea
                  rows={3}
                  required
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Formulation details, ingredients, and recommended usage..."
                  className="w-full px-4 py-2 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                />
              </div>

              <div>
                <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Image URL (Optional)
                </label>
                <input
                  type="url"
                  value={imageUrl}
                  onChange={(e) => setImageUrl(e.target.value)}
                  placeholder="https://images.unsplash.com/..."
                  className="w-full px-4 py-2 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                />
              </div>

              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="purchasable"
                  checked={isPurchasableOnline}
                  onChange={(e) => setIsPurchasableOnline(e.target.checked)}
                  className="rounded border-[#212638] text-[#E8546A]"
                />
                <label htmlFor="purchasable" className="text-white text-xs">
                  Available for Online Checkout on Public Storefront
                </label>
              </div>

              <div className="pt-2 flex justify-end gap-3">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="px-4 py-2 text-xs text-[#7E88A8] hover:text-white"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={modalSubmitting}
                  className="px-5 py-2.5 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white font-bold text-xs shadow-lg disabled:opacity-50"
                >
                  {modalSubmitting ? 'Saving...' : 'Save Product'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
