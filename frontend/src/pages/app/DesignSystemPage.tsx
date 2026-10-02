import React, { useState } from 'react';
import { Button, IconButton, ButtonGroup } from '../../components/ui/Button';
import { Input, Textarea, Select, SearchBox, Switch } from '../../components/forms/Inputs';
import { Card, MetricCard, Badge, StatusBadge, Avatar, Skeleton, EmptyState, ErrorState } from '../../components/data-display/DataDisplay';
import { Alert, Modal, Drawer, ConfirmDialog } from '../../components/feedback/Feedback';
import { Slot, SlotGrid, AppointmentCard } from '../../components/scheduling/Scheduling';
import { Calendar, User, Sparkles, Bell, DollarSign } from 'lucide-react';

export const DesignSystemPage: React.FC = () => {
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [switchVal, setSwitchVal] = useState(true);
  const [selectedSlot, setSelectedSlot] = useState<string | null>('10:00');

  return (
    <div className="space-y-12 max-w-6xl mx-auto pb-20">
      {/* Header */}
      <div className="border-b border-[#212638] pb-6 space-y-2">
        <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-[#181D2C] border border-[#212638] text-xs font-mono text-[#E8546A]">
          <Sparkles className="w-3.5 h-3.5" /> PHASE 1 DESIGN SYSTEM
        </div>
        <h1 className="font-heading text-4xl font-extrabold text-white">Bookline Design Token Matrix</h1>
        <p className="text-sm text-[#7E88A8]">
          Reusable UI primitives, forms, data display, and scheduling tokens adhering to the dark luxury visual language.
        </p>
      </div>

      {/* Buttons & Actions */}
      <section className="space-y-4">
        <h3 className="font-heading text-xl font-bold text-white">1. Buttons & Action Primitives</h3>
        <div className="flex flex-wrap gap-4 items-center">
          <Button variant="primary">Primary Coral</Button>
          <Button variant="secondary">Secondary Dark</Button>
          <Button variant="outline">Outline</Button>
          <Button variant="ghost">Ghost</Button>
          <Button variant="danger">Danger Red</Button>
          <Button variant="primary" isLoading>Loading</Button>
          <Button variant="primary" disabled>Disabled</Button>
        </div>

        <div className="flex items-center gap-4 pt-2">
          <IconButton icon={<Bell className="w-4 h-4" />} variant="primary" />
          <IconButton icon={<User className="w-4 h-4" />} variant="secondary" />
          <IconButton icon={<Calendar className="w-4 h-4" />} variant="outline" />
          <ButtonGroup>
            <Button variant="ghost" size="sm">Day</Button>
            <Button variant="secondary" size="sm">Week</Button>
            <Button variant="ghost" size="sm">Month</Button>
          </ButtonGroup>
        </div>
      </section>

      {/* Inputs & Form Controls */}
      <section className="space-y-4">
        <h3 className="font-heading text-xl font-bold text-white">2. Form Input Primitives</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <Input label="Business Name" placeholder="e.g. Acme Hair & Spa" />
          <Input label="Email Address" error="Please enter a valid business email" defaultValue="invalid-email" />
          <Select
            label="Default Timezone"
            options={[
              { value: 'Asia/Kolkata', label: 'Asia/Kolkata (IST +05:30)' },
              { value: 'America/New_York', label: 'America/New_York (EST -05:00)' },
              { value: 'Europe/London', label: 'Europe/London (GMT +00:00)' },
            ]}
          />
          <Textarea label="Special Notes" placeholder="Add guest intake instructions..." />
        </div>
        <div className="flex items-center gap-8 pt-2">
          <Switch checked={switchVal} onChange={setSwitchVal} label="Enable Online Booking Engine" />
          <SearchBox />
        </div>
      </section>

      {/* Data Display Primitives */}
      <section className="space-y-4">
        <h3 className="font-heading text-xl font-bold text-white">3. Metric Cards & Container Primitives</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <MetricCard
            label="Today's Revenue"
            metric="$1,240.00"
            comparison="+18.4% vs last week"
            trend="up"
            icon={<DollarSign className="w-5 h-5" />}
          />
          <MetricCard
            label="Total Bookings"
            metric="24"
            comparison="Optimal capacity"
            trend="neutral"
            icon={<Calendar className="w-5 h-5" />}
          />
          <Card className="flex flex-col justify-between">
            <h4 className="font-heading font-semibold text-white">Standard Card Surface</h4>
            <p className="text-xs text-[#7E88A8]">Elevated surface background `#111520` with subtle `#212638` border.</p>
          </Card>
        </div>
      </section>

      {/* Badges & Status Indicators */}
      <section className="space-y-4">
        <h3 className="font-heading text-xl font-bold text-white">4. Badges & Appointment Status Tokens</h3>
        <div className="flex flex-wrap gap-3 items-center">
          <StatusBadge status="Free" />
          <StatusBadge status="Held" />
          <StatusBadge status="Booked" />
          <StatusBadge status="Pending" />
          <StatusBadge status="Confirmed" />
          <StatusBadge status="CheckedIn" />
          <StatusBadge status="Completed" />
          <StatusBadge status="Cancelled" />
          <StatusBadge status="NoShow" />
        </div>
        <div className="flex items-center gap-3">
          <Badge variant="primary">Coral Primary</Badge>
          <Badge variant="success">Positive Free</Badge>
          <Badge variant="warning">Pending Hold</Badge>
          <Badge variant="neutral">Neutral System</Badge>
          <Badge variant="danger">High Severity</Badge>
          <Avatar name="Alex Johnson" />
          <Avatar name="Priya Shah" />
        </div>
      </section>

      {/* Scheduling & Slots */}
      <section className="space-y-4">
        <h3 className="font-heading text-xl font-bold text-white">5. Scheduling Slot & Appointment Cards</h3>
        <SlotGrid>
          <Slot time="09:00" state="FREE" onClick={() => setSelectedSlot('09:00')} />
          <Slot time="09:30" state="FREE" onClick={() => setSelectedSlot('09:30')} />
          <Slot time="10:00" state={selectedSlot === '10:00' ? 'SELECTED' : 'FREE'} onClick={() => setSelectedSlot('10:00')} />
          <Slot time="10:30" state="HELD" holdTimeRemaining="04:45" />
          <Slot time="11:00" state="BOOKED" />
          <Slot time="11:30" state="UNAVAILABLE" />
        </SlotGrid>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6 pt-4">
          <AppointmentCard
            customerName="Priya Shah"
            serviceName="Haircut & Styling"
            durationMinutes={45}
            staffName="Alex Johnson"
            timeRange="10:00 AM - 10:45 AM"
            status="Confirmed"
            price="$50.00"
          />
          <AppointmentCard
            customerName="Rahul Mehta"
            serviceName="Beard Trim & Facial"
            durationMinutes={30}
            staffName="Meera Patel"
            timeRange="11:00 AM - 11:30 AM"
            status="Pending"
            price="$35.00"
          />
        </div>
      </section>

      {/* Empty & Error States */}
      <section className="space-y-4">
        <h3 className="font-heading text-xl font-bold text-white">6. Skeleton, Empty & Error States</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="space-y-3 bg-[#111520] p-6 rounded-2xl border border-[#212638]">
            <h5 className="text-xs font-semibold text-[#7E88A8]">SKELETON LOADING</h5>
            <Skeleton className="h-6 w-3/4" />
            <Skeleton className="h-4 w-1/2" />
            <Skeleton className="h-10 w-full" />
          </div>
          <EmptyState title="No Staff Found" description="Get started by adding your first staff member." />
          <ErrorState message="Failed to load availability. Please check server connection." />
        </div>
      </section>

      {/* Feedback & Modals */}
      <section className="space-y-4">
        <h3 className="font-heading text-xl font-bold text-white">7. Feedback & Modal Systems</h3>
        <div className="flex gap-4">
          <Button variant="primary" onClick={() => setIsModalOpen(true)}>
            Open Sample Modal
          </Button>
          <Button variant="secondary" onClick={() => setIsDrawerOpen(true)}>
            Open Detail Drawer
          </Button>
          <Button variant="danger" onClick={() => setIsConfirmOpen(true)}>
            Trigger Confirm Dialog
          </Button>
        </div>

        <div className="space-y-3 pt-2">
          <Alert variant="info" title="System Info">
            Double-booking prevention engine uses PostgreSQL exclusion constraints (`EXCLUDE USING gist`) and 5-minute Redis holds.
          </Alert>
          <Alert variant="success" title="Hold Active">
            Slot 10:00 AM reserved atomically for 5 minutes.
          </Alert>
          <Alert variant="warning" title="Notice Required">
            Appointments must be cancelled at least 2 hours prior to scheduled start time.
          </Alert>
        </div>
      </section>

      {/* Dialog Components */}
      <Modal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} title="Create New Appointment">
        <div className="space-y-4">
          <Input label="Customer Name" placeholder="e.g. Jane Doe" />
          <Select
            label="Service"
            options={[
              { value: '1', label: 'Haircut & Style (45m)' },
              { value: '2', label: 'Color & Highlights (90m)' },
            ]}
          />
          <p className="text-xs text-[#7E88A8]">Real-time availability validation will trigger on submission.</p>
        </div>
      </Modal>

      <Drawer isOpen={isDrawerOpen} onClose={() => setIsDrawerOpen(false)} title="Appointment Details — BL-7H4Q2M">
        <div className="space-y-6">
          <div className="space-y-1">
            <h4 className="font-heading text-xl font-bold text-white">Priya Shah</h4>
            <p className="text-xs text-[#7E88A8]">Customer Reference #CUST-901</p>
          </div>
          <StatusBadge status="Confirmed" />
          <div className="border-t border-[#212638] pt-4 space-y-2 text-sm text-[#7E88A8]">
            <p><strong className="text-white">Service:</strong> Haircut & Style</p>
            <p><strong className="text-white">Time:</strong> 10:00 AM - 10:45 AM</p>
            <p><strong className="text-white">Price:</strong> $50.00</p>
          </div>
        </div>
      </Drawer>

      <ConfirmDialog
        isOpen={isConfirmOpen}
        onClose={() => setIsConfirmOpen(false)}
        onConfirm={() => setIsConfirmOpen(false)}
        title="Cancel Appointment?"
        message="Are you sure you want to cancel this appointment? The customer will receive an email notification and the slot will be released immediately."
        confirmText="Cancel Appointment"
        isDanger
      />
    </div>
  );
};
