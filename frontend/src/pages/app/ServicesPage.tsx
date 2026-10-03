import React, { useState } from 'react';
import { Button, IconButton } from '../../components/ui/Button';
import { Input, Select, SearchBox } from '../../components/forms/Inputs';
import { Card } from '../../components/data-display/DataDisplay';
import { Modal, ConfirmDialog } from '../../components/feedback/Feedback';
import { Scissors, Plus, Edit2, Copy, Archive, Clock, DollarSign, Tag, Globe } from 'lucide-react';
import { ServiceItem, ServiceCategoryItem } from '../../services/api/services';

export const ServicesPage: React.FC = () => {
  // Initial state
  const [categories, setCategories] = useState<ServiceCategoryItem[]>([
    { id: 'cat-1', tenantId: 't-1', name: 'Haircuts & Styling', description: 'Precision cuts, blowouts and treatments', sortOrder: 1, isActive: true, servicesCount: 3 },
    { id: 'cat-2', tenantId: 't-1', name: 'Coloring & Highlights', description: 'Balayage, highlights, full color', sortOrder: 2, isActive: true, servicesCount: 2 },
    { id: 'cat-3', tenantId: 't-1', name: 'Nails & Spa', description: 'Manicures, pedicures, and nail care', sortOrder: 3, isActive: true, servicesCount: 1 },
  ]);

  const [services, setServices] = useState<ServiceItem[]>([
    {
      id: 'srv-1',
      tenantId: 't-1',
      categoryId: 'cat-1',
      categoryName: 'Haircuts & Styling',
      name: 'Signature Haircut & Style',
      description: 'Includes consultation, hair wash, scalp massage, precision cut, and blowout.',
      durationMinutes: 45,
      bufferBeforeMinutes: 5,
      bufferAfterMinutes: 10,
      totalDurationMinutes: 60,
      price: 85.00,
      currency: 'USD',
      isActive: true,
      isOnlineBookingEnabled: true,
      isArchived: false,
      colorHex: '#E8546A',
      createdAtUtc: new Date().toISOString(),
    },
    {
      id: 'srv-2',
      tenantId: 't-1',
      categoryId: 'cat-1',
      categoryName: 'Haircuts & Styling',
      name: 'Express Men\'s Cut',
      description: 'Quick clipper and scissor cut with wash and style.',
      durationMinutes: 30,
      bufferBeforeMinutes: 0,
      bufferAfterMinutes: 5,
      totalDurationMinutes: 35,
      price: 45.00,
      currency: 'USD',
      isActive: true,
      isOnlineBookingEnabled: true,
      isArchived: false,
      colorHex: '#34D399',
      createdAtUtc: new Date().toISOString(),
    },
    {
      id: 'srv-3',
      tenantId: 't-1',
      categoryId: 'cat-2',
      categoryName: 'Coloring & Highlights',
      name: 'Full Balayage & Gloss Treatment',
      description: 'Hand-painted highlights with customized gloss tone and deep treatment.',
      durationMinutes: 120,
      bufferBeforeMinutes: 10,
      bufferAfterMinutes: 15,
      totalDurationMinutes: 145,
      price: 220.00,
      currency: 'USD',
      isActive: true,
      isOnlineBookingEnabled: true,
      isArchived: false,
      colorHex: '#FBBF24',
      createdAtUtc: new Date().toISOString(),
    },
  ]);

  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  
  // Modals
  const [isServiceModalOpen, setIsServiceModalOpen] = useState(false);
  const [editingService, setEditingService] = useState<ServiceItem | null>(null);
  const [isCategoryModalOpen, setIsCategoryModalOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<ServiceCategoryItem | null>(null);
  const [archivingServiceId, setArchivingServiceId] = useState<string | null>(null);

  // Form State
  const [serviceForm, setServiceForm] = useState({
    categoryId: 'cat-1',
    name: '',
    description: '',
    durationMinutes: 45,
    bufferBeforeMinutes: 0,
    bufferAfterMinutes: 10,
    price: 50.00,
    currency: 'USD',
    isOnlineBookingEnabled: true,
    colorHex: '#E8546A',
  });

  const [categoryForm, setCategoryForm] = useState({
    name: '',
    description: '',
    sortOrder: 1,
  });

  const filteredServices = services.filter((srv) => {
    const matchesSearch = srv.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (srv.description && srv.description.toLowerCase().includes(searchQuery.toLowerCase()));
    const matchesCat = selectedCategory === 'all' || srv.categoryId === selectedCategory;
    return matchesSearch && matchesCat && !srv.isArchived;
  });

  const handleOpenCreateService = () => {
    setEditingService(null);
    setServiceForm({
      categoryId: categories[0]?.id || 'cat-1',
      name: '',
      description: '',
      durationMinutes: 45,
      bufferBeforeMinutes: 0,
      bufferAfterMinutes: 10,
      price: 50.00,
      currency: 'USD',
      isOnlineBookingEnabled: true,
      colorHex: '#E8546A',
    });
    setIsServiceModalOpen(true);
  };

  const handleOpenEditService = (service: ServiceItem) => {
    setEditingService(service);
    setServiceForm({
      categoryId: service.categoryId,
      name: service.name,
      description: service.description || '',
      durationMinutes: service.durationMinutes,
      bufferBeforeMinutes: service.bufferBeforeMinutes,
      bufferAfterMinutes: service.bufferAfterMinutes,
      price: service.price,
      currency: service.currency,
      isOnlineBookingEnabled: service.isOnlineBookingEnabled,
      colorHex: service.colorHex || '#E8546A',
    });
    setIsServiceModalOpen(true);
  };

  const handleSaveService = (e: React.FormEvent) => {
    e.preventDefault();
    const categoryName = categories.find(c => c.id === serviceForm.categoryId)?.name || 'General';
    const totalDuration = Number(serviceForm.durationMinutes) + Number(serviceForm.bufferBeforeMinutes) + Number(serviceForm.bufferAfterMinutes);

    if (editingService) {
      setServices(services.map(s => s.id === editingService.id ? {
        ...s,
        ...serviceForm,
        categoryName,
        totalDurationMinutes: totalDuration,
      } : s));
    } else {
      const newService: ServiceItem = {
        id: `srv-${Date.now()}`,
        tenantId: 't-1',
        ...serviceForm,
        categoryName,
        totalDurationMinutes: totalDuration,
        isActive: true,
        isArchived: false,
        createdAtUtc: new Date().toISOString(),
      };
      setServices([...services, newService]);
    }
    setIsServiceModalOpen(false);
  };

  const handleDuplicateService = (service: ServiceItem) => {
    const copy: ServiceItem = {
      ...service,
      id: `srv-${Date.now()}`,
      name: `${service.name} (Copy)`,
      createdAtUtc: new Date().toISOString(),
    };
    setServices([...services, copy]);
  };

  const handleConfirmArchive = () => {
    if (archivingServiceId) {
      setServices(services.map(s => s.id === archivingServiceId ? { ...s, isArchived: true } : s));
      setArchivingServiceId(null);
    }
  };

  const handleSaveCategory = (e: React.FormEvent) => {
    e.preventDefault();
    if (editingCategory) {
      setCategories(categories.map(c => c.id === editingCategory.id ? { ...c, ...categoryForm } : c));
    } else {
      const newCat: ServiceCategoryItem = {
        id: `cat-${Date.now()}`,
        tenantId: 't-1',
        ...categoryForm,
        isActive: true,
        servicesCount: 0,
      };
      setCategories([...categories, newCat]);
    }
    setIsCategoryModalOpen(false);
  };

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-[#ECEFFE] font-semibold tracking-tight">
            Service Catalog
          </h1>
          <p className="text-sm text-[#7E88A8] mt-1">
            Configure service offerings, durations, pricing, and buffer intervals.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Button variant="secondary" onClick={() => { setEditingCategory(null); setCategoryForm({ name: '', description: '', sortOrder: categories.length + 1 }); setIsCategoryModalOpen(true); }}>
            <Tag className="w-4 h-4 mr-2" />
            Add Category
          </Button>
          <Button variant="primary" onClick={handleOpenCreateService}>
            <Plus className="w-4 h-4 mr-2" />
            Create Service
          </Button>
        </div>
      </div>

      {/* Filter Bar */}
      <Card className="p-4">
        <div className="flex flex-col md:flex-row items-center justify-between gap-4">
          <div className="w-full md:w-80">
            <SearchBox
              placeholder="Search services..."
              value={searchQuery}
              onChange={(val) => setSearchQuery(val)}
            />
          </div>

          <div className="flex items-center gap-2 overflow-x-auto w-full md:w-auto pb-1 md:pb-0">
            <button
              onClick={() => setSelectedCategory('all')}
              className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-colors ${
                selectedCategory === 'all'
                  ? 'bg-[#E8546A] text-white'
                  : 'bg-[#181D2C] text-[#7E88A8] hover:text-[#ECEFFE] hover:bg-[#212638]'
              }`}
            >
              All Categories ({services.filter(s => !s.isArchived).length})
            </button>
            {categories.map((cat) => (
              <button
                key={cat.id}
                onClick={() => setSelectedCategory(cat.id)}
                className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-colors whitespace-nowrap ${
                  selectedCategory === cat.id
                    ? 'bg-[#E8546A] text-white'
                    : 'bg-[#181D2C] text-[#7E88A8] hover:text-[#ECEFFE] hover:bg-[#212638]'
                }`}
              >
                {cat.name} ({services.filter(s => s.categoryId === cat.id && !s.isArchived).length})
              </button>
            ))}
          </div>
        </div>
      </Card>

      {/* Services Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        {filteredServices.map((srv) => (
          <Card key={srv.id} className="flex flex-col justify-between hover:border-[#E8546A]/40 transition-all group">
            <div>
              {/* Category & Color tag header */}
              <div className="flex items-center justify-between gap-2 mb-3">
                <div className="flex items-center gap-2">
                  <span
                    className="w-3 h-3 rounded-full flex-shrink-0"
                    style={{ backgroundColor: srv.colorHex || '#E8546A' }}
                  />
                  <span className="text-xs font-medium text-[#7E88A8] uppercase tracking-wider">
                    {srv.categoryName}
                  </span>
                </div>
                <span className={`px-2 py-0.5 text-[10px] font-semibold rounded-full ${srv.isActive ? 'bg-[#34D399]/10 text-[#34D399]' : 'bg-[#7E88A8]/10 text-[#7E88A8]'}`}>
                  {srv.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>

              {/* Service Title */}
              <h3 className="text-lg font-semibold text-[#ECEFFE] group-hover:text-[#E8546A] transition-colors">
                {srv.name}
              </h3>

              <p className="text-xs text-[#7E88A8] mt-1.5 line-clamp-2">
                {srv.description || 'No description specified.'}
              </p>

              {/* Specs Badge Bar */}
              <div className="grid grid-cols-2 gap-2 my-4 pt-3 border-t border-[#212638]">
                <div className="flex items-center gap-2 bg-[#181D2C] p-2 rounded-md">
                  <Clock className="w-4 h-4 text-[#E8546A]" />
                  <div>
                    <div className="text-xs font-medium text-[#ECEFFE]">
                      {srv.durationMinutes} mins
                    </div>
                    {(srv.bufferBeforeMinutes > 0 || srv.bufferAfterMinutes > 0) && (
                      <div className="text-[10px] text-[#7E88A8]">
                        +{srv.bufferBeforeMinutes}m pre / +{srv.bufferAfterMinutes}m post
                      </div>
                    )}
                  </div>
                </div>

                <div className="flex items-center gap-2 bg-[#181D2C] p-2 rounded-md">
                  <DollarSign className="w-4 h-4 text-[#34D399]" />
                  <div>
                    <div className="text-xs font-medium text-[#34D399]">
                      ${srv.price.toFixed(2)}
                    </div>
                    <div className="text-[10px] text-[#7E88A8]">
                      {srv.currency}
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Footer Actions */}
            <div className="flex items-center justify-between pt-3 border-t border-[#212638]">
              <div className="flex items-center text-[11px] text-[#7E88A8]">
                {srv.isOnlineBookingEnabled ? (
                  <span className="flex items-center gap-1 text-[#34D399]">
                    <Globe className="w-3 h-3" /> Online Booking
                  </span>
                ) : (
                  <span className="text-[#7E88A8]">Internal Only</span>
                )}
              </div>

              <div className="flex items-center gap-1">
                <IconButton
                  icon={<Copy className="w-4 h-4" />}
                  variant="ghost"
                  size="sm"
                  title="Duplicate Service"
                  onClick={() => handleDuplicateService(srv)}
                />

                <IconButton
                  icon={<Edit2 className="w-4 h-4" />}
                  variant="ghost"
                  size="sm"
                  title="Edit Service"
                  onClick={() => handleOpenEditService(srv)}
                />

                <IconButton
                  icon={<Archive className="w-4 h-4 text-[#E8546A]" />}
                  variant="ghost"
                  size="sm"
                  title="Archive Service"
                  onClick={() => setArchivingServiceId(srv.id)}
                />
              </div>
            </div>
          </Card>
        ))}
      </div>

      {filteredServices.length === 0 && (
        <Card className="p-12 text-center">
          <Scissors className="w-10 h-10 text-[#7E88A8] mx-auto mb-3" />
          <h3 className="text-lg font-medium text-[#ECEFFE]">No services found</h3>
          <p className="text-xs text-[#7E88A8] mt-1 max-w-sm mx-auto">
            Try adjusting your search query or selected category to find active services.
          </p>
          <Button variant="primary" className="mt-4" onClick={handleOpenCreateService}>
            <Plus className="w-4 h-4 mr-2" />
            Add First Service
          </Button>
        </Card>
      )}

      {/* Service Modal */}
      <Modal
        isOpen={isServiceModalOpen}
        onClose={() => setIsServiceModalOpen(false)}
        title={editingService ? 'Edit Service' : 'Create New Service'}
      >
        <form onSubmit={handleSaveService} className="space-y-4">
          <Select
            label="Service Category"
            value={serviceForm.categoryId}
            onChange={(e) => setServiceForm({ ...serviceForm, categoryId: e.target.value })}
            options={categories.map((c) => ({ value: c.id, label: c.name }))}
            required
          />

          <Input
            label="Service Name"
            placeholder="e.g. Signature Haircut & Styling"
            value={serviceForm.name}
            onChange={(e) => setServiceForm({ ...serviceForm, name: e.target.value })}
            required
          />

          <div>
            <label className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider mb-1.5">
              Description
            </label>
            <textarea
              className="w-full bg-[#181D2C] border border-[#212638] rounded-xl px-4 py-2.5 text-sm text-[#ECEFFE] focus:outline-none focus:border-[#E8546A] min-h-[80px]"
              placeholder="Detailed description of what the service includes..."
              value={serviceForm.description}
              onChange={(e) => setServiceForm({ ...serviceForm, description: e.target.value })}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <Input
              label="Duration (mins)"
              type="number"
              min={5}
              value={serviceForm.durationMinutes}
              onChange={(e) => setServiceForm({ ...serviceForm, durationMinutes: Number(e.target.value) })}
              required
            />
            <Input
              label="Buffer Before (mins)"
              type="number"
              min={0}
              value={serviceForm.bufferBeforeMinutes}
              onChange={(e) => setServiceForm({ ...serviceForm, bufferBeforeMinutes: Number(e.target.value) })}
            />
            <Input
              label="Buffer After (mins)"
              type="number"
              min={0}
              value={serviceForm.bufferAfterMinutes}
              onChange={(e) => setServiceForm({ ...serviceForm, bufferAfterMinutes: Number(e.target.value) })}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <Input
              label="Price ($)"
              type="number"
              step="0.01"
              min={0}
              value={serviceForm.price}
              onChange={(e) => setServiceForm({ ...serviceForm, price: Number(e.target.value) })}
              required
            />
            <div>
              <label className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider mb-1.5">
                Calendar Color Hex
              </label>
              <div className="flex items-center gap-2">
                <input
                  type="color"
                  className="w-10 h-10 rounded-xl border border-[#212638] bg-transparent cursor-pointer"
                  value={serviceForm.colorHex}
                  onChange={(e) => setServiceForm({ ...serviceForm, colorHex: e.target.value })}
                />
                <Input
                  value={serviceForm.colorHex}
                  onChange={(e) => setServiceForm({ ...serviceForm, colorHex: e.target.value })}
                />
              </div>
            </div>
          </div>

          <div className="flex items-center gap-2 pt-2">
            <input
              type="checkbox"
              id="onlineBooking"
              className="rounded bg-[#181D2C] border-[#212638] text-[#E8546A] focus:ring-0 cursor-pointer"
              checked={serviceForm.isOnlineBookingEnabled}
              onChange={(e) => setServiceForm({ ...serviceForm, isOnlineBookingEnabled: e.target.checked })}
            />
            <label htmlFor="onlineBooking" className="text-xs text-[#ECEFFE] cursor-pointer">
              Enable for online public customer booking
            </label>
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t border-[#212638]">
            <Button variant="ghost" type="button" onClick={() => setIsServiceModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" type="submit">
              {editingService ? 'Save Changes' : 'Create Service'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Category Modal */}
      <Modal
        isOpen={isCategoryModalOpen}
        onClose={() => setIsCategoryModalOpen(false)}
        title={editingCategory ? 'Edit Category' : 'Create Category'}
      >
        <form onSubmit={handleSaveCategory} className="space-y-4">
          <Input
            label="Category Name"
            placeholder="e.g. Haircuts & Styling"
            value={categoryForm.name}
            onChange={(e) => setCategoryForm({ ...categoryForm, name: e.target.value })}
            required
          />

          <Input
            label="Sort Order"
            type="number"
            value={categoryForm.sortOrder}
            onChange={(e) => setCategoryForm({ ...categoryForm, sortOrder: Number(e.target.value) })}
          />

          <div>
            <label className="block text-xs font-semibold text-[#7E88A8] uppercase tracking-wider mb-1.5">
              Description
            </label>
            <textarea
              className="w-full bg-[#181D2C] border border-[#212638] rounded-xl px-4 py-2.5 text-sm text-[#ECEFFE] focus:outline-none focus:border-[#E8546A] min-h-[60px]"
              placeholder="Short description..."
              value={categoryForm.description}
              onChange={(e) => setCategoryForm({ ...categoryForm, description: e.target.value })}
            />
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t border-[#212638]">
            <Button variant="ghost" type="button" onClick={() => setIsCategoryModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" type="submit">
              Save Category
            </Button>
          </div>
        </form>
      </Modal>

      {/* Archive Confirm Dialog */}
      <ConfirmDialog
        isOpen={!!archivingServiceId}
        onClose={() => setArchivingServiceId(null)}
        onConfirm={handleConfirmArchive}
        title="Archive Service"
        message="Are you sure you want to archive this service? It will no longer be available for new bookings."
        confirmText="Archive Service"
        isDanger={true}
      />
    </div>
  );
};
