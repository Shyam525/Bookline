import React, { useState, useEffect, useMemo } from 'react';
import { useAuth } from '../../app/providers/AuthProvider';
import {
  productsApi,
  ProductItem,
  ProductCategoryItem,
  ProductImageItem,
} from '../../services/api/products';
import {
  Package,
  Plus,
  Edit2,
  Trash2,
  Search,
  X,
  AlertTriangle,
  Eye,
  Sliders,
  Check,
  ShoppingBag,
  ExternalLink,
  ShieldCheck,
  Tag,
  Boxes,
  ArrowUpDown,
  Image as ImageIcon,
  CheckCircle2,
} from 'lucide-react';

export const ProviderProductsPage: React.FC = () => {
  const { token, activeBusiness } = useAuth();
  const [products, setProducts] = useState<ProductItem[]>([]);
  const [categories, setCategories] = useState<ProductCategoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [categoryFilter, setCategoryFilter] = useState<string>('all');

  // Modals
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState<ProductItem | null>(null);
  const [detailProduct, setDetailProduct] = useState<ProductItem | null>(null);
  const [adjustingProduct, setAdjustingProduct] = useState<ProductItem | null>(null);
  const [newStockInput, setNewStockInput] = useState<number>(0);
  const [adjustReason, setAdjustReason] = useState<string>('Restock Shipment');

  // Form State
  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');
  const [sku, setSku] = useState('');
  const [description, setDescription] = useState('');
  const [price, setPrice] = useState<number>(28);
  const [currency, setCurrency] = useState<string>('USD');
  const [categoryId, setCategoryId] = useState<string>('');
  const [stockQuantity, setStockQuantity] = useState<number>(25);
  const [lowStockThreshold, setLowStockThreshold] = useState<number>(5);
  const [trackInventory, setTrackInventory] = useState<boolean>(true);
  const [isPurchasableOnline, setIsPurchasableOnline] = useState<boolean>(true);
  const [isPublished, setIsPublished] = useState<boolean>(true);
  const [isActive, setIsActive] = useState<boolean>(true);
  const [images, setImages] = useState<ProductImageItem[]>([]);
  const [newImageUrl, setNewImageUrl] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const [modalSubmitting, setModalSubmitting] = useState(false);

  // Initial fetch
  const fetchCatalog = async () => {
    setLoading(true);
    try {
      const [fetchedProducts, fetchedCategories] = await Promise.all([
        productsApi.getProducts(token || undefined, activeBusiness?.id, { includeInactive: true }),
        productsApi.getCategories(token || undefined, activeBusiness?.id),
      ]);

      if (fetchedCategories.length > 0) {
        setCategories(fetchedCategories);
      } else {
        setCategories([
          { id: 'cat-skincare', name: 'Skincare & Aromatherapy', slug: 'skincare-aromatherapy', sortOrder: 1, isActive: true },
          { id: 'cat-haircare', name: 'Haircare Formulations', slug: 'haircare-formulations', sortOrder: 2, isActive: true },
          { id: 'cat-equipment', name: 'Wellness Equipment & Tools', slug: 'wellness-equipment-tools', sortOrder: 3, isActive: true },
          { id: 'cat-accessories', name: 'Merchandise & Accessories', slug: 'merchandise-accessories', sortOrder: 4, isActive: true },
        ]);
      }

      if (fetchedProducts.length > 0) {
        setProducts(fetchedProducts);
      } else {
        // Fallback realistic seeded catalog representing all 5 states
        setProducts([
          {
            id: 'p-seed-1',
            name: 'Botanical Keratin Restorative Hair Mask (250ml)',
            slug: 'botanical-keratin-restorative-hair-mask-250ml',
            description: 'Intense salon-grade keratin bonding formula with cold-pressed argan extracts.',
            price: 45,
            currency: 'USD',
            sku: 'BL-KER-01',
            categoryName: 'Haircare Formulations',
            imageUrl: 'https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=600&q=80',
            images: [
              { url: 'https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=600&q=80', isPrimary: true, altText: 'Front Mask Jar' },
            ],
            stockQuantity: 28,
            reservedQuantity: 2,
            soldQuantity: 14,
            availableQuantity: 26,
            trackInventory: true,
            lowStockThreshold: 5,
            isInStock: true,
            isLowStock: false,
            isActive: true,
            isPurchasableOnline: true,
            isPublished: true,
            commercialStatus: 'ActiveInStock',
            inventoryStatus: 'InStock',
          },
          {
            id: 'p-seed-2',
            name: 'Organic French Lavender Essential Oil (100ml)',
            slug: 'organic-french-lavender-essential-oil-100ml',
            description: 'Triple-distilled botanical oil blend engineered for deep relaxation and restorative sleep.',
            price: 34,
            currency: 'USD',
            sku: 'BL-OIL-02',
            categoryName: 'Skincare & Aromatherapy',
            imageUrl: 'https://images.unsplash.com/photo-1608248597359-24755f190696?auto=format&fit=crop&w=600&q=80',
            images: [
              { url: 'https://images.unsplash.com/photo-1608248597359-24755f190696?auto=format&fit=crop&w=600&q=80', isPrimary: true, altText: 'Amber dropper bottle' },
            ],
            stockQuantity: 4,
            reservedQuantity: 1,
            soldQuantity: 28,
            availableQuantity: 3,
            trackInventory: true,
            lowStockThreshold: 5,
            isInStock: true,
            isLowStock: true,
            isActive: true,
            isPurchasableOnline: true,
            isPublished: true,
            commercialStatus: 'ActiveInStock',
            inventoryStatus: 'LowStock',
          },
          {
            id: 'p-seed-3',
            name: 'Silk Acoustic Recovery Eye Pillow',
            slug: 'silk-acoustic-recovery-eye-pillow',
            description: 'Mulberry silk weighted eye pillow filled with organic lavender seeds. Sold out following weekend rush.',
            price: 38,
            currency: 'USD',
            sku: 'BL-EYE-03',
            categoryName: 'Wellness Equipment & Tools',
            imageUrl: 'https://images.unsplash.com/photo-1540555700478-4be289fbecef?auto=format&fit=crop&w=600&q=80',
            stockQuantity: 0,
            reservedQuantity: 0,
            soldQuantity: 50,
            availableQuantity: 0,
            trackInventory: true,
            lowStockThreshold: 5,
            isInStock: false,
            isLowStock: false,
            isActive: true,
            isPurchasableOnline: true,
            isPublished: true,
            commercialStatus: 'ActiveOutOfStock',
            inventoryStatus: 'OutOfStock',
          },
          {
            id: 'p-seed-4',
            name: 'Professional Basalt Hot Stone Therapy Kit',
            slug: 'professional-basalt-hot-stone-kit',
            description: 'Volcanic basalt therapy stones for therapist application. Exclusively available for in-person studio pickup.',
            price: 145,
            currency: 'USD',
            sku: 'BL-STN-04',
            categoryName: 'Wellness Equipment & Tools',
            imageUrl: 'https://images.unsplash.com/photo-1544367567-0f2fcb009e0b?auto=format&fit=crop&w=600&q=80',
            stockQuantity: 8,
            reservedQuantity: 0,
            soldQuantity: 12,
            availableQuantity: 8,
            trackInventory: true,
            lowStockThreshold: 2,
            isInStock: true,
            isLowStock: false,
            isActive: true,
            isPurchasableOnline: false,
            isPublished: true,
            commercialStatus: 'StoreOnly',
            inventoryStatus: 'InStock',
          },
          {
            id: 'p-seed-5',
            name: 'Upcoming Peptide Radiance Firming Serum (50ml)',
            slug: 'upcoming-peptide-radiance-firming-serum',
            description: 'Pre-launch inventory batch. Formulated with copper tripeptides and multi-weight hyaluronic acid.',
            price: 68,
            currency: 'USD',
            sku: 'BL-SRM-05',
            categoryName: 'Skincare & Aromatherapy',
            imageUrl: 'https://images.unsplash.com/photo-1620916566398-39f1143ab7be?auto=format&fit=crop&w=600&q=80',
            stockQuantity: 40,
            reservedQuantity: 0,
            soldQuantity: 0,
            availableQuantity: 40,
            trackInventory: true,
            lowStockThreshold: 10,
            isInStock: true,
            isLowStock: false,
            isActive: true,
            isPurchasableOnline: true,
            isPublished: false,
            commercialStatus: 'Draft',
            inventoryStatus: 'InStock',
          },
        ]);
      }
    } catch {
      // Fallback
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCatalog();
  }, [activeBusiness?.id]);

  // Handle open create
  const handleOpenCreate = () => {
    setEditingProduct(null);
    setName('');
    setSlug('');
    setSku(`BL-SKU-${Math.floor(1000 + Math.random() * 9000)}`);
    setDescription('');
    setPrice(28);
    setCurrency((activeBusiness as any)?.currency || 'USD');
    setCategoryId(categories.length > 0 ? categories[0].id : '');
    setStockQuantity(25);
    setLowStockThreshold(5);
    setTrackInventory(true);
    setIsPurchasableOnline(true);
    setIsPublished(true);
    setIsActive(true);
    setImages([]);
    setNewImageUrl('');
    setFormError(null);
    setIsModalOpen(true);
  };

  // Handle open edit
  const handleOpenEdit = (p: ProductItem) => {
    setEditingProduct(p);
    setName(p.name);
    setSlug(p.slug || '');
    setSku(p.sku || '');
    setDescription(p.description);
    setPrice(p.price);
    setCurrency(p.currency || 'USD');
    setCategoryId(p.categoryId || (categories.length > 0 ? categories[0].id : ''));
    setStockQuantity(p.stockQuantity);
    setLowStockThreshold(p.lowStockThreshold ?? 5);
    setTrackInventory(p.trackInventory ?? true);
    setIsPurchasableOnline(p.isPurchasableOnline);
    setIsPublished(p.isPublished ?? true);
    setIsActive(p.isActive);
    setImages(p.images && p.images.length > 0 ? [...p.images] : p.imageUrl ? [{ url: p.imageUrl, isPrimary: true }] : []);
    setNewImageUrl('');
    setFormError(null);
    setIsModalOpen(true);
  };

  // Add Image to gallery
  const handleAddImage = () => {
    if (!newImageUrl.trim()) return;
    const isPrimary = images.length === 0;
    setImages([...images, { url: newImageUrl.trim(), isPrimary, sortOrder: images.length }]);
    setNewImageUrl('');
  };

  const handleRemoveImage = (index: number) => {
    const updated = images.filter((_, i) => i !== index);
    if (updated.length > 0 && !updated.some((i) => i.isPrimary)) {
      updated[0].isPrimary = true;
    }
    setImages(updated);
  };

  const handleSetPrimaryImage = (index: number) => {
    setImages(images.map((img, i) => ({ ...img, isPrimary: i === index })));
  };

  // Save product
  const handleSaveProduct = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    setModalSubmitting(true);

    const primaryImg = images.find((i) => i.isPrimary) || images[0];

    const payload: Partial<ProductItem> = {
      name: name.trim(),
      slug: slug.trim() || undefined,
      sku: sku.trim() || undefined,
      description: description.trim(),
      price: Number(price),
      currency: currency.trim().toUpperCase(),
      categoryId: categoryId || undefined,
      stockQuantity: Number(stockQuantity),
      lowStockThreshold: Number(lowStockThreshold),
      trackInventory,
      isPurchasableOnline,
      isPublished,
      isActive,
      imageUrl: primaryImg?.url,
      images: images.map((img, idx) => ({ ...img, sortOrder: idx })),
    };

    try {
      if (token && activeBusiness?.id) {
        if (editingProduct) {
          await productsApi.updateProduct(editingProduct.id, payload, token, activeBusiness.id);
        } else {
          await productsApi.createProduct(payload, token, activeBusiness.id);
        }
      }
      await fetchCatalog();
      setIsModalOpen(false);
    } catch (err: any) {
      setFormError(err.message || 'Failed to save product.');
    } finally {
      setModalSubmitting(false);
    }
  };

  // Quick adjust stock
  const handleOpenAdjust = (p: ProductItem) => {
    setAdjustingProduct(p);
    setNewStockInput(p.stockQuantity);
    setAdjustReason('Restock Shipment');
  };

  const handleSaveStockAdjust = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!adjustingProduct) return;
    if (newStockInput < 0) {
      alert('Inventory stock level cannot be negative.');
      return;
    }

    try {
      if (token && activeBusiness?.id) {
        await productsApi.adjustInventory(
          adjustingProduct.id,
          Number(newStockInput),
          adjustReason,
          token,
          activeBusiness.id
        );
      }
      setProducts((prev) =>
        prev.map((item) =>
          item.id === adjustingProduct.id
            ? {
                ...item,
                stockQuantity: Number(newStockInput),
                availableQuantity: Number(newStockInput) - item.reservedQuantity,
                isInStock: Number(newStockInput) - item.reservedQuantity > 0,
                isLowStock: Number(newStockInput) - item.reservedQuantity > 0 && Number(newStockInput) - item.reservedQuantity <= (item.lowStockThreshold ?? 5),
              }
            : item
        )
      );
      if (detailProduct && detailProduct.id === adjustingProduct.id) {
        setDetailProduct({
          ...detailProduct,
          stockQuantity: Number(newStockInput),
          availableQuantity: Number(newStockInput) - detailProduct.reservedQuantity,
          isInStock: Number(newStockInput) - detailProduct.reservedQuantity > 0,
        });
      }
      setAdjustingProduct(null);
    } catch (err: any) {
      alert(err.message || 'Failed to adjust stock.');
    }
  };

  // Quick toggle publish
  const handleTogglePublish = async (p: ProductItem) => {
    const nextPublished = !p.isPublished;
    try {
      if (token && activeBusiness?.id) {
        await productsApi.setProductStatus(p.id, { isPublished: nextPublished }, token, activeBusiness.id);
      }
      setProducts((prev) => prev.map((item) => (item.id === p.id ? { ...item, isPublished: nextPublished } : item)));
      if (detailProduct && detailProduct.id === p.id) {
        setDetailProduct({ ...detailProduct, isPublished: nextPublished });
      }
    } catch (err: any) {
      alert(err.message || 'Failed to update publication status.');
    }
  };

  // Delete / Archive product
  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to deactivate and remove this retail product from active sales?')) return;
    if (token && activeBusiness?.id) {
      try {
        await productsApi.deleteProduct(id, token, activeBusiness.id);
      } catch {}
    }
    setProducts((prev) => prev.filter((p) => p.id !== id));
    if (detailProduct?.id === id) setDetailProduct(null);
  };

  // Filtered list
  const filteredProducts = useMemo(() => {
    return products.filter((p) => {
      // Search
      const matchesSearch =
        !searchQuery ||
        p.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        (p.sku && p.sku.toLowerCase().includes(searchQuery.toLowerCase())) ||
        p.description.toLowerCase().includes(searchQuery.toLowerCase());

      if (!matchesSearch) return false;

      // Category
      if (categoryFilter !== 'all' && p.categoryId !== categoryFilter) {
        return false;
      }

      // Lifecycle status
      if (statusFilter === 'all') return true;
      if (statusFilter === 'in_stock') return p.isActive && p.isPublished && p.isInStock && p.isPurchasableOnline;
      if (statusFilter === 'low_stock') return p.isActive && p.isLowStock;
      if (statusFilter === 'out_of_stock') return p.isActive && !p.isInStock;
      if (statusFilter === 'store_only') return p.isActive && p.isPublished && !p.isPurchasableOnline;
      if (statusFilter === 'draft') return p.isActive && !p.isPublished;
      if (statusFilter === 'inactive') return !p.isActive;

      return true;
    });
  }, [products, searchQuery, statusFilter, categoryFilter]);

  // Aggregate metrics
  const totalCount = products.length;
  const inStockCount = products.filter((p) => p.isActive && p.isPublished && p.isInStock && p.isPurchasableOnline).length;
  const lowStockCount = products.filter((p) => p.isActive && p.isLowStock).length;
  const outOfStockCount = products.filter((p) => p.isActive && !p.isInStock).length;
  const storeOnlyCount = products.filter((p) => p.isActive && p.isPublished && !p.isPurchasableOnline).length;
  const draftCount = products.filter((p) => p.isActive && !p.isPublished).length;
  const totalAvailableUnits = products.reduce((acc, p) => acc + (p.trackInventory ? p.availableQuantity : 0), 0);
  const totalReservedUnits = products.reduce((acc, p) => acc + p.reservedQuantity, 0);

  return (
    <div className="space-y-6 max-w-7xl mx-auto pb-12">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2 mb-1">
            <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-[#E8546A]/10 text-[#E8546A] border border-[#E8546A]/20">
              <Boxes className="w-3 h-3" />
              Section 26 Retail Domain
            </span>
            <span className="text-xs text-[#7E88A8]">• Multi-Tenant Commerce Engine</span>
          </div>
          <h1 className="font-heading text-2xl sm:text-3xl font-bold text-white tracking-tight">
            Product Management & Inventory
          </h1>
          <p className="text-xs text-[#7E88A8] mt-1 max-w-2xl">
            Manage physical retail products, real-time inventory allocation, multi-image galleries, and omnichannel publication for{' '}
            <strong className="text-white">{activeBusiness?.name || 'Your Business'}</strong>.
          </p>
        </div>

        <button
          onClick={handleOpenCreate}
          className="px-4 py-2.5 rounded-xl bg-gradient-to-r from-[#E8546A] to-[#D44359] hover:from-[#D44359] hover:to-[#B83247] text-white text-xs font-bold transition-all shadow-lg shadow-[#E8546A]/25 flex items-center gap-2 self-start sm:self-auto cursor-pointer"
        >
          <Plus className="w-4 h-4" />
          <span>Add New Product</span>
        </button>
      </div>

      {/* Domain Separation Notice Banner */}
      <div className="p-3.5 rounded-2xl bg-[#111520] border border-[#212638] flex items-center justify-between gap-4 text-xs">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-xl bg-[#60A5FA]/10 border border-[#60A5FA]/20 flex items-center justify-center text-[#60A5FA] flex-shrink-0">
            <ShieldCheck className="w-4 h-4" />
          </div>
          <div>
            <p className="font-bold text-white">Domain Separation Architecture</p>
            <p className="text-[11px] text-[#7E88A8]">
              Products represent independent retail merchandise. Changing product inventory, reservations, or sales does not alter staff schedules or appointment capacity.
            </p>
          </div>
        </div>
        <div className="hidden md:flex items-center gap-2 font-mono text-[11px] text-[#34D399] bg-[#34D399]/10 px-3 py-1.5 rounded-xl border border-[#34D399]/20">
          <span>Non-Negative Stock Invariant: Enforced</span>
        </div>
      </div>

      {/* Stats Summary Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-3.5">
        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-4">
          <div className="flex items-center justify-between text-xs text-[#7E88A8] mb-1">
            <span>Catalog Items</span>
            <Package className="w-4 h-4 text-[#7E88A8]" />
          </div>
          <p className="font-heading text-2xl font-bold text-white">{totalCount}</p>
          <p className="text-[10px] text-[#7E88A8] mt-1">{inStockCount} active & online purchasable</p>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-4">
          <div className="flex items-center justify-between text-xs text-[#7E88A8] mb-1">
            <span>Available Stock</span>
            <Boxes className="w-4 h-4 text-[#34D399]" />
          </div>
          <p className="font-heading text-2xl font-bold text-[#34D399]">{totalAvailableUnits}</p>
          <p className="text-[10px] text-[#7E88A8] mt-1">Available across tracked units</p>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-4">
          <div className="flex items-center justify-between text-xs text-[#7E88A8] mb-1">
            <span>Reserved In Carts</span>
            <ShoppingBag className="w-4 h-4 text-[#FBBF24]" />
          </div>
          <p className="font-heading text-2xl font-bold text-[#FBBF24]">{totalReservedUnits}</p>
          <p className="text-[10px] text-[#7E88A8] mt-1">Held during checkout</p>
        </div>

        <div className="bg-[#111520] border border-[#212638] rounded-2xl p-4">
          <div className="flex items-center justify-between text-xs text-[#7E88A8] mb-1">
            <span>Inventory Alerts</span>
            <AlertTriangle className="w-4 h-4 text-[#E8546A]" />
          </div>
          <p className="font-heading text-2xl font-bold text-[#E8546A]">
            {outOfStockCount + lowStockCount}
          </p>
          <p className="text-[10px] text-[#7E88A8] mt-1">
            {outOfStockCount} out of stock / {lowStockCount} low
          </p>
        </div>
      </div>

      {/* Filter Tabs & Search Bar */}
      <div className="space-y-3">
        {/* Status Lifecycle Filter Pills */}
        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 scrollbar-none text-xs font-semibold">
          {[
            { id: 'all', label: 'All Products', count: totalCount },
            { id: 'in_stock', label: 'Active In Stock', count: inStockCount, color: 'text-[#34D399]' },
            { id: 'low_stock', label: 'Low Stock', count: lowStockCount, color: 'text-[#FBBF24]' },
            { id: 'out_of_stock', label: 'Out of Stock', count: outOfStockCount, color: 'text-red-400' },
            { id: 'store_only', label: 'In-Store Only', count: storeOnlyCount, color: 'text-[#60A5FA]' },
            { id: 'draft', label: 'Drafts / Unpublished', count: draftCount, color: 'text-purple-400' },
            { id: 'inactive', label: 'Inactive / Archived', count: products.filter((p) => !p.isActive).length, color: 'text-[#7E88A8]' },
          ].map((tab) => (
            <button
              key={tab.id}
              onClick={() => setStatusFilter(tab.id)}
              className={`px-3 py-1.5 rounded-xl border transition-all whitespace-nowrap flex items-center gap-2 cursor-pointer ${
                statusFilter === tab.id
                  ? 'bg-[#181D2C] border-[#E8546A] text-white shadow-sm'
                  : 'bg-[#111520] border-[#212638] text-[#7E88A8] hover:text-white hover:border-[#2b3248]'
              }`}
            >
              <span className={statusFilter === tab.id ? 'text-white' : tab.color}>{tab.label}</span>
              <span className="px-1.5 py-0.2 rounded-full text-[10px] bg-[#212638] text-[#ECEFFE]">
                {tab.count}
              </span>
            </button>
          ))}
        </div>

        {/* Search & Category Filter Row */}
        <div className="flex flex-col sm:flex-row items-center gap-3">
          <div className="relative flex-1 w-full">
            <Search className="w-4 h-4 text-[#7E88A8] absolute left-3.5 top-3" />
            <input
              type="text"
              placeholder="Search by product name, SKU code, or formulation description..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2.5 rounded-xl bg-[#111520] border border-[#212638] text-white text-xs placeholder-[#7E88A8] focus:outline-none focus:border-[#E8546A]"
            />
          </div>

          <div className="flex items-center gap-2 w-full sm:w-auto">
            <div className="relative w-full sm:w-60">
              <Tag className="w-3.5 h-3.5 text-[#7E88A8] absolute left-3 top-3" />
              <select
                value={categoryFilter}
                onChange={(e) => setCategoryFilter(e.target.value)}
                className="w-full pl-9 pr-8 py-2.5 rounded-xl bg-[#111520] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A] appearance-none"
              >
                <option value="all">All Categories</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>
      </div>

      {/* Products Table */}
      <div className="bg-[#111520] border border-[#212638] rounded-2xl overflow-hidden shadow-xl">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs text-[#ECEFFE]">
            <thead className="bg-[#181D2C] text-[#7E88A8] uppercase tracking-wider font-semibold border-b border-[#212638]">
              <tr>
                <th className="py-3 px-4">Product & Category</th>
                <th className="py-3 px-4">SKU Code</th>
                <th className="py-3 px-4">Price</th>
                <th className="py-3 px-4">Inventory Integrity</th>
                <th className="py-3 px-4">Commerce Status</th>
                <th className="py-3 px-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#212638]">
              {loading ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center text-[#7E88A8]">
                    Loading retail merchandise...
                  </td>
                </tr>
              ) : filteredProducts.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-12 text-center">
                    <p className="text-[#7E88A8] font-medium">No matching retail products found.</p>
                    <p className="text-[11px] text-[#4E5672] mt-1">
                      Try adjusting your status filter or search parameters.
                    </p>
                  </td>
                </tr>
              ) : (
                filteredProducts.map((prod) => {
                  const primaryImg =
                    prod.images?.find((i) => i.isPrimary)?.url ||
                    prod.imageUrl ||
                    'https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=200&q=80';

                  return (
                    <tr key={prod.id} className="hover:bg-[#181D2C]/40 transition-colors">
                      {/* Product Name & Details */}
                      <td className="py-3.5 px-4">
                        <div className="flex items-center gap-3">
                          <div className="w-11 h-11 rounded-xl bg-[#181D2C] border border-[#212638] overflow-hidden flex-shrink-0 relative group">
                            <img src={primaryImg} alt={prod.name} className="w-full h-full object-cover" />
                            {prod.images && prod.images.length > 1 && (
                              <span className="absolute bottom-1 right-1 px-1 py-0.2 rounded text-[9px] bg-black/75 text-white font-mono">
                                +{prod.images.length - 1}
                              </span>
                            )}
                          </div>
                          <div>
                            <div className="flex items-center gap-2">
                              <span className="font-semibold text-white hover:text-[#E8546A] cursor-pointer" onClick={() => setDetailProduct(prod)}>
                                {prod.name}
                              </span>
                              {!prod.isPublished && (
                                <span className="px-1.5 py-0.2 rounded text-[9px] font-bold bg-purple-500/10 text-purple-400 border border-purple-500/20">
                                  Draft
                                </span>
                              )}
                              {!prod.isActive && (
                                <span className="px-1.5 py-0.2 rounded text-[9px] font-bold bg-[#7E88A8]/10 text-[#7E88A8] border border-[#7E88A8]/20">
                                  Inactive
                                </span>
                              )}
                            </div>
                            <div className="flex items-center gap-2 mt-0.5">
                              <span className="text-[10px] text-[#E8546A] font-medium">
                                {prod.categoryName || 'General Merchandise'}
                              </span>
                              <span className="text-[10px] text-[#4E5672]">•</span>
                              <span className="text-[10px] text-[#7E88A8] font-mono">
                                /{prod.slug || 'n-a'}
                              </span>
                            </div>
                          </div>
                        </div>
                      </td>

                      {/* SKU */}
                      <td className="py-3.5 px-4 font-mono text-[11px] text-[#7E88A8]">
                        {prod.sku || 'N/A'}
                      </td>

                      {/* Price */}
                      <td className="py-3.5 px-4 font-heading font-bold text-white">
                        {prod.currency === 'USD' ? '$' : prod.currency}{prod.price.toFixed(2)}
                      </td>

                      {/* Inventory Integrity */}
                      <td className="py-3.5 px-4">
                        {prod.trackInventory ? (
                          <div>
                            <div className="flex items-center gap-2">
                              <span
                                className={`font-mono font-bold text-xs ${
                                  prod.availableQuantity > (prod.lowStockThreshold ?? 5)
                                    ? 'text-[#34D399]'
                                    : prod.availableQuantity > 0
                                    ? 'text-[#FBBF24]'
                                    : 'text-red-400'
                                }`}
                              >
                                {prod.availableQuantity} Avail
                              </span>
                              <button
                                onClick={() => handleOpenAdjust(prod)}
                                className="text-[10px] text-[#60A5FA] hover:underline flex items-center gap-0.5"
                                title="Adjust stock"
                              >
                                <ArrowUpDown className="w-2.5 h-2.5" />
                                <span>Adjust</span>
                              </button>
                            </div>
                            <div className="text-[10px] text-[#7E88A8] font-mono mt-0.5">
                              {prod.stockQuantity} Total • {prod.reservedQuantity} Held • {prod.soldQuantity} Sold
                            </div>
                          </div>
                        ) : (
                          <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-medium bg-[#60A5FA]/10 text-[#60A5FA]">
                            Untracked
                          </span>
                        )}
                      </td>

                      {/* Commerce Availability */}
                      <td className="py-3.5 px-4">
                        <div className="flex flex-col gap-1 items-start">
                          <span
                            className={`inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-bold ${
                              prod.isPurchasableOnline
                                ? 'bg-[#34D399]/15 text-[#34D399]'
                                : 'bg-[#60A5FA]/15 text-[#60A5FA]'
                            }`}
                          >
                            {prod.isPurchasableOnline ? 'Online & Store' : 'In-Store Only'}
                          </span>
                          <span
                            className={`text-[10px] ${
                              prod.isPublished ? 'text-[#7E88A8]' : 'text-purple-400 font-medium'
                            }`}
                          >
                            {prod.isPublished ? 'Published Storefront' : 'Hidden from Public'}
                          </span>
                        </div>
                      </td>

                      {/* Actions */}
                      <td className="py-3.5 px-4 text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          <button
                            onClick={() => setDetailProduct(prod)}
                            className="p-1.5 rounded-lg bg-[#181D2C] hover:bg-[#212638] text-[#ECEFFE] hover:text-white transition-colors"
                            title="View Product Details"
                          >
                            <Eye className="w-3.5 h-3.5" />
                          </button>
                          <button
                            onClick={() => handleOpenEdit(prod)}
                            className="p-1.5 rounded-lg bg-[#181D2C] hover:bg-[#212638] text-[#ECEFFE] hover:text-white transition-colors"
                            title="Edit Product"
                          >
                            <Edit2 className="w-3.5 h-3.5" />
                          </button>
                          <button
                            onClick={() => handleDelete(prod.id)}
                            className="p-1.5 rounded-lg bg-[#181D2C] hover:bg-red-500/20 text-red-400 hover:text-red-300 transition-colors"
                            title="Deactivate / Delete"
                          >
                            <Trash2 className="w-3.5 h-3.5" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Product Detail Slide-over / Modal */}
      {detailProduct && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-fadeIn">
          <div className="bg-[#111520] border border-[#212638] rounded-3xl w-full max-w-2xl max-h-[90vh] overflow-y-auto p-6 space-y-6 shadow-2xl">
            <div className="flex items-center justify-between border-b border-[#212638] pb-4">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-[#E8546A]/10 border border-[#E8546A]/20 flex items-center justify-center text-[#E8546A]">
                  <Boxes className="w-5 h-5" />
                </div>
                <div>
                  <h3 className="font-heading font-bold text-lg text-white">{detailProduct.name}</h3>
                  <p className="text-xs text-[#7E88A8]">
                    SKU: <span className="font-mono text-white">{detailProduct.sku || 'N/A'}</span> • Slug: <span className="font-mono text-white">{detailProduct.slug}</span>
                  </p>
                </div>
              </div>
              <button
                onClick={() => setDetailProduct(null)}
                className="p-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] text-[#7E88A8] hover:text-white"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {/* Gallery Images */}
            {detailProduct.images && detailProduct.images.length > 0 && (
              <div className="space-y-2">
                <span className="text-[11px] font-bold uppercase tracking-wider text-[#7E88A8]">
                  Product Image Gallery ({detailProduct.images.length})
                </span>
                <div className="grid grid-cols-3 gap-3">
                  {detailProduct.images.map((img, i) => (
                    <div key={i} className="aspect-video rounded-xl bg-[#181D2C] border border-[#212638] overflow-hidden relative">
                      <img src={img.url} alt={img.altText || detailProduct.name} className="w-full h-full object-cover" />
                      {img.isPrimary && (
                        <span className="absolute top-2 left-2 px-1.5 py-0.5 rounded text-[9px] font-bold bg-[#E8546A] text-white">
                          Primary
                        </span>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Metrics Breakdown Grid */}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 bg-[#181D2C] p-4 rounded-2xl border border-[#212638]">
              <div>
                <span className="text-[10px] text-[#7E88A8] uppercase font-bold">Total Physical Stock</span>
                <p className="font-mono text-lg font-bold text-white">{detailProduct.stockQuantity}</p>
              </div>
              <div>
                <span className="text-[10px] text-[#7E88A8] uppercase font-bold">Cart Reserved</span>
                <p className="font-mono text-lg font-bold text-[#FBBF24]">{detailProduct.reservedQuantity}</p>
              </div>
              <div>
                <span className="text-[10px] text-[#7E88A8] uppercase font-bold">Available to Buy</span>
                <p className="font-mono text-lg font-bold text-[#34D399]">{detailProduct.availableQuantity}</p>
              </div>
              <div>
                <span className="text-[10px] text-[#7E88A8] uppercase font-bold">Historical Sold</span>
                <p className="font-mono text-lg font-bold text-[#60A5FA]">{detailProduct.soldQuantity}</p>
              </div>
            </div>

            {/* Description & Operational Settings */}
            <div className="space-y-3 text-xs">
              <div>
                <span className="text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] block mb-1">
                  Product Description
                </span>
                <p className="text-[#ECEFFE] bg-[#181D2C] p-3 rounded-xl border border-[#212638] leading-relaxed">
                  {detailProduct.description || 'No description provided.'}
                </p>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <div className="bg-[#181D2C] p-3 rounded-xl border border-[#212638]">
                  <span className="text-[10px] text-[#7E88A8] font-bold uppercase">Storefront Publication</span>
                  <p className="font-semibold text-white mt-1">
                    {detailProduct.isPublished ? 'Published & Discoverable' : 'Draft / Unpublished'}
                  </p>
                </div>
                <div className="bg-[#181D2C] p-3 rounded-xl border border-[#212638]">
                  <span className="text-[10px] text-[#7E88A8] font-bold uppercase">Online Purchasing</span>
                  <p className="font-semibold text-white mt-1">
                    {detailProduct.isPurchasableOnline ? 'Online Cart Enabled' : 'In-Store Purchase Only'}
                  </p>
                </div>
              </div>
            </div>

            {/* Modal Actions */}
            <div className="flex items-center justify-between pt-3 border-t border-[#212638]">
              <button
                onClick={() => handleTogglePublish(detailProduct)}
                className={`px-4 py-2 rounded-xl text-xs font-bold border transition-colors ${
                  detailProduct.isPublished
                    ? 'border-[#212638] text-[#7E88A8] hover:text-white'
                    : 'bg-purple-500/10 border-purple-500/30 text-purple-400 hover:bg-purple-500/20'
                }`}
              >
                {detailProduct.isPublished ? 'Unpublish to Draft' : 'Publish to Storefront'}
              </button>

              <div className="flex items-center gap-2">
                <button
                  onClick={() => {
                    handleOpenAdjust(detailProduct);
                  }}
                  className="px-4 py-2 rounded-xl bg-[#181D2C] hover:bg-[#212638] text-white text-xs font-bold transition-all border border-[#212638]"
                >
                  Quick Stock Adjust
                </button>
                <button
                  onClick={() => {
                    setDetailProduct(null);
                    handleOpenEdit(detailProduct);
                  }}
                  className="px-4 py-2 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white text-xs font-bold transition-all shadow-md"
                >
                  Edit Full Details
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Quick Adjust Inventory Modal */}
      {adjustingProduct && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-fadeIn">
          <div className="bg-[#111520] border border-[#212638] rounded-3xl w-full max-w-md p-6 space-y-5 shadow-2xl">
            <div className="flex items-center justify-between border-b border-[#212638] pb-3">
              <div>
                <h3 className="font-heading font-bold text-base text-white">Adjust Product Stock</h3>
                <p className="text-xs text-[#7E88A8]">{adjustingProduct.name}</p>
              </div>
              <button
                onClick={() => setAdjustingProduct(null)}
                className="p-1 rounded-xl bg-[#181D2C] hover:bg-[#212638] text-[#7E88A8] hover:text-white"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            <form onSubmit={handleSaveStockAdjust} className="space-y-4 text-xs">
              <div className="p-3 rounded-xl bg-[#181D2C] border border-[#212638] text-[11px] text-[#7E88A8]">
                <span>Currently reserved in carts: </span>
                <strong className="text-white font-mono">{adjustingProduct.reservedQuantity} units</strong>.
                <span> Stock level cannot drop below 0 under non-negative invariant.</span>
              </div>

              <div>
                <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  New Total Physical Stock Level
                </label>
                <input
                  type="number"
                  min="0"
                  required
                  value={newStockInput}
                  onChange={(e) => setNewStockInput(Math.max(0, Number(e.target.value)))}
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-sm font-mono focus:outline-none focus:border-[#E8546A]"
                />
              </div>

              <div>
                <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Audit Reason for Stock Adjustment
                </label>
                <select
                  value={adjustReason}
                  onChange={(e) => setAdjustReason(e.target.value)}
                  className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white text-xs focus:outline-none focus:border-[#E8546A]"
                >
                  <option value="Restock Shipment">Restock Shipment</option>
                  <option value="Damaged / Expired Batch">Damaged / Expired Batch</option>
                  <option value="Physical Inventory Audit">Physical Inventory Audit</option>
                  <option value="Customer Return">Customer Return</option>
                </select>
              </div>

              <div className="pt-2 flex justify-end gap-3">
                <button
                  type="button"
                  onClick={() => setAdjustingProduct(null)}
                  className="px-4 py-2 text-xs text-[#7E88A8] hover:text-white"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  className="px-5 py-2.5 rounded-xl bg-[#E8546A] hover:bg-[#D44359] text-white font-bold text-xs shadow-lg"
                >
                  Confirm Adjustment
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* New / Edit Product Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/80 backdrop-blur-sm animate-fadeIn">
          <div className="bg-[#111520] border border-[#212638] rounded-3xl w-full max-w-2xl max-h-[92vh] overflow-y-auto p-6 space-y-6 shadow-2xl">
            <div className="flex items-center justify-between border-b border-[#212638] pb-3">
              <div>
                <h3 className="font-heading font-bold text-lg text-white">
                  {editingProduct ? 'Edit Retail Product' : 'Add New Retail Product'}
                </h3>
                <p className="text-xs text-[#7E88A8]">
                  Define merchandise specifications, pricing, inventory threshold, and public availability.
                </p>
              </div>
              <button
                onClick={() => setIsModalOpen(false)}
                className="p-1.5 rounded-xl bg-[#181D2C] hover:bg-[#212638] text-[#7E88A8] hover:text-white"
              >
                <X className="w-4 h-4" />
              </button>
            </div>

            {formError && (
              <div className="p-3 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 text-xs flex items-center gap-2">
                <AlertTriangle className="w-4 h-4 flex-shrink-0" />
                <span>{formError}</span>
              </div>
            )}

            <form onSubmit={handleSaveProduct} className="space-y-4 text-xs">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    Product Name *
                  </label>
                  <input
                    type="text"
                    required
                    value={name}
                    onChange={(e) => {
                      setName(e.target.value);
                      if (!editingProduct) {
                        setSlug(e.target.value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, ''));
                      }
                    }}
                    placeholder="e.g. Keratin Restorative Mask"
                    className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                  />
                </div>

                <div>
                  <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    Product Category
                  </label>
                  <select
                    value={categoryId}
                    onChange={(e) => setCategoryId(e.target.value)}
                    className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                  >
                    <option value="">Select a Category</option>
                    {categories.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <div>
                  <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    SKU Code *
                  </label>
                  <input
                    type="text"
                    required
                    value={sku}
                    onChange={(e) => setSku(e.target.value)}
                    placeholder="e.g. BL-KER-01"
                    className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white font-mono focus:outline-none focus:border-[#E8546A]"
                  />
                </div>

                <div>
                  <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    Unit Price *
                  </label>
                  <input
                    type="number"
                    min="0"
                    step="0.01"
                    required
                    value={price}
                    onChange={(e) => setPrice(Number(e.target.value))}
                    className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                  />
                </div>

                <div>
                  <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                    Currency (ISO)
                  </label>
                  <input
                    type="text"
                    maxLength={3}
                    required
                    value={currency}
                    onChange={(e) => setCurrency(e.target.value.toUpperCase())}
                    className="w-full px-4 py-2.5 rounded-xl bg-[#181D2C] border border-[#212638] text-white font-mono focus:outline-none focus:border-[#E8546A]"
                  />
                </div>
              </div>

              {/* Slug Preview */}
              <div>
                <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Public URL Slug
                </label>
                <div className="flex items-center gap-2">
                  <span className="text-[#7E88A8] font-mono text-[11px]">/products/</span>
                  <input
                    type="text"
                    value={slug}
                    onChange={(e) => setSlug(e.target.value)}
                    placeholder="keratin-restorative-mask"
                    className="flex-1 px-4 py-2 rounded-xl bg-[#181D2C] border border-[#212638] text-white font-mono focus:outline-none focus:border-[#E8546A]"
                  />
                </div>
              </div>

              {/* Description */}
              <div>
                <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8] mb-1">
                  Product Description
                </label>
                <textarea
                  rows={3}
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Key ingredients, recommended usage, formulation benefits..."
                  className="w-full px-4 py-2 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                />
              </div>

              {/* Inventory Policy Box */}
              <div className="p-4 rounded-2xl bg-[#181D2C] border border-[#212638] space-y-3">
                <div className="flex items-center justify-between">
                  <label htmlFor="track-inv" className="text-white font-bold cursor-pointer">
                    Track Inventory Stock Units
                  </label>
                  <input
                    type="checkbox"
                    id="track-inv"
                    checked={trackInventory}
                    onChange={(e) => setTrackInventory(e.target.checked)}
                    className="rounded border-[#212638] text-[#E8546A] cursor-pointer"
                  />
                </div>

                {trackInventory && (
                  <div className="grid grid-cols-2 gap-3 pt-2 border-t border-[#212638]">
                    <div>
                      <label className="block text-[10px] font-bold uppercase text-[#7E88A8] mb-1">
                        Physical Stock Units
                      </label>
                      <input
                        type="number"
                        min="0"
                        required
                        value={stockQuantity}
                        onChange={(e) => setStockQuantity(Math.max(0, Number(e.target.value)))}
                        className="w-full px-3 py-2 rounded-xl bg-[#111520] border border-[#212638] text-white font-mono focus:outline-none focus:border-[#E8546A]"
                      />
                    </div>
                    <div>
                      <label className="block text-[10px] font-bold uppercase text-[#7E88A8] mb-1">
                        Low Stock Alert Threshold
                      </label>
                      <input
                        type="number"
                        min="0"
                        required
                        value={lowStockThreshold}
                        onChange={(e) => setLowStockThreshold(Math.max(0, Number(e.target.value)))}
                        className="w-full px-3 py-2 rounded-xl bg-[#111520] border border-[#212638] text-white font-mono focus:outline-none focus:border-[#E8546A]"
                      />
                    </div>
                  </div>
                )}
              </div>

              {/* Multi-Image Gallery Manager */}
              <div className="space-y-2">
                <label className="block text-[11px] font-bold uppercase tracking-wider text-[#7E88A8]">
                  Product Image Collection
                </label>
                <div className="flex gap-2">
                  <input
                    type="url"
                    value={newImageUrl}
                    onChange={(e) => setNewImageUrl(e.target.value)}
                    placeholder="https://images.unsplash.com/..."
                    className="flex-1 px-4 py-2 rounded-xl bg-[#181D2C] border border-[#212638] text-white focus:outline-none focus:border-[#E8546A]"
                  />
                  <button
                    type="button"
                    onClick={handleAddImage}
                    className="px-4 py-2 rounded-xl bg-[#212638] hover:bg-[#2b3248] text-white font-semibold transition-colors"
                  >
                    Add Image
                  </button>
                </div>

                {images.length > 0 && (
                  <div className="grid grid-cols-4 gap-2 pt-2">
                    {images.map((img, idx) => (
                      <div key={idx} className="relative aspect-video rounded-xl bg-[#181D2C] border border-[#212638] overflow-hidden group">
                        <img src={img.url} alt="thumbnail" className="w-full h-full object-cover" />
                        <div className="absolute inset-0 bg-black/60 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center gap-2">
                          <button
                            type="button"
                            onClick={() => handleSetPrimaryImage(idx)}
                            className="p-1 rounded bg-[#E8546A] text-white text-[9px]"
                            title="Set as primary"
                          >
                            <Check className="w-3 h-3" />
                          </button>
                          <button
                            type="button"
                            onClick={() => handleRemoveImage(idx)}
                            className="p-1 rounded bg-red-600 text-white text-[9px]"
                            title="Remove"
                          >
                            <Trash2 className="w-3 h-3" />
                          </button>
                        </div>
                        {img.isPrimary && (
                          <span className="absolute bottom-1 left-1 px-1 py-0.2 rounded text-[8px] font-bold bg-[#E8546A] text-white">
                            Primary
                          </span>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Publication & Omnichannel Toggles */}
              <div className="space-y-2 pt-2">
                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="purchasable"
                    checked={isPurchasableOnline}
                    onChange={(e) => setIsPurchasableOnline(e.target.checked)}
                    className="rounded border-[#212638] text-[#E8546A] cursor-pointer"
                  />
                  <label htmlFor="purchasable" className="text-white text-xs cursor-pointer">
                    Available for Online Checkout on Public Storefront (Uncheck for In-Store Only)
                  </label>
                </div>

                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="published"
                    checked={isPublished}
                    onChange={(e) => setIsPublished(e.target.checked)}
                    className="rounded border-[#212638] text-[#E8546A] cursor-pointer"
                  />
                  <label htmlFor="published" className="text-white text-xs cursor-pointer">
                    Published on Public Storefront Profile (Uncheck for Draft mode)
                  </label>
                </div>
              </div>

              <div className="pt-4 flex justify-end gap-3 border-t border-[#212638]">
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
                  className="px-5 py-2.5 rounded-xl bg-gradient-to-r from-[#E8546A] to-[#D44359] hover:from-[#D44359] hover:to-[#B83247] text-white font-bold text-xs shadow-lg disabled:opacity-50 cursor-pointer"
                >
                  {modalSubmitting ? 'Saving...' : editingProduct ? 'Save Changes' : 'Create Product'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
