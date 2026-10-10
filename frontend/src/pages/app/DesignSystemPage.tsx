import React, { useState } from 'react';
import {
  Sparkles,
  Sliders,
  Compass,
  Calendar as CalendarIcon,
  ShoppingBag,
  Database,
  Layers,
  Bell,
  CheckCircle,
  Eye,
  Laptop,
  Smartphone,
  ShieldCheck,
  Building2,
  DollarSign,
  User,
  RefreshCw,
  Search,
} from 'lucide-react';
import { Button, IconButton, ButtonGroup } from '../../components/ui/Button';

// 1. Navigation Primitives
import {
  Breadcrumbs,
  NotificationBell,
  OrganizationSwitcher,
  LocationSwitcher,
  UserMenu,
} from '../../components/navigation';

// 2. Forms & Inputs
import {
  Input,
  Textarea,
  Select,
  Switch,
  SearchInput,
  MoneyInput,
  PhoneInput,
  Combobox,
  DatePicker,
  TimePicker,
} from '../../components/forms';

// 3. Feedback & Overlays
import {
  Alert,
  Modal,
  Drawer,
  ConfirmDialog,
  Toast,
} from '../../components/feedback';

// 4. Data Display & Tables
import {
  Card,
  MetricCard,
  Badge,
  StatusBadge,
  Avatar,
  Skeleton,
  EmptyState,
  ErrorState,
  DataTable,
  Stat,
  Pagination,
} from '../../components/data-display';

// 5. Scheduling
import {
  Slot,
  SlotGrid,
  AppointmentCard,
  HoldBanner,
  DateStrip,
  Calendar,
} from '../../components/scheduling';

// 6. Marketplace
import {
  ProviderCard,
  CategoryCard,
  SearchBar,
  LocationPicker,
  FilterBar,
  SortSelector,
  MapView,
  MapMarker,
  ProviderPreview,
} from '../../components/marketplace';

// 7. Commerce
import {
  ProductCard,
  CartItem,
  CartSummary,
  OrderStatus,
  PaymentSummary,
} from '../../components/commerce';

type DensityMode = 'customer' | 'provider' | 'admin';
type TabSection = 'all' | 'nav' | 'forms' | 'feedback' | 'data' | 'scheduling' | 'marketplace' | 'commerce' | 'contract';

