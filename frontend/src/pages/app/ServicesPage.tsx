import React, { useState, useEffect, useMemo } from 'react';
import { Button } from '../../components/ui/Button';
import { Input, SearchBox } from '../../components/forms/Inputs';
import { Card } from '../../components/data-display/DataDisplay';
import { Modal, ConfirmDialog, EmptyState, ErrorState, Skeleton } from '../../components/feedback/Feedback';
import { useDebounce } from '../../hooks/useDebounce';
import { 
  Scissors, Plus, Edit2, Copy, Archive, Clock, DollarSign, Tag, Globe, 
  CheckCircle2, XCircle, AlertCircle, Sparkles, Building2, User, Box,
  Layers, RefreshCw, Eye, EyeOff
} from 'lucide-react';
import { 
  servicesApi, ServiceItem, ServiceCategoryItem, ResourceItem,
  CreateServicePayload, UpdateServicePayload, CreateResourcePayload
} from '../../services/api/services';
import { staffApi, StaffItem } from '../../services/api/staff';
import { locationsApi, LocationItem } from '../../services/api/locations';
import { useAuth } from '../../app/providers/AuthProvider';

export const ServicesPage: React.FC = () => {
  const { token, activeBusiness } = useAuth();

  const [categories, setCategories] = useState<ServiceCategoryItem[]>([]);
  const [services, setServices] = useState<ServiceItem[]>([]);
  const [staffList, setStaffList] = useState<StaffItem[]>([]);
  const [locations, setLocations] = useState<LocationItem[]>([]);
  const [resources, setResources] = useState<ResourceItem[]>([]);

  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  // Filters
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [includeArchived, setIncludeArchived] = useState<boolean>(false);

  const debouncedSearch = useDebounce(searchQuery, 300);

  // Modals
  const [isServiceModalOpen, setIsServiceModalOpen] = useState(false);
  const [editingService, setEditingService] = useState<ServiceItem | null>(null);
  const [isCategoryModalOpen, setIsCategoryModalOpen] = useState(false);
  const [editingCategory, setEditingCategory] = useState<ServiceCategoryItem | null>(null);
  const [isResourceModalOpen, setIsResourceModalOpen] = useState(false);
  const [archivingServiceId, setArchivingServiceId] = useState<string | null>(null);

  // Service Form State
  const [serviceForm, setServiceForm] = useState({
    categoryId: '',
    name: '',
    slug: '',
    description: '',
    durationMinutes: 45,
    bufferBeforeMinutes: 0,
    bufferAfterMinutes: 10,
    price: 50.00,
    currency: 'USD',
    isOnlineBookingEnabled: true,
    isPublished: true,
    isActive: true,
    colorHex: '#E8546A',
    requiresResource: false,
    requiredResourceType: 'Room',
    requiredResourceId: '',
    staffIds: [] as string[],
    locationIds: [] as string[],
  });

  // Category Form State
  const [categoryForm, setCategoryForm] = useState({
    name: '',
    description: '',
    sortOrder: 1,
  });

  // Resource Form State
  const [resourceForm, setResourceForm] = useState({
    name: '',
    description: '',
    type: 'Room',
    capacity: 1,
    locationId: '',
  });

  // Load Initial Data
  const loadData = async () => {
    if (!token) return;
    setIsLoading(true);
    setErrorMsg(null);
    try {
      const [catsRes, servsRes, staffRes, locsRes, resRes] = await Promise.all([
        servicesApi.getCategories(token).catch(() => []),
        servicesApi.getServices(token, { includeArchived }).catch(() => []),
        staffApi.getAllStaff(token).catch(() => []),
        locationsApi.getLocations(token).catch(() => []),
        servicesApi.getResources(token).catch(() => []),
      ]);

      setCategories(catsRes);
      setServices(servsRes);
      setStaffList(staffRes);
      setLocations(locsRes);
      setResources(resRes);
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to load services data');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [token, includeArchived]);

  // Flash message helper
  const showToast = (msg: string) => {
    setSuccessMsg(msg);
    setTimeout(() => setSuccessMsg(null), 4000);
  };

  // Filtered Services
  const filteredServices = useMemo(() => {
    return services.filter((srv) => {
      const matchesSearch = srv.name.toLowerCase().includes(debouncedSearch.toLowerCase()) ||
        (srv.description && srv.description.toLowerCase().includes(debouncedSearch.toLowerCase())) ||
        (srv.slug && srv.slug.toLowerCase().includes(debouncedSearch.toLowerCase()));
      const matchesCat = selectedCategory === 'all' || srv.categoryId === selectedCategory;
      const matchesStatus = statusFilter === 'all' || 
        (statusFilter === 'active' && srv.isActive && !srv.isArchived) ||
        (statusFilter === 'draft' && !srv.isPublished && !srv.isArchived) ||
        (statusFilter === 'inactive' && !srv.isActive && !srv.isArchived);
      return matchesSearch && matchesCat && matchesStatus;
    });
  }, [services, debouncedSearch, selectedCategory, statusFilter]);

  // Statistics
  const activeCount = services.filter(s => s.isActive && !s.isArchived).length;
  const publishedCount = services.filter(s => s.isPublished && s.isOnlineBookingEnabled && !s.isArchived).length;
  const avgPrice = activeCount > 0 
    ? (services.filter(s => s.isActive && !s.isArchived).reduce((acc, s) => acc + Number(s.price), 0) / activeCount).toFixed(2)
    : '0.00';
  const resourceReqCount = services.filter(s => s.requiresResource && !s.isArchived).length;

  // Handlers for Services
  const handleOpenCreateService = () => {
    setEditingService(null);
    setServiceForm({
      categoryId: categories[0]?.id || '',
      name: '',
      slug: '',
      description: '',
      durationMinutes: 45,
      bufferBeforeMinutes: 0,
      bufferAfterMinutes: 10,
      price: 50.00,
      currency: 'INR',
      isOnlineBookingEnabled: true,
      isPublished: true,
      isActive: true,
      colorHex: '#E8546A',
      requiresResource: false,
      requiredResourceType: 'Room',
      requiredResourceId: '',
      staffIds: staffList.map(s => s.id),
      locationIds: locations.map(l => l.id),
    });
    setIsServiceModalOpen(true);
  };

  const handleOpenEditService = (service: ServiceItem) => {
    setEditingService(service);
    setServiceForm({
      categoryId: service.categoryId,
      name: service.name,
      slug: service.slug || '',
      description: service.description || '',
      durationMinutes: service.durationMinutes,
      bufferBeforeMinutes: service.bufferBeforeMinutes,
      bufferAfterMinutes: service.bufferAfterMinutes,
      price: service.price,
      currency: service.currency,
      isOnlineBookingEnabled: service.isOnlineBookingEnabled,
      isPublished: service.isPublished ?? true,
      isActive: service.isActive,
      colorHex: service.colorHex || '#E8546A',
      requiresResource: service.requiresResource || false,
      requiredResourceType: service.requiredResourceType || 'Room',
      requiredResourceId: service.requiredResourceId || '',
      staffIds: service.staffIds || [],
      locationIds: service.locationIds || [],
    });
    setIsServiceModalOpen(true);
  };

  const handleSaveService = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;

    if (!serviceForm.name.trim()) {
      setErrorMsg('Service name is required');
      return;
    }
    if (!serviceForm.categoryId) {
      setErrorMsg('Please select a service category');
      return;
    }
    if (serviceForm.durationMinutes <= 0) {
      setErrorMsg('Duration must be greater than 0 minutes');
      return;
    }

    try {
      if (editingService) {
        const payload: UpdateServicePayload = {
          categoryId: serviceForm.categoryId,
          name: serviceForm.name.trim(),
          slug: serviceForm.slug.trim() || undefined,
          description: serviceForm.description || undefined,
          durationMinutes: Number(serviceForm.durationMinutes),
          bufferBeforeMinutes: Number(serviceForm.bufferBeforeMinutes),
          bufferAfterMinutes: Number(serviceForm.bufferAfterMinutes),
          price: Number(serviceForm.price),
          currency: serviceForm.currency,
          isActive: serviceForm.isActive,
          isOnlineBookingEnabled: serviceForm.isOnlineBookingEnabled,
          isPublished: serviceForm.isPublished,
          colorHex: serviceForm.colorHex,
          requiresResource: serviceForm.requiresResource,
          requiredResourceType: serviceForm.requiresResource ? serviceForm.requiredResourceType : undefined,
          requiredResourceId: (serviceForm.requiresResource && serviceForm.requiredResourceId) ? serviceForm.requiredResourceId : undefined,
          staffIds: serviceForm.staffIds,
          locationIds: serviceForm.locationIds,
        };

        const updated = await servicesApi.updateService(token, editingService.id, payload);
        setServices(services.map(s => s.id === updated.id ? updated : (s.id === editingService.id ? updated : s)));
        showToast(`Service "${updated.name}" updated successfully.`);
      } else {
        const payload: CreateServicePayload = {
          categoryId: serviceForm.categoryId,
          name: serviceForm.name.trim(),
          slug: serviceForm.slug.trim() || undefined,
          description: serviceForm.description || undefined,
          durationMinutes: Number(serviceForm.durationMinutes),
          bufferBeforeMinutes: Number(serviceForm.bufferBeforeMinutes),
          bufferAfterMinutes: Number(serviceForm.bufferAfterMinutes),
          price: Number(serviceForm.price),
          currency: serviceForm.currency,
          isOnlineBookingEnabled: serviceForm.isOnlineBookingEnabled,
          isPublished: serviceForm.isPublished,
          colorHex: serviceForm.colorHex,
          requiresResource: serviceForm.requiresResource,
          requiredResourceType: serviceForm.requiresResource ? serviceForm.requiredResourceType : undefined,
          requiredResourceId: (serviceForm.requiresResource && serviceForm.requiredResourceId) ? serviceForm.requiredResourceId : undefined,
          staffIds: serviceForm.staffIds,
          locationIds: serviceForm.locationIds,
        };

        const created = await servicesApi.createService(token, payload);
        setServices([created, ...services]);
        showToast(`Service "${created.name}" created successfully.`);
      }

      setIsServiceModalOpen(false);
      loadData();
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to save service');
    }
  };

  const handleDuplicateService = async (service: ServiceItem) => {
    if (!token) return;
    try {
      const duplicated = await servicesApi.duplicateService(token, service.id);
      setServices([duplicated, ...services]);
      showToast(`Created duplicate: "${duplicated.name}"`);
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to duplicate service');
    }
  };

  const handleTogglePublish = async (service: ServiceItem) => {
    if (!token) return;
    try {
      const newPublished = !service.isPublished;
      const updated = await servicesApi.setPublishStatus(token, service.id, newPublished);
      setServices(services.map(s => s.id === service.id ? updated : s));
      showToast(`Service "${service.name}" is now ${newPublished ? 'Published' : 'Unpublished'}.`);
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to toggle publish status');
    }
  };

  const handleToggleActive = async (service: ServiceItem) => {
    if (!token) return;
    try {
      const newStatus = service.isActive ? 'Inactive' : 'Active';
      const updated = await servicesApi.setStatus(token, service.id, newStatus);
      setServices(services.map(s => s.id === service.id ? updated : s));
      showToast(`Service "${service.name}" is now ${newStatus}.`);
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to change service status');
    }
  };

  const handleConfirmArchive = async () => {
    if (!token || !archivingServiceId) return;
    try {
      await servicesApi.archiveService(token, archivingServiceId);
      setServices(services.map(s => s.id === archivingServiceId ? { ...s, isArchived: true, isActive: false, status: 'Archived' } : s));
      showToast('Service archived successfully.');
      setArchivingServiceId(null);
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to archive service');
    }
  };

  // Categories Handlers
  const handleSaveCategory = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;
    if (!categoryForm.name.trim()) return;

    try {
      if (editingCategory) {
        const updated = await servicesApi.updateCategory(token, editingCategory.id, {
          name: categoryForm.name.trim(),
          description: categoryForm.description || undefined,
          sortOrder: Number(categoryForm.sortOrder),
        });
        setCategories(categories.map(c => c.id === updated.id ? updated : c));
        showToast('Category updated successfully.');
      } else {
        const created = await servicesApi.createCategory(token, {
          name: categoryForm.name.trim(),
          description: categoryForm.description || undefined,
          sortOrder: Number(categoryForm.sortOrder),
        });
        setCategories([...categories, created]);
        showToast('Category created successfully.');
      }
      setCategoryForm({ name: '', description: '', sortOrder: categories.length + 1 });
      setEditingCategory(null);
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to save category');
    }
  };

  const handleDeleteCategory = async (id: string) => {
    if (!token) return;
    try {
      await servicesApi.deleteCategory(token, id);
      setCategories(categories.filter(c => c.id !== id));
      showToast('Category removed successfully.');
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to delete category');
    }
  };

  // Resource Handlers
  const handleCreateResource = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;
    if (!resourceForm.name.trim()) return;

    try {
      const created = await servicesApi.createResource(token, {
        name: resourceForm.name.trim(),
        description: resourceForm.description || undefined,
        type: resourceForm.type,
        capacity: Number(resourceForm.capacity),
        locationId: resourceForm.locationId || undefined,
      });
      setResources([...resources, created]);
      setResourceForm({ name: '', description: '', type: 'Room', capacity: 1, locationId: '' });
      showToast(`Resource "${created.name}" created successfully.`);
    } catch (err: any) {
      setErrorMsg(err.message || 'Failed to create resource');
    }
  };

  const totalOccupancyCalculated = Number(serviceForm.durationMinutes) + Number(serviceForm.bufferBeforeMinutes) + Number(serviceForm.bufferAfterMinutes);

  return (
    <div className="space-y-6">
      {/* Toast Alert */}
      {successMsg && (
        <div className="bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 p-4 rounded-xl flex items-center gap-3 animate-fade-in">
          <CheckCircle2 className="w-5 h-5 flex-shrink-0" />
          <p className="text-sm font-medium">{successMsg}</p>
        </div>
      )}

      {errorMsg && (
        <div className="bg-rose-500/10 border border-rose-500/20 text-rose-400 p-4 rounded-xl flex items-center justify-between gap-3 animate-fade-in">
          <div className="flex items-center gap-3">
            <AlertCircle className="w-5 h-5 flex-shrink-0" />
            <p className="text-sm font-medium">{errorMsg}</p>
          </div>
          <button onClick={() => setErrorMsg(null)} className="text-xs underline hover:text-rose-300">Dismiss</button>
        </div>
      )}

      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold font-heading text-[#F4F6FA] flex items-center gap-3">
            <Scissors className="w-7 h-7 text-[#E8546A]" />
            Service Management
          </h1>
          <p className="text-sm text-[#7E88A8] mt-1">
            Configure bookable activities, buffer windows, staff eligibility, and required resources.
          </p>
        </div>
        <div className="flex items-center gap-3">
          <Button variant="outline" onClick={() => setIsResourceModalOpen(true)}>
            <Box className="w-4 h-4 mr-2" />
            Bookable Resources ({resources.length})
          </Button>
          <Button variant="outline" onClick={() => {
            setEditingCategory(null);
            setCategoryForm({ name: '', description: '', sortOrder: categories.length + 1 });
            setIsCategoryModalOpen(true);
          }}>
            <Layers className="w-4 h-4 mr-2" />
            Categories ({categories.length})
          </Button>
          <Button variant="primary" onClick={handleOpenCreateService}>
            <Plus className="w-4 h-4 mr-2" />
            New Service
          </Button>
        </div>
      </div>

      {/* Metric Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <Card className="p-4 bg-[#141B34] border-[#1C264F]">
          <p className="text-xs text-[#7E88A8]">Active Services</p>
          <p className="text-2xl font-bold text-[#F4F6FA] mt-1">{activeCount}</p>
          <span className="text-[10px] text-emerald-400">Ready for booking</span>
        </Card>
        <Card className="p-4 bg-[#141B34] border-[#1C264F]">
          <p className="text-xs text-[#7E88A8]">Online Storefront</p>
          <p className="text-2xl font-bold text-[#F4F6FA] mt-1">{publishedCount}</p>
          <span className="text-[10px] text-[#A2B4D6]">Published to marketplace</span>
        </Card>
        <Card className="p-4 bg-[#141B34] border-[#1C264F]">
          <p className="text-xs text-[#7E88A8]">Average Price</p>
          <p className="text-2xl font-bold text-[#F4F6FA] mt-1">
            INR {avgPrice}
          </p>
          <span className="text-[10px] text-[#7E88A8]">Across active catalogue</span>
        </Card>
        <Card className="p-4 bg-[#141B34] border-[#1C264F]">
          <p className="text-xs text-[#7E88A8]">Requires Resource</p>
          <p className="text-2xl font-bold text-[#F4F6FA] mt-1">{resourceReqCount}</p>
          <span className="text-[10px] text-amber-400">Rooms &amp; Equipment bound</span>
        </Card>
      </div>

      {/* Filters Bar */}
      <Card className="p-4 bg-[#141B34] border-[#1C264F] flex flex-col md:flex-row gap-4 justify-between items-center">
        <div className="w-full md:w-80">
          <SearchBox
            placeholder="Search services by name or description..."
            value={searchQuery}
            onChange={(val) => setSearchQuery(val)}
          />
        </div>
        <div className="flex flex-wrap items-center gap-3 w-full md:w-auto">
          <select
            value={selectedCategory}
            onChange={(e) => setSelectedCategory(e.target.value)}
            className="w-44 bg-[#0B0F1E] border border-[#1C264F] rounded-xl px-3 py-2 text-xs text-[#F4F6FA] focus:border-[#E8546A] outline-none cursor-pointer"
          >
            <option value="all">All Categories</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>

          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="w-36 bg-[#0B0F1E] border border-[#1C264F] rounded-xl px-3 py-2 text-xs text-[#F4F6FA] focus:border-[#E8546A] outline-none cursor-pointer"
          >
            <option value="all">All Statuses</option>
            <option value="active">Active</option>
            <option value="draft">Draft (Unpublished)</option>
            <option value="inactive">Inactive</option>
          </select>

          <label className="flex items-center gap-2 text-xs text-[#A2B4D6] cursor-pointer">
            <input
              type="checkbox"
              checked={includeArchived}
              onChange={(e) => setIncludeArchived(e.target.checked)}
              className="rounded bg-[#0B0F1E] border-[#1C264F] text-[#E8546A] focus:ring-0"
            />
            Show Archived
          </label>

          <button 
            type="button"
            onClick={loadData}
            title="Refresh Services"
            className="p-2 rounded-xl text-[#7E88A8] hover:text-[#F4F6FA] hover:bg-[#181D2C] transition-colors"
          >
            <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin' : ''}`} />
          </button>
        </div>
      </Card>

      {/* Services Grid Content */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <Skeleton className="h-72 rounded-2xl" />
          <Skeleton className="h-72 rounded-2xl" />
          <Skeleton className="h-72 rounded-2xl" />
        </div>
      ) : errorMsg ? (
        <ErrorState
          title="Unable to load services"
          message={errorMsg}
          onRetry={loadData}
        />
      ) : filteredServices.length === 0 ? (
        <EmptyState
          title={services.length === 0 ? "No services yet." : "No services match your criteria."}
          description={
            services.length === 0
              ? "Start building your bookable catalogue with durations, buffers, and staff assignments."
              : `No services found matching "${debouncedSearch || selectedCategory}". Try clearing your filters.`
          }
          action={
            services.length === 0 ? (
              <Button variant="primary" onClick={handleOpenCreateService}>
                <Plus className="w-4 h-4 mr-2" /> Create First Service
              </Button>
            ) : (
              <Button
                variant="outline"
                onClick={() => {
                  setSearchQuery('');
                  setSelectedCategory('all');
                  setStatusFilter('all');
                }}
              >
                Reset Filters
              </Button>
            )
          }
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {filteredServices.map((service) => {
            const assignedStaffCount = service.staffIds?.length || 0;
            const assignedLocationCount = service.locationIds?.length || 0;

            return (
              <Card 
                key={service.id} 
                className={`p-5 bg-[#141B34] border-[#1C264F] flex flex-col justify-between hover:border-[#E8546A]/40 transition-all ${
                  service.isArchived ? 'opacity-50' : ''
                }`}
              >
                <div>
                  {/* Top badges & color indicator */}
                  <div className="flex items-start justify-between gap-3 mb-3">
                    <div className="flex items-center gap-2">
                      <div 
                        className="w-3.5 h-3.5 rounded-full flex-shrink-0"
                        style={{ backgroundColor: service.colorHex || '#E8546A' }}
                      />
                      <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-[#1C264F] text-[#A2B4D6]">
                        {service.categoryName || 'General'}
                      </span>
                    </div>

                    <div className="flex items-center gap-1.5">
                      {service.isPublished ? (
                        <span className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 flex items-center gap-1">
                          <Globe className="w-2.5 h-2.5" /> Published
                        </span>
                      ) : (
                        <span className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-amber-500/10 text-amber-400 border border-amber-500/20">
                          Draft
                        </span>
                      )}

                      {!service.isActive && (
                        <span className="text-[10px] font-medium px-2 py-0.5 rounded-full bg-rose-500/10 text-rose-400 border border-rose-500/20">
                          Inactive
                        </span>
                      )}
                    </div>
                  </div>

                  <h3 className="text-base font-bold text-[#F4F6FA] font-heading line-clamp-1">{service.name}</h3>
                  <p className="text-xs text-[#7E88A8] mt-1 line-clamp-2 min-h-[32px]">
                    {service.description || 'No description provided.'}
                  </p>

                  {/* Timing & Buffer Breakdown */}
                  <div className="mt-4 p-3 bg-[#0B0F1E]/80 rounded-xl border border-[#1C264F] space-y-2">
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-[#7E88A8] flex items-center gap-1.5">
                        <Clock className="w-3.5 h-3.5 text-[#A2B4D6]" /> Customer Duration:
                      </span>
                      <span className="font-semibold text-[#F4F6FA]">{service.durationMinutes} min</span>
                    </div>

                    {(service.bufferBeforeMinutes > 0 || service.bufferAfterMinutes > 0) && (
                      <div className="flex items-center justify-between text-[11px] text-[#A2B4D6] pt-1 border-t border-[#1C264F]/50">
                        <span>Buffers ({service.bufferBeforeMinutes}m prep / {service.bufferAfterMinutes}m wrap):</span>
                        <span className="text-amber-400">+{service.bufferBeforeMinutes + service.bufferAfterMinutes}m</span>
                      </div>
                    )}

                    <div className="flex items-center justify-between text-[11px] font-medium text-[#7E88A8] pt-1 border-t border-[#1C264F]/50">
                      <span>Total Reserved Occupancy:</span>
                      <span className="text-[#F4F6FA] font-bold">{service.totalDurationMinutes} min</span>
                    </div>
                  </div>

                  {/* Resource & Staff Tags */}
                  <div className="mt-3 flex flex-wrap gap-2 text-[11px]">
                    {service.requiresResource && (
                      <span className="px-2 py-0.5 rounded-md bg-amber-500/10 text-amber-300 border border-amber-500/20 flex items-center gap-1">
                        <Box className="w-3 h-3" />
                        {service.requiredResourceName ? service.requiredResourceName : `Requires ${service.requiredResourceType || 'Room'}`}
                      </span>
                    )}

                    <span className="px-2 py-0.5 rounded-md bg-[#1C264F]/50 text-[#A2B4D6] flex items-center gap-1">
                      <User className="w-3 h-3" /> {assignedStaffCount} Staff
                    </span>

                    <span className="px-2 py-0.5 rounded-md bg-[#1C264F]/50 text-[#A2B4D6] flex items-center gap-1">
                      <Building2 className="w-3 h-3" /> {assignedLocationCount} Locations
                    </span>
                  </div>
                </div>

                {/* Bottom Action Footer */}
                <div className="mt-5 pt-3 border-t border-[#1C264F] flex items-center justify-between">
                  <div className="flex items-baseline gap-1">
                    <span className="text-xs text-[#7E88A8]">{service.currency}</span>
                    <span className="text-lg font-bold text-[#F4F6FA]">
                      {Number(service.price).toFixed(2)}
                    </span>
                  </div>

                  <div className="flex items-center gap-1">
                    <button
                      type="button"
                      title={service.isPublished ? 'Unpublish from Storefront' : 'Publish to Storefront'}
                      onClick={() => handleTogglePublish(service)}
                      className="p-1.5 rounded-lg text-[#7E88A8] hover:text-[#F4F6FA] hover:bg-[#1C264F] transition-colors"
                    >
                      {service.isPublished ? <Eye className="w-4 h-4 text-emerald-400" /> : <EyeOff className="w-4 h-4 text-[#7E88A8]" />}
                    </button>

                    <button
                      type="button"
                      title={service.isActive ? 'Deactivate Service' : 'Activate Service'}
                      onClick={() => handleToggleActive(service)}
                      className="p-1.5 rounded-lg text-[#7E88A8] hover:text-[#F4F6FA] hover:bg-[#1C264F] transition-colors"
                    >
                      {service.isActive ? <CheckCircle2 className="w-4 h-4 text-emerald-400" /> : <XCircle className="w-4 h-4 text-rose-400" />}
                    </button>

                    <button
                      type="button"
                      title="Duplicate Service"
                      onClick={() => handleDuplicateService(service)}
                      className="p-1.5 rounded-lg text-[#7E88A8] hover:text-[#F4F6FA] hover:bg-[#1C264F] transition-colors"
                    >
                      <Copy className="w-4 h-4" />
                    </button>

                    <button
                      type="button"
                      title="Edit Service"
                      onClick={() => handleOpenEditService(service)}
                      className="p-1.5 rounded-lg text-[#7E88A8] hover:text-[#F4F6FA] hover:bg-[#1C264F] transition-colors"
                    >
                      <Edit2 className="w-4 h-4" />
                    </button>

                    <button
                      type="button"
                      title="Archive Service"
                      onClick={() => setArchivingServiceId(service.id)}
                      className="p-1.5 rounded-lg text-rose-400/70 hover:text-rose-400 hover:bg-[#1C264F] transition-colors"
                    >
                      <Archive className="w-4 h-4" />
                    </button>
                  </div>
                </div>
              </Card>
            );
          })}
        </div>
      )}

      {/* --- CREATE / EDIT SERVICE MODAL --- */}
      <Modal
        isOpen={isServiceModalOpen}
        onClose={() => setIsServiceModalOpen(false)}
        title={editingService ? `Edit Service: ${editingService.name}` : 'Create New Service'}
        maxWidth="max-w-2xl"
      >
        <form onSubmit={handleSaveService} className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-[#A2B4D6] mb-1">Service Name *</label>
              <Input
                required
                value={serviceForm.name}
                onChange={(e) => setServiceForm({ ...serviceForm, name: e.target.value })}
                placeholder="e.g. Signature Facial & Peel"
              />
            </div>

            <div>
              <label className="block text-xs font-medium text-[#A2B4D6] mb-1">Category *</label>
              <select
                required
                value={serviceForm.categoryId}
                onChange={(e) => setServiceForm({ ...serviceForm, categoryId: e.target.value })}
                className="w-full bg-[#0B0F1E] border border-[#1C264F] rounded-xl px-3 py-2.5 text-sm text-[#F4F6FA] focus:border-[#E8546A] outline-none cursor-pointer"
              >
                <option value="" disabled>Select category...</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>{c.name}</option>
                ))}
              </select>
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-[#A2B4D6] mb-1">Description</label>
            <textarea
              value={serviceForm.description}
              onChange={(e) => setServiceForm({ ...serviceForm, description: e.target.value })}
              rows={2}
              className="w-full bg-[#0B0F1E] border border-[#1C264F] rounded-xl p-2.5 text-sm text-[#F4F6FA] focus:border-[#E8546A] outline-none"
              placeholder="What does this service include? Any preparation instructions?"
            />
          </div>

          {/* Duration & Buffer Architecture */}
          <div className="p-4 bg-[#0B0F1E] rounded-xl border border-[#1C264F] space-y-3">
            <h4 className="text-xs font-bold text-[#F4F6FA] uppercase tracking-wider flex items-center gap-2">
              <Clock className="w-4 h-4 text-[#E8546A]" /> Appointment Duration &amp; Buffer Schedule
            </h4>

            <div className="grid grid-cols-3 gap-3">
              <div>
                <label className="block text-[11px] text-[#A2B4D6] mb-1">Appointment Time (min) *</label>
                <Input
                  type="number"
                  min="1"
                  required
                  value={serviceForm.durationMinutes}
                  onChange={(e) => setServiceForm({ ...serviceForm, durationMinutes: Number(e.target.value) })}
                />
                <span className="text-[10px] text-[#7E88A8]">Customer sees this</span>
              </div>

              <div>
                <label className="block text-[11px] text-[#A2B4D6] mb-1">Buffer Before (min)</label>
                <Input
                  type="number"
                  min="0"
                  value={serviceForm.bufferBeforeMinutes}
                  onChange={(e) => setServiceForm({ ...serviceForm, bufferBeforeMinutes: Number(e.target.value) })}
                />
                <span className="text-[10px] text-[#7E88A8]">Room/prep time</span>
              </div>

              <div>
                <label className="block text-[11px] text-[#A2B4D6] mb-1">Buffer After (min)</label>
                <Input
                  type="number"
                  min="0"
                  value={serviceForm.bufferAfterMinutes}
                  onChange={(e) => setServiceForm({ ...serviceForm, bufferAfterMinutes: Number(e.target.value) })}
                />
                <span className="text-[10px] text-[#7E88A8]">Cleanup/turnover</span>
              </div>
            </div>

            <div className="p-2.5 bg-[#141B34] rounded-lg border border-[#1C264F] flex items-center justify-between text-xs">
              <span className="text-[#A2B4D6]">Total Occupancy Window reserved on calendar:</span>
              <span className="text-[#E8546A] font-bold text-sm">{totalOccupancyCalculated} minutes</span>
            </div>
          </div>

          {/* Pricing & Tag Color */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="block text-xs font-medium text-[#A2B4D6] mb-1">Price *</label>
              <Input
                type="number"
                step="0.01"
                min="0"
                required
                value={serviceForm.price}
                onChange={(e) => setServiceForm({ ...serviceForm, price: Number(e.target.value) })}
              />
            </div>

            <div>
              <label className="block text-xs font-medium text-[#A2B4D6] mb-1">Currency</label>
              <Input
                maxLength={3}
                value={serviceForm.currency}
                onChange={(e) => setServiceForm({ ...serviceForm, currency: e.target.value.toUpperCase() })}
              />
            </div>

            <div>
              <label className="block text-xs font-medium text-[#A2B4D6] mb-1">Color Code</label>
              <div className="flex items-center gap-2">
                <input
                  type="color"
                  value={serviceForm.colorHex}
                  onChange={(e) => setServiceForm({ ...serviceForm, colorHex: e.target.value })}
                  className="w-10 h-10 rounded cursor-pointer bg-transparent border-0"
                />
                <span className="text-xs text-[#F4F6FA] font-mono">{serviceForm.colorHex}</span>
              </div>
            </div>
          </div>

          {/* Staff Assignment */}
          <div>
            <label className="block text-xs font-medium text-[#A2B4D6] mb-1">
              Eligible Staff Members ({serviceForm.staffIds.length} selected)
            </label>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-2 max-h-32 overflow-y-auto p-2 bg-[#0B0F1E] rounded-xl border border-[#1C264F]">
              {staffList.map((st) => {
                const checked = serviceForm.staffIds.includes(st.id);
                return (
                  <label key={st.id} className="flex items-center gap-2 text-xs text-[#F4F6FA] cursor-pointer">
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setServiceForm({ ...serviceForm, staffIds: [...serviceForm.staffIds, st.id] });
                        } else {
                          setServiceForm({ ...serviceForm, staffIds: serviceForm.staffIds.filter(id => id !== st.id) });
                        }
                      }}
                      className="rounded bg-[#141B34] border-[#1C264F] text-[#E8546A]"
                    />
                    <span className="truncate">{st.name}</span>
                  </label>
                );
              })}
            </div>
          </div>

          {/* Location Assignment */}
          <div>
            <label className="block text-xs font-medium text-[#A2B4D6] mb-1">
              Applicable Business Locations ({serviceForm.locationIds.length} selected)
            </label>
            <div className="grid grid-cols-2 gap-2 max-h-24 overflow-y-auto p-2 bg-[#0B0F1E] rounded-xl border border-[#1C264F]">
              {locations.map((loc) => {
                const checked = serviceForm.locationIds.includes(loc.id);
                return (
                  <label key={loc.id} className="flex items-center gap-2 text-xs text-[#F4F6FA] cursor-pointer">
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={(e) => {
                        if (e.target.checked) {
                          setServiceForm({ ...serviceForm, locationIds: [...serviceForm.locationIds, loc.id] });
                        } else {
                          setServiceForm({ ...serviceForm, locationIds: serviceForm.locationIds.filter(id => id !== loc.id) });
                        }
                      }}
                      className="rounded bg-[#141B34] border-[#1C264F] text-[#E8546A]"
                    />
                    <span className="truncate">{loc.name}</span>
                  </label>
                );
              })}
            </div>
          </div>

          {/* Bookable Resource Requirement */}
          <div className="p-3 bg-[#0B0F1E] rounded-xl border border-[#1C264F] space-y-3">
            <label className="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                checked={serviceForm.requiresResource}
                onChange={(e) => setServiceForm({ ...serviceForm, requiresResource: e.target.checked })}
                className="rounded bg-[#141B34] border-[#1C264F] text-[#E8546A]"
              />
              <span className="text-xs font-bold text-[#F4F6FA]">
                Requires Bookable Resource (Room, Chair, Device, Bay)
              </span>
            </label>

            {serviceForm.requiresResource && (
              <div className="grid grid-cols-2 gap-3 pt-2 border-t border-[#1C264F]/50">
                <div>
                  <label className="block text-[11px] text-[#A2B4D6] mb-1">Required Resource Type</label>
                  <select
                    value={serviceForm.requiredResourceType}
                    onChange={(e) => setServiceForm({ ...serviceForm, requiredResourceType: e.target.value })}
                    className="w-full bg-[#0B0F1E] border border-[#1C264F] rounded-xl px-3 py-2 text-xs text-[#F4F6FA] focus:border-[#E8546A] outline-none cursor-pointer"
                  >
                    <option value="Room">Treatment Room</option>
                    <option value="Chair">Styling Chair / Station</option>
                    <option value="Machine">Specialized Machine / Laser</option>
                    <option value="Bay">Service Bay / Bed</option>
                  </select>
                </div>

                <div>
                  <label className="block text-[11px] text-[#A2B4D6] mb-1">Specific Assigned Resource (Optional)</label>
                  <select
                    value={serviceForm.requiredResourceId}
                    onChange={(e) => setServiceForm({ ...serviceForm, requiredResourceId: e.target.value })}
                    className="w-full bg-[#0B0F1E] border border-[#1C264F] rounded-xl px-3 py-2 text-xs text-[#F4F6FA] focus:border-[#E8546A] outline-none cursor-pointer"
                  >
                    <option value="">Any available {serviceForm.requiredResourceType}</option>
                    {resources.map((r) => (
                      <option key={r.id} value={r.id}>{r.name} ({r.type})</option>
                    ))}
                  </select>
                </div>
              </div>
            )}
          </div>

          {/* Toggles */}
          <div className="flex flex-wrap items-center gap-6 pt-2">
            <label className="flex items-center gap-2 text-xs text-[#F4F6FA] cursor-pointer">
              <input
                type="checkbox"
                checked={serviceForm.isOnlineBookingEnabled}
                onChange={(e) => setServiceForm({ ...serviceForm, isOnlineBookingEnabled: e.target.checked })}
                className="rounded bg-[#141B34] border-[#1C264F] text-[#E8546A]"
              />
              Enable Online Booking
            </label>

            <label className="flex items-center gap-2 text-xs text-[#F4F6FA] cursor-pointer">
              <input
                type="checkbox"
                checked={serviceForm.isPublished}
                onChange={(e) => setServiceForm({ ...serviceForm, isPublished: e.target.checked })}
                className="rounded bg-[#141B34] border-[#1C264F] text-[#E8546A]"
              />
              Publish to Public Storefront
            </label>

            <label className="flex items-center gap-2 text-xs text-[#F4F6FA] cursor-pointer">
              <input
                type="checkbox"
                checked={serviceForm.isActive}
                onChange={(e) => setServiceForm({ ...serviceForm, isActive: e.target.checked })}
                className="rounded bg-[#141B34] border-[#1C264F] text-[#E8546A]"
              />
              Active Status
            </label>
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-3 pt-4 border-t border-[#1C264F]">
            <Button variant="outline" type="button" onClick={() => setIsServiceModalOpen(false)}>
              Cancel
            </Button>
            <Button variant="primary" type="submit">
              {editingService ? 'Save Changes' : 'Create Service'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* --- CATEGORIES MODAL --- */}
      <Modal
        isOpen={isCategoryModalOpen}
        onClose={() => setIsCategoryModalOpen(false)}
        title="Manage Service Categories"
        maxWidth="max-w-xl"
      >
        <div className="space-y-6">
          {/* Add / Edit Category Form */}
          <form onSubmit={handleSaveCategory} className="p-4 bg-[#0B0F1E] rounded-xl border border-[#1C264F] space-y-3">
            <h4 className="text-xs font-bold text-[#F4F6FA]">
              {editingCategory ? `Edit Category: ${editingCategory.name}` : 'Add New Category'}
            </h4>
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
              <div className="sm:col-span-2">
                <Input
                  placeholder="Category Name"
                  required
                  value={categoryForm.name}
                  onChange={(e) => setCategoryForm({ ...categoryForm, name: e.target.value })}
                />
              </div>
              <div>
                <Input
                  type="number"
                  placeholder="Sort Order"
                  value={categoryForm.sortOrder}
                  onChange={(e) => setCategoryForm({ ...categoryForm, sortOrder: Number(e.target.value) })}
                />
              </div>
            </div>
            <Input
              placeholder="Description (optional)"
              value={categoryForm.description}
              onChange={(e) => setCategoryForm({ ...categoryForm, description: e.target.value })}
            />
            <div className="flex justify-end gap-2">
              {editingCategory && (
                <Button variant="ghost" size="sm" type="button" onClick={() => {
                  setEditingCategory(null);
                  setCategoryForm({ name: '', description: '', sortOrder: categories.length + 1 });
                }}>
                  Cancel Edit
                </Button>
              )}
              <Button variant="primary" size="sm" type="submit">
                {editingCategory ? 'Update Category' : 'Add Category'}
              </Button>
            </div>
          </form>

          {/* Categories List */}
          <div className="space-y-2">
            <h4 className="text-xs font-bold text-[#A2B4D6]">Existing Categories ({categories.length})</h4>
            <div className="space-y-2 max-h-60 overflow-y-auto">
              {categories.map((cat) => (
                <div key={cat.id} className="p-3 bg-[#141B34] border border-[#1C264F] rounded-xl flex items-center justify-between">
                  <div>
                    <p className="text-sm font-semibold text-[#F4F6FA]">{cat.name}</p>
                    <p className="text-xs text-[#7E88A8]">
                      Order: {cat.sortOrder} &bull; {cat.servicesCount || 0} services
                    </p>
                  </div>
                  <div className="flex items-center gap-1">
                    <button
                      type="button"
                      className="p-1.5 rounded-lg text-[#A2B4D6] hover:text-[#F4F6FA] hover:bg-[#1C264F] transition-colors"
                      onClick={() => {
                        setEditingCategory(cat);
                        setCategoryForm({
                          name: cat.name,
                          description: cat.description || '',
                          sortOrder: cat.sortOrder,
                        });
                      }}
                    >
                      <Edit2 className="w-3.5 h-3.5" />
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDeleteCategory(cat.id)}
                      className="p-1.5 rounded-lg text-rose-400 hover:text-rose-300 hover:bg-[#1C264F] transition-colors"
                    >
                      <Archive className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </Modal>

      {/* --- BOOKABLE RESOURCES MODAL --- */}
      <Modal
        isOpen={isResourceModalOpen}
        onClose={() => setIsResourceModalOpen(false)}
        title="Bookable Resources &amp; Equipment"
        maxWidth="max-w-xl"
      >
        <div className="space-y-6">
          <p className="text-xs text-[#7E88A8]">
            Configure physical rooms, chairs, and machines required by your services. The availability engine automatically locks these to prevent double bookings.
          </p>

          {/* Add Resource Form */}
          <form onSubmit={handleCreateResource} className="p-4 bg-[#0B0F1E] rounded-xl border border-[#1C264F] space-y-3">
            <h4 className="text-xs font-bold text-[#F4F6FA]">Add New Resource</h4>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-[11px] text-[#A2B4D6] mb-1">Resource Name *</label>
                <Input
                  required
                  placeholder="e.g. Treatment Room 1"
                  value={resourceForm.name}
                  onChange={(e) => setResourceForm({ ...resourceForm, name: e.target.value })}
                />
              </div>
              <div>
                <label className="block text-[11px] text-[#A2B4D6] mb-1">Type</label>
                <select
                  value={resourceForm.type}
                  onChange={(e) => setResourceForm({ ...resourceForm, type: e.target.value })}
                  className="w-full bg-[#0B0F1E] border border-[#1C264F] rounded-xl px-3 py-2 text-xs text-[#F4F6FA] focus:border-[#E8546A] outline-none cursor-pointer"
                >
                  <option value="Room">Treatment Room</option>
                  <option value="Chair">Styling Chair</option>
                  <option value="Machine">Machine / Device</option>
                  <option value="Bay">Service Bay</option>
                </select>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-[11px] text-[#A2B4D6] mb-1">Location</label>
                <select
                  value={resourceForm.locationId}
                  onChange={(e) => setResourceForm({ ...resourceForm, locationId: e.target.value })}
                  className="w-full bg-[#0B0F1E] border border-[#1C264F] rounded-xl px-3 py-2 text-xs text-[#F4F6FA] focus:border-[#E8546A] outline-none cursor-pointer"
                >
                  <option value="">All / Primary Location</option>
                  {locations.map((loc) => (
                    <option key={loc.id} value={loc.id}>{loc.name}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-[11px] text-[#A2B4D6] mb-1">Capacity</label>
                <Input
                  type="number"
                  min="1"
                  value={resourceForm.capacity}
                  onChange={(e) => setResourceForm({ ...resourceForm, capacity: Number(e.target.value) })}
                />
              </div>
            </div>

            <div className="flex justify-end pt-2">
              <Button variant="primary" size="sm" type="submit">
                <Plus className="w-3.5 h-3.5 mr-1" /> Add Resource
              </Button>
            </div>
          </form>

          {/* Existing Resources */}
          <div className="space-y-2">
            <h4 className="text-xs font-bold text-[#A2B4D6]">Configured Resources ({resources.length})</h4>
            <div className="space-y-2 max-h-52 overflow-y-auto">
              {resources.length === 0 ? (
                <p className="text-xs text-[#7E88A8] text-center py-4">No bookable resources created yet.</p>
              ) : (
                resources.map((res) => (
                  <div key={res.id} className="p-3 bg-[#141B34] border border-[#1C264F] rounded-xl flex items-center justify-between">
                    <div>
                      <p className="text-sm font-semibold text-[#F4F6FA]">{res.name}</p>
                      <p className="text-xs text-[#7E88A8]">
                        Type: {res.type} &bull; Capacity: {res.capacity} {res.locationName ? `&bull; ${res.locationName}` : ''}
                      </p>
                    </div>
                    <span className="text-[10px] px-2 py-0.5 rounded-full bg-emerald-500/10 text-emerald-400">
                      Active
                    </span>
                  </div>
                ))
              )}
            </div>
          </div>
        </div>
      </Modal>

      {/* --- CONFIRM ARCHIVE DIALOG --- */}
      <ConfirmDialog
        isOpen={Boolean(archivingServiceId)}
        onClose={() => setArchivingServiceId(null)}
        onConfirm={handleConfirmArchive}
        title="Archive Service"
        message="Are you sure you want to archive this service? Archived services are deactivated and hidden from online booking, but existing bookings will remain preserved."
        confirmText="Archive Service"
        isDanger={true}
      />
    </div>
  );
};