export const DesignSystemPage: React.FC = () => {
  // Density Spectrum (Section 108)
  const [density, setDensity] = useState<DensityMode>('customer');
  const [activeTab, setActiveTab] = useState<TabSection>('all');

  // Interactive dialog states
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  // Form states
  const [switchVal, setSwitchVal] = useState(true);
  const [selectedSlot, setSelectedSlot] = useState<string | null>('10:00');
  const [moneyVal, setMoneyVal] = useState<number>(125.5);
  const [phoneVal, setPhoneVal] = useState('+1 415 555 2671');
  const [comboboxVal, setComboboxVal] = useState<string>('hair-cut');
  const [searchVal, setSearchVal] = useState('');
  const [dateVal, setDateVal] = useState('2026-10-10');
  const [timeVal, setTimeVal] = useState('14:30');
  const [searchQuery, setSearchQuery] = useState('');

  // Marketplace states
  const [selectedProviderId, setSelectedProviderId] = useState<string | null>('p-1');
  const [hoveredProviderId, setHoveredProviderId] = useState<string | null>(null);
  const [mapErrorSimulated, setMapErrorSimulated] = useState(false);

  // Commerce states
  const [productQuantity, setProductQuantity] = useState(1);
  const [cartItemsCount, setCartItemsCount] = useState(2);

  // Density CSS container modifier
  const densityContainerClass = {
    customer: 'space-y-12 p-2 sm:p-6 max-w-6xl',
    provider: 'space-y-8 p-1 sm:p-4 max-w-7xl text-sm',
    admin: 'space-y-6 p-0 sm:p-2 max-w-full text-xs font-mono',
  }[density];

  return (
    <div className={`mx-auto pb-24 ${densityContainerClass} transition-all duration-200`}>
      {/* Toast Alert */}
      {toastMessage && (
        <Toast
          title="Notification"
          message={toastMessage}
          variant="success"
          onClose={() => setToastMessage(null)}
          durationMs={3500}
        />
      )}

      {/* Header & Density Spectrum Bar (Section 108) */}
      <header className="border-b border-[#212638] pb-6 space-y-4">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="space-y-1.5">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[#181D2C] border border-[#212638] text-xs font-mono text-[#E8546A]">
              <Sparkles className="w-3.5 h-3.5" /> SECTION 103-110 DESIGN SYSTEM MATRIX
            </div>
            <h1 className="font-heading text-3xl sm:text-4xl font-extrabold text-white">
              Bookline Universal UI Kit
            </h1>
            <p className="text-xs sm:text-sm text-[#7E88A8]">
              Standardized components across Navigation, Forms, Feedback, Data, Scheduling, Marketplace, and Commerce.
            </p>
          </div>

          {/* Section 108: Visual Density Spectrum Switcher */}
          <div className="bg-[#111520] border border-[#212638] p-1.5 rounded-2xl flex items-center gap-1 shrink-0 self-start md:self-auto">
            <span className="text-[11px] font-mono text-[#7E88A8] px-2 uppercase flex items-center gap-1">
              <Sliders className="w-3.5 h-3.5 text-[#E8546A]" /> Density:
            </span>
            <button
              type="button"
              onClick={() => setDensity('customer')}
              className={`px-3 py-1.5 rounded-xl text-xs font-medium transition-all flex items-center gap-1.5 ${
                density === 'customer'
                  ? 'bg-[#E8546A] text-white shadow-md shadow-[#E8546A]/20'
                  : 'text-[#7E88A8] hover:text-white hover:bg-[#181D2C]'
              }`}
            >
              <Smartphone className="w-3.5 h-3.5" />
              <span>Customer</span>
            </button>
            <button
              type="button"
              onClick={() => setDensity('provider')}
              className={`px-3 py-1.5 rounded-xl text-xs font-medium transition-all flex items-center gap-1.5 ${
                density === 'provider'
                  ? 'bg-[#E8546A] text-white shadow-md shadow-[#E8546A]/20'
                  : 'text-[#7E88A8] hover:text-white hover:bg-[#181D2C]'
              }`}
            >
              <Laptop className="w-3.5 h-3.5" />
              <span>Provider</span>
            </button>
            <button
              type="button"
              onClick={() => setDensity('admin')}
              className={`px-3 py-1.5 rounded-xl text-xs font-medium transition-all flex items-center gap-1.5 ${
                density === 'admin'
                  ? 'bg-[#E8546A] text-white shadow-md shadow-[#E8546A]/20'
                  : 'text-[#7E88A8] hover:text-white hover:bg-[#181D2C]'
              }`}
            >
              <Database className="w-3.5 h-3.5" />
              <span>Admin</span>
            </button>
          </div>
        </div>

        {/* Section 108 Density Banner */}
        <div className="p-3 rounded-xl bg-[#181D2C] border border-[#212638] text-xs flex items-center justify-between flex-wrap gap-2">
          <div className="flex items-center gap-2">
            <span className="w-2 h-2 rounded-full bg-[#34D399] animate-pulse" />
            <span className="text-[#ECEFFE] font-semibold">Active Density Mode:</span>
            <span className="text-[#E8546A] font-bold uppercase">{density}</span>
          </div>
          <p className="text-[11px] text-[#7E88A8]">
            {density === 'customer' && 'Spacious discovery, large imagery, comfortable touch targets (>=44px), breathing room.'}
            {density === 'provider' && 'High-information density, calendar multi-columns, fast keyboard triggers, quick status updates.'}
            {density === 'admin' && 'Highest density, dense table grids, audit logs, fine-grained moderation controls.'}
          </p>
        </div>

        {/* Filter Navigation Tabs */}
        <div className="flex items-center gap-2 overflow-x-auto pb-1 scrollbar-none">
          {[
            { key: 'all', label: 'All 7 Domains' },
            { key: 'nav', label: '1. Navigation' },
            { key: 'forms', label: '2. Forms & Inputs' },
            { key: 'feedback', label: '3. Feedback & Modals' },
            { key: 'data', label: '4. Data & Tables' },
            { key: 'scheduling', label: '5. Scheduling' },
            { key: 'marketplace', label: '6. Marketplace' },
            { key: 'commerce', label: '7. Commerce' },
            { key: 'contract', label: 'Section 110 Contract' },
          ].map((tab) => (
            <button
              key={tab.key}
              type="button"
              onClick={() => setActiveTab(tab.key as TabSection)}
              className={`px-3 py-1.5 rounded-xl text-xs font-semibold whitespace-nowrap transition-colors ${
                activeTab === tab.key
                  ? 'bg-[#212638] text-white border border-[#313A52]'
                  : 'text-[#7E88A8] hover:text-white hover:bg-[#181D2C]'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </header>

      {/* =========================================================================
       * 1. NAVIGATION PRIMITIVES (Section 109)
       * ========================================================================= */}
      {(activeTab === 'all' || activeTab === 'nav') && (
        <section className="space-y-4">
          <div className="flex items-center justify-between border-b border-[#212638] pb-2">
            <h2 className="font-heading text-xl font-bold text-white flex items-center gap-2">
              <Compass className="w-5 h-5 text-[#E8546A]" /> 1. Navigation Primitives
            </h2>
            <span className="text-[10px] font-mono text-[#7E88A8]">AppShell, Topbar, Breadcrumbs, Switchers</span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="p-4 rounded-2xl bg-[#111520] border border-[#212638] space-y-3">
              <h4 className="text-xs font-bold uppercase tracking-wider text-[#7E88A8]">
                Breadcrumbs & Indicators
              </h4>
              <Breadcrumbs
                items={[
                  { label: 'Marketplace', path: '/explore' },
                  { label: 'Hair & Styling', path: '/explore?category=hair' },
                  { label: 'Aura Luxury Studio' },
                ]}
              />
              <div className="flex items-center gap-4 pt-2">
                <NotificationBell unreadCount={3} onClick={() => setToastMessage('Opened Notification Drawer')} />
                <NotificationBell unreadCount={0} />
              </div>
            </div>

            <div className="p-4 rounded-2xl bg-[#111520] border border-[#212638] space-y-3">
              <h4 className="text-xs font-bold uppercase tracking-wider text-[#7E88A8]">
                Organization & Location Switchers
              </h4>
              <div className="flex flex-wrap gap-3">
                <OrganizationSwitcher
                  organizations={[
                    { slug: 'aura', name: 'Aura Aesthetics Group', role: 'Owner' },
                    { slug: 'solace', name: 'Solace Wellness Co', role: 'Manager' },
                  ]}
                  activeOrg="aura"
                  onSelectOrg={(slug) => setToastMessage(`Switched to Org: ${slug}`)}
                />
                <LocationSwitcher
                  locations={[
                    'Downtown Flagship (101 Market St)',
                    'Uptown Spa Branch (404 Grand Ave)',
                  ]}
                  activeLocation="Downtown Flagship (101 Market St)"
                  onSelectLocation={(loc) => setToastMessage(`Switched location to: ${loc}`)}
                />
              </div>
            </div>
          </div>
        </section>
      )}

      {/* =========================================================================
       * 2. FORMS & ADVANCED INPUTS (Section 109)
       * ========================================================================= */}
      {(activeTab === 'all' || activeTab === 'forms') && (
        <section className="space-y-4">
          <div className="flex items-center justify-between border-b border-[#212638] pb-2">
            <h2 className="font-heading text-xl font-bold text-white flex items-center gap-2">
              <Sliders className="w-5 h-5 text-[#E8546A]" /> 2. Form Inputs & Advanced Controls
            </h2>
            <span className="text-[10px] font-mono text-[#7E88A8]">Money, Phone, Combobox, Pickers</span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4">
            <Input label="Standard Input" placeholder="Provider or service name..." />
            <SearchInput
              value={searchVal}
              onChange={setSearchVal}
              placeholder="Quick search inventory..."
              onClear={() => setSearchVal('')}
            />
            <MoneyInput
              label="Money Input (Currency Locked)"
              value={moneyVal}
              onChange={(val) => setMoneyVal(val ?? 0)}
              currency="USD"
            />
            <PhoneInput
              label="Phone Input (E.164 Clean)"
              value={phoneVal}
              onChange={setPhoneVal}
            />
            <Combobox
              label="Service Combobox (Filterable)"
              value={comboboxVal}
              onChange={(val) => setComboboxVal(String(val))}
              options={[
                { value: 'hair-cut', label: 'Classic Precision Cut ($55)' },
                { value: 'balayage', label: 'Custom Balayage & Glaze ($180)' },
                { value: 'spa-massage', label: 'Deep Tissue Massage ($120)' },
              ]}
            />
            <div className="grid grid-cols-2 gap-2">
              <DatePicker
                label="Date Picker"
                value={dateVal}
                onChange={setDateVal}
              />
              <TimePicker
                label="Time Picker"
                value={timeVal}
                onChange={setTimeVal}
              />
            </div>
          </div>

          <div className="p-4 rounded-xl bg-[#111520] border border-[#212638] flex items-center justify-between flex-wrap gap-4">
            <Switch
              checked={switchVal}
              onChange={setSwitchVal}
              label="Online Appointment Prepayments Required (Section 94)"
            />
            <span className="text-xs text-[#7E88A8]">
              Active: <strong className="text-white">{switchVal ? 'Enforced' : 'Optional'}</strong>
            </span>
          </div>
        </section>
      )}

      {/* =========================================================================
       * 3. FEEDBACK & MODALS (Section 109)
       * ========================================================================= */}
      {(activeTab === 'all' || activeTab === 'feedback') && (
        <section className="space-y-4">
          <div className="flex items-center justify-between border-b border-[#212638] pb-2">
            <h2 className="font-heading text-xl font-bold text-white flex items-center gap-2">
              <ShieldCheck className="w-5 h-5 text-[#E8546A]" /> 3. Feedback, Overlays & Banners
            </h2>
            <span className="text-[10px] font-mono text-[#7E88A8]">Toast, Alert, Modal, Drawer, Confirm</span>
          </div>

          <div className="flex flex-wrap gap-3">
            <Button variant="primary" onClick={() => setIsModalOpen(true)}>
              Open Interactive Modal
            </Button>
            <Button variant="secondary" onClick={() => setIsDrawerOpen(true)}>
              Open Detail Drawer
            </Button>
            <Button variant="danger" onClick={() => setIsConfirmOpen(true)}>
              Open Confirm Dialog
            </Button>
            <Button variant="outline" onClick={() => setToastMessage('Slot held atomically for 5 minutes!')}>
              Trigger Success Toast
            </Button>
          </div>

          <div className="space-y-2">
            <Alert variant="info" title="Section 103 Location Resilience">
              GPS denied falls back to city search; geocoding failure falls back to manual entry; map canvas failure leaves listings operable.
            </Alert>
            <Alert variant="success" title="Section 104 Real Providers State">
              Zero fake providers rendered. Never fabricate mock results after an API error.
            </Alert>
          </div>

          {/* Modal / Drawer / Confirm instances */}
          <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Create New Reservation">
            <div className="space-y-4">
              <Input label="Customer Full Name" placeholder="e.g. Priya Sharma" />
              <MoneyInput label="Estimated Total" value={95} onChange={() => {}} />
              <div className="flex justify-end gap-2 pt-2">
                <Button variant="ghost" onClick={() => setIsModalOpen(false)}>Cancel</Button>
                <Button variant="primary" onClick={() => { setIsModalOpen(false); setToastMessage('Reservation created'); }}>
                  Confirm Booking
                </Button>
              </div>
            </div>
          </Modal>

          <Drawer isOpen={isDrawerOpen} onClose={() => setIsDrawerOpen(false)} title="Audit Trail & Ledger Details">
            <div className="space-y-4 text-xs text-[#7E88A8]">
              <p className="text-white font-semibold">Ledger Transaction #BL-99214</p>
              <div className="p-3 rounded-xl bg-[#181D2C] border border-[#212638] space-y-1 font-mono">
                <p>Status: HELD_IN_ESCROW</p>
                <p>Deposit: $25.00</p>
                <p>Balance: $70.00</p>
              </div>
              <Button variant="primary" className="w-full" onClick={() => setIsDrawerOpen(false)}>
                Close Drawer
              </Button>
            </div>
          </Drawer>

          <ConfirmDialog
            isOpen={isConfirmOpen}
            onClose={() => setIsConfirmOpen(false)}
            onConfirm={() => { setIsConfirmOpen(false); setToastMessage('Action confirmed safely.'); }}
            title="Cancel Appointment Slot?"
            message="This action will release the 5-minute atomic slot hold and notify the client."
            confirmText="Release Slot"
            isDanger
          />
        </section>
      )}

      {/* =========================================================================
       * 4. DATA DISPLAY & TABLES (Section 109)
       * ========================================================================= */}
      {(activeTab === 'all' || activeTab === 'data') && (
        <section className="space-y-4">
          <div className="flex items-center justify-between border-b border-[#212638] pb-2">
            <h2 className="font-heading text-xl font-bold text-white flex items-center gap-2">
              <Database className="w-5 h-5 text-[#E8546A]" /> 4. Data Display, Badges & Tables
            </h2>
            <span className="text-[10px] font-mono text-[#7E88A8]">DataTable, MetricCard, Stats, Badges</span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
            <Stat label="Total Appointments" value="1,428" trend="up" caption="+12.5% vs last month" />
            <Stat label="Gross Service Value" value="$48,920" trend="up" caption="+8.1% vs target" />
            <Stat label="Hold Dropoff Rate" value="3.2%" trend="down" caption="-1.4% improvement" />
            <Stat label="Average Provider Rating" value="4.92" trend="neutral" caption="820 reviews" />
          </div>

          <div className="flex flex-wrap gap-2 items-center">
            <StatusBadge status="Confirmed" />
            <StatusBadge status="Held" />
            <StatusBadge status="CheckedIn" />
            <StatusBadge status="Completed" />
            <StatusBadge status="Cancelled" />
            <Badge variant="primary">Coral Prime</Badge>
            <Badge variant="success">Active</Badge>
            <Badge variant="warning">Holding</Badge>
            <Badge variant="danger">Disputed</Badge>
          </div>

          {/* DataTable component */}
          <div className="rounded-2xl bg-[#111520] border border-[#212638] overflow-hidden">
            <DataTable
              columns={[
                { key: 'client', header: 'Client Name', render: (row: any) => <span className="font-bold text-white">{row.client}</span> },
                { key: 'service', header: 'Service' },
                { key: 'time', header: 'Scheduled Time' },
                { key: 'status', header: 'Status', render: (row: any) => <StatusBadge status={row.status} /> },
                { key: 'amount', header: 'Amount', render: (row: any) => <span className="text-[#34D399] font-mono font-bold">${row.amount}</span> },
              ]}
              data={[
                { id: '1', client: 'Sophia Ramirez', service: 'Precision Cut & Blowout', time: '10:00 AM', status: 'Confirmed', amount: '85.00' },
                { id: '2', client: 'Liam Chen', service: 'Beard Sculpture & Facial', time: '11:15 AM', status: 'Held', amount: '45.00' },
                { id: '3', client: 'Aaliyah Khan', service: 'Full Balayage Highlights', time: '01:30 PM', status: 'CheckedIn', amount: '195.00' },
              ]}
            />
            <div className="p-3 border-t border-[#212638]">
              <Pagination currentPage={1} totalPages={8} onPageChange={() => {}} />
            </div>
          </div>
        </section>
      )}

      {/* =========================================================================
       * 5. SCHEDULING & CALENDAR ENGINE (Section 109)
       * ========================================================================= */}
      {(activeTab === 'all' || activeTab === 'scheduling') && (
        <section className="space-y-4">
          <div className="flex items-center justify-between border-b border-[#212638] pb-2">
            <h2 className="font-heading text-xl font-bold text-white flex items-center gap-2">
              <CalendarIcon className="w-5 h-5 text-[#E8546A]" /> 5. Scheduling, Slots & Hold Banners
            </h2>
            <span className="text-[10px] font-mono text-[#7E88A8]">HoldBanner, DateStrip, SlotGrid, AppointmentBlock</span>
          </div>

          {/* Section 92 / Hold Banner */}
          <HoldBanner
            serviceName="Balayage & Hair Care"
            slotTime="10:00 AM"
            initialSeconds={285}
            onExpire={() => setToastMessage('Hold expired')}
            onProceed={() => setToastMessage('Hold converted to checkout')}
          />

          {/* DateStrip */}
          <DateStrip
            dates={[
              { date: '2026-10-10', dayName: 'Sat', dayNumber: '10', isToday: true },
              { date: '2026-10-11', dayName: 'Sun', dayNumber: '11' },
              { date: '2026-10-12', dayName: 'Mon', dayNumber: '12' },
              { date: '2026-10-13', dayName: 'Tue', dayNumber: '13' },
              { date: '2026-10-14', dayName: 'Wed', dayNumber: '14' },
            ]}
            selectedDate={dateVal}
            onSelectDate={setDateVal}
          />

          {/* Slot Grid Contract */}
          <div className="space-y-2">
            <h4 className="text-xs font-bold uppercase tracking-wider text-[#7E88A8]">
              Real-time Slot State Matrix
            </h4>
            <SlotGrid>
              <Slot time="09:00 AM" state="FREE" onClick={() => setSelectedSlot('09:00 AM')} />
              <Slot time="09:30 AM" state="FREE" onClick={() => setSelectedSlot('09:30 AM')} />
              <Slot
                time="10:00 AM"
                state={selectedSlot === '10:00 AM' ? 'SELECTED' : 'FREE'}
                onClick={() => setSelectedSlot('10:00 AM')}
              />
              <Slot time="10:30 AM" state="HELD" holdTimeRemaining="04:22" />
              <Slot time="11:00 AM" state="BOOKED" />
              <Slot time="11:30 AM" state="UNAVAILABLE" />
            </SlotGrid>
          </div>
        </section>
      )}

      {/* =========================================================================
       * 6. MARKETPLACE & GEO DISCOVERY (Section 103-106, 109)
       * ========================================================================= */}
      {(activeTab === 'all' || activeTab === 'marketplace') && (
        <section className="space-y-4">
          <div className="flex items-center justify-between border-b border-[#212638] pb-2">
            <h2 className="font-heading text-xl font-bold text-white flex items-center gap-2">
              <Building2 className="w-5 h-5 text-[#E8546A]" /> 6. Marketplace & Geo Discovery
            </h2>
            <span className="text-[10px] font-mono text-[#7E88A8]">ProviderCard, LocationPicker, MapView</span>
          </div>

          {/* LocationPicker & SearchBar */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <LocationPicker
              locationName="Central District, San Francisco"
              onOpenSelector={() => setToastMessage('Opened Location Selector')}
              onDetectGPS={() => setToastMessage('Detecting GPS coordinates...')}
            />
            <SearchBar
              query={searchQuery}
              onQueryChange={setSearchQuery}
              locationLabel="San Francisco, CA"
              onOpenLocation={() => setToastMessage('Opened Location Selector')}
              onSubmit={(e) => {
                e.preventDefault();
                setToastMessage(`Searching: ${searchQuery}`);
              }}
            />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {/* ProviderCard */}
            <div className="space-y-3">
              <h4 className="text-xs font-bold uppercase tracking-wider text-[#7E88A8]">
                ProviderCard (Touch Friendly, Image Fallback)
              </h4>
              <ProviderCard
                provider={{
                  id: 'p-1',
                  name: 'Luxe Botanica Hair & Glow Lounge',
                  slug: 'luxe-botanica',
                  category: 'Hair Salon',
                  rating: 4.9,
                  reviewCount: 238,
                  city: 'San Francisco',
                  address: '450 Sutter St, San Francisco',
                  distanceKm: 0.8,
                  startingPrice: 65,
                  currency: '$',
                  isVerified: true,
                  servicesSummary: ['Precision Cuts', 'Balayage', 'Hydra Facial'],
                }}
                isSelected={selectedProviderId === 'p-1'}
                onSelect={() => setSelectedProviderId('p-1')}
              />
            </div>

            {/* MapView with Resilience Controls (Section 103) */}
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <h4 className="text-xs font-bold uppercase tracking-wider text-[#7E88A8]">
                  Interactive Map Canvas & Section 103 Fallback
                </h4>
                <button
                  type="button"
                  onClick={() => setMapErrorSimulated(!mapErrorSimulated)}
                  className="text-[10px] font-mono text-[#E8546A] hover:underline"
                >
                  {mapErrorSimulated ? 'Reset Map Canvas' : 'Simulate Map Crash'}
                </button>
              </div>

              <MapView
                hasError={mapErrorSimulated}
                onRetry={() => setMapErrorSimulated(false)}
                providers={[
                  {
                    id: 'p-1',
                    name: 'Luxe Botanica',
                    slug: 'luxe-botanica',
                    category: 'Hair Salon',
                    rating: 4.9,
                    reviewCount: 238,
                    city: 'San Francisco',
                    startingPrice: 65,
                  },
                ]}
                selectedProviderId={selectedProviderId}
                onSelectProvider={(p) => setSelectedProviderId(p.id)}
              />
            </div>
          </div>
        </section>
      )}

      {/* =========================================================================
       * 7. RETAIL COMMERCE & MULTI-STAGE ESCROW (Section 109)
       * ========================================================================= */}
      {(activeTab === 'all' || activeTab === 'commerce') && (
        <section className="space-y-4">
          <div className="flex items-center justify-between border-b border-[#212638] pb-2">
            <h2 className="font-heading text-xl font-bold text-white flex items-center gap-2">
              <ShoppingBag className="w-5 h-5 text-[#E8546A]" /> 7. Commerce & Payment Escrow Primitives
            </h2>
            <span className="text-[10px] font-mono text-[#7E88A8]">ProductCard, CartItem, OrderStatus, PaymentSummary</span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {/* ProductCard */}
            <ProductCard
              product={{
                id: 'prod-1',
                name: 'Silk Revitalizing Hair Mask',
                description: 'Organic argan oil treatment infused with keratin proteins.',
                price: 38.0,
                originalPrice: 48.0,
                stock: 8,
                category: 'Retail Care',
                isFeatured: true,
              }}
              inCartQuantity={productQuantity}
              onAddToCart={() => setProductQuantity(1)}
              onUpdateQuantity={(_, q) => setProductQuantity(q)}
            />

            {/* CartItem list */}
            <div className="space-y-3">
              <h4 className="text-xs font-bold uppercase tracking-wider text-[#7E88A8]">
                Cart Items Breakdown
              </h4>
              <CartItem
                id="prod-1"
                name="Silk Revitalizing Hair Mask"
                price={38.0}
                quantity={productQuantity || 1}
                onUpdateQuantity={(_, q) => setProductQuantity(q)}
                onRemove={() => setProductQuantity(0)}
              />
              <CartItem
                id="prod-2"
                name="Rosehip Sculpting Clay"
                price={24.0}
                quantity={1}
                variantName="Matte Finish (100ml)"
              />
            </div>

            {/* CartSummary */}
            <CartSummary
              subtotal={38.0 * (productQuantity || 1) + 24.0}
              tax={3.1}
              platformFee={1.5}
              depositRequired={15.0}
              itemCount={cartItemsCount}
              onCheckout={() => setToastMessage('Redirecting to 3D-Secure checkout')}
            />
          </div>

          {/* OrderStatus & PaymentSummary */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 pt-2">
            <OrderStatus
              currentStatus="PROCESSING"
              orderNumber="BL-99201"
              orderDate="Oct 10, 2026"
              estimatedDelivery="Oct 12, 2026"
              trackingNumber="BK-TRK-7840192"
              carrierName="Express Cargo"
            />

            <PaymentSummary
              depositPaid={25.0}
              balanceDue={65.0}
              totalAmount={90.0}
              transactionId="txn_9x28hfqk192a"
              paidAt="10:14 AM, Oct 10"
              paymentMethod={{ type: 'CARD', brand: 'Visa', last4: '4242' }}
              onDownloadReceipt={() => setToastMessage('Downloading invoice PDF...')}
            />
          </div>
        </section>
      )}

      {/* =========================================================================
       * SECTION 110: COMPONENT CONTRACT STATE MATRIX
       * ========================================================================= */}
      {(activeTab === 'all' || activeTab === 'contract') && (
        <section className="space-y-4 pt-4 border-t border-[#212638]">
          <div className="flex items-center justify-between">
            <h2 className="font-heading text-xl font-bold text-white flex items-center gap-2">
              <CheckCircle className="w-5 h-5 text-[#34D399]" /> Section 110 Component State Coverage
            </h2>
            <span className="text-[10px] font-mono text-[#7E88A8]">10 Standardized State Contracts</span>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-3 text-xs">
            <div className="p-3 rounded-xl bg-[#111520] border border-[#212638] space-y-1">
              <p className="font-bold text-white">1. Default State</p>
              <p className="text-[11px] text-[#7E88A8]">Clean rest state with zero clutter.</p>
              <Button variant="secondary" size="sm" className="w-full mt-2">Resting</Button>
            </div>

            <div className="p-3 rounded-xl bg-[#111520] border border-[#212638] space-y-1">
              <p className="font-bold text-white">2. Hover State</p>
              <p className="text-[11px] text-[#7E88A8]">Subtle elevation & luminance shift.</p>
              <div className="p-2 rounded-lg bg-[#181D2C] border border-[#313A52] text-center text-white mt-2">
                Hover Active
              </div>
            </div>

            <div className="p-3 rounded-xl bg-[#111520] border border-[#212638] space-y-1">
              <p className="font-bold text-white">3. Focus State</p>
              <p className="text-[11px] text-[#7E88A8]">Accessible ring with 2px contrast outline.</p>
              <input
                readOnly
                value="Focus ring"
                className="w-full bg-[#181D2C] border border-[#E8546A] ring-2 ring-[#E8546A]/30 text-white rounded-lg px-2 py-1 text-xs mt-2"
              />
            </div>

            <div className="p-3 rounded-xl bg-[#111520] border border-[#212638] space-y-1">
              <p className="font-bold text-white">4. Selected State</p>
              <p className="text-[11px] text-[#7E88A8]">Active token highlight with brand coral.</p>
              <div className="p-2 rounded-lg bg-[#E8546A]/20 border border-[#E8546A] text-center text-[#E8546A] font-bold mt-2">
                Selected (10:00 AM)
              </div>
            </div>

            <div className="p-3 rounded-xl bg-[#111520] border border-[#212638] space-y-1">
              <p className="font-bold text-white">5. Disabled State</p>
              <p className="text-[11px] text-[#7E88A8]">Dimmed opacity (60%) & cursor-not-allowed.</p>
              <Button variant="primary" size="sm" disabled className="w-full mt-2">Disabled</Button>
            </div>

            <div className="p-3 rounded-xl bg-[#111520] border border-[#212638] space-y-1">
              <p className="font-bold text-white">6. Loading State</p>
              <p className="text-[11px] text-[#7E88A8]">Skeleton pulse or centered micro-spinner.</p>
              <Skeleton className="h-7 w-full mt-2" />
            </div>

            <div className="p-3 rounded-xl bg-[#111520] border border-[#212638] space-y-1">
              <p className="font-bold text-white">7. Error State</p>
              <p className="text-[11px] text-[#7E88A8]">Accessible red border & validation message.</p>
              <Input error="Invalid entry" defaultValue="Bad input" className="mt-2 text-xs" />
            </div>

            <div className="p-3 rounded-xl bg-[#111520] border border-[#212638] space-y-1">
              <p className="font-bold text-white">8. Empty State</p>
              <p className="text-[11px] text-[#7E88A8]">Helpful placeholder with action CTA.</p>
              <div className="p-2 text-center text-[11px] text-[#7E88A8] bg-[#181D2C] rounded-lg mt-2">
                0 Items Available
              </div>
            </div>

            <div className="p-3 rounded-xl bg-[#111520] border border-[#212638] space-y-1">
              <p className="font-bold text-white">9. Responsive</p>
              <p className="text-[11px] text-[#7E88A8]">Stackable grid down to 320px mobile viewports.</p>
              <div className="p-2 text-center text-[10px] text-[#34D399] bg-[#34D399]/10 rounded-lg mt-2 font-mono">
                Flex / Grid Fluid
              </div>
            </div>

            <div className="p-3 rounded-xl bg-[#111520] border border-[#212638] space-y-1">
              <p className="font-bold text-white">10. Accessible</p>
              <p className="text-[11px] text-[#7E88A8]">WCAG 2.1 AA compliant aria-labels & keyboard traps.</p>
              <div className="p-2 text-center text-[10px] text-white bg-[#212638] rounded-lg mt-2 font-mono">
                aria-label & roles
              </div>
            </div>
          </div>
        </section>
      )}
    </div>
  );
};
