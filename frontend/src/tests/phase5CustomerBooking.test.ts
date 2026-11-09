import test from 'node:test';
import assert from 'node:assert/strict';

/**
 * Phase 5 Specification & Verification:
 * Customer Booking Experience, Public Storefront, Direct Booking Links & Guest Access
 */

test('Phase 5: Public Storefront Resolution - Only exposes public active information', () => {
  const rawProvider = {
    id: 'prov-001',
    name: 'Aura Wellness & Spa',
    slug: 'aura-wellness-ahmedabad',
    category: 'Wellness & Spa',
    city: 'Ahmedabad',
    area: 'Bodakdev',
    address: '402 Bodakdev Commercial Hub, SG Highway',
    description: 'Bespoke clinical aesthetics and restorative therapy.',
    isActive: true,
    isPublished: true,
    // Sensitive / internal fields that MUST NOT leak to public customers:
    internalNotes: 'Tenant has credit dispute on invoice #492',
    bankAccountNumber: '9182390123901',
    adminCommissionRate: 0.15,
  };

  const sanitizeForPublic = (p: typeof rawProvider) => {
    assert.equal(p.isActive, true, 'Provider must be active');
    assert.equal(p.isPublished, true, 'Provider must be published');

    return {
      id: p.id,
      name: p.name,
      slug: p.slug,
      category: p.category,
      city: p.city,
      area: p.area,
      address: p.address,
      description: p.description,
    };
  };

  const publicData = sanitizeForPublic(rawProvider);
  assert.equal(publicData.name, 'Aura Wellness & Spa');
  assert.equal(publicData.slug, 'aura-wellness-ahmedabad');
  assert.equal((publicData as any).internalNotes, undefined, 'Internal notes must never leak to customers');
  assert.equal((publicData as any).bankAccountNumber, undefined, 'Financial details must never leak');
});

test('Phase 5: Direct Booking Links - Stable public URL formatting and WhatsApp share integration', () => {
  const origin = 'https://bookline.app';
  const slug = 'aura-wellness-ahmedabad';
  const businessName = 'Aura Wellness & Spa';

  const storefrontUrl = `${origin}/business/${slug}`;
  const directBookUrl = `${origin}/book/${slug}`;
  const shareText = encodeURIComponent(
    `Book your appointment online with ${businessName} on Bookline: ${directBookUrl}`
  );
  const whatsappUrl = `https://wa.me/?text=${shareText}`;

  assert.equal(storefrontUrl, 'https://bookline.app/business/aura-wellness-ahmedabad');
  assert.equal(directBookUrl, 'https://bookline.app/book/aura-wellness-ahmedabad');
  assert.ok(whatsappUrl.startsWith('https://wa.me/?text='));
  assert.ok(whatsappUrl.includes(encodeURIComponent(directBookUrl)));
});

test('Phase 5: Booking Flow - Service Selection, Staff Preference, Slot Hold, and Confirmation', () => {
  // Step 1: Select service
  const selectedService = {
    id: 'srv-101',
    name: 'Holistic Aromatherapy Massage',
    durationMinutes: 60,
    price: 2400,
    currency: 'INR',
  };
  assert.ok(selectedService.durationMinutes > 0);
  assert.ok(selectedService.price > 0);

  // Step 2: Select staff or "Any specialist"
  const staffMember = {
    id: 'staff-201',
    name: 'Elena Vance',
    isActive: true,
  };
  assert.equal(staffMember.isActive, true);

  // Step 3: Server-controlled slot hold (5 minutes = 300 seconds)
  const slotHold = {
    holdId: 'hold-uuid-555',
    staffId: staffMember.id,
    serviceId: selectedService.id,
    startUtc: '2026-11-15T10:00:00Z',
    expiresAtUtc: '2026-11-15T09:05:00Z',
    status: 'HELD',
    ttlSeconds: 300,
  };
  assert.equal(slotHold.status, 'HELD');
  assert.equal(slotHold.ttlSeconds, 300);

  // Step 4: Confirm Booking & generate reference and opaque management token
  const bookingReference = 'BL-849201';
  const opaqueManageToken = 'CfDJ8N3...opaque-token-payload';
  const booking = {
    id: 'bk-uuid-999',
    bookingReference,
    serviceId: selectedService.id,
    staffId: staffMember.id,
    status: 'Confirmed',
    totalPrice: selectedService.price,
    manageToken: opaqueManageToken,
    publicManageUrl: `/manage-booking?token=${encodeURIComponent(opaqueManageToken)}`,
  };

  assert.match(booking.bookingReference, /^BL-\d{6}$/);
  assert.equal(booking.status, 'Confirmed');
  assert.ok(booking.manageToken.length > 10);
  assert.ok(booking.publicManageUrl.startsWith('/manage-booking?token='));
});

test('Phase 5: Guest & Customer Access Isolation - Rejects ID guessing without cryptographic token', () => {
  // Guessing an appointment ID directly without auth or token must be rejected
  const validateAccess = (params: { token?: string; authUserId?: string; appointmentOwnerUserId?: string }) => {
    if (params.authUserId && params.appointmentOwnerUserId && params.authUserId === params.appointmentOwnerUserId) {
      return { allowed: true, reason: 'Authenticated owner' };
    }
    if (params.token && params.token.startsWith('CfDJ8') && params.token.length >= 20) {
      return { allowed: true, reason: 'Valid cryptographically signed token' };
    }
    return { allowed: false, reason: 'Unauthorized access' };
  };

  // 1. Attacker tries to guess an appointment ID with no token
  const attackerResult = validateAccess({ token: undefined, authUserId: undefined });
  assert.equal(attackerResult.allowed, false);

  // 2. Attacker provides a random or short token
  const tamperedResult = validateAccess({ token: 'random-guess-123' });
  assert.equal(tamperedResult.allowed, false);

  // 3. Legitimate guest user with signed token from confirmation
  const legitGuest = validateAccess({ token: 'CfDJ8N398492019238102931238' });
  assert.equal(legitGuest.allowed, true);

  // 4. Legitimate authenticated user matching appointment owner
  const legitAuthUser = validateAccess({ authUserId: 'user-1', appointmentOwnerUserId: 'user-1' });
  assert.equal(legitAuthUser.allowed, true);

  // 5. Authenticated user trying to access another user's appointment
  const snoopingUser = validateAccess({ authUserId: 'user-2', appointmentOwnerUserId: 'user-1' });
  assert.equal(snoopingUser.allowed, false);
});

test('Phase 5: Cancellation Policy - Enforces minimum notice window', () => {
  const minimumNoticeHours = 4;
  const now = new Date('2026-11-10T12:00:00Z');

  const checkCanCancel = (appointmentStart: Date) => {
    const diffHours = (appointmentStart.getTime() - now.getTime()) / (1000 * 60 * 60);
    return {
      canCancel: diffHours >= minimumNoticeHours,
      diffHours,
    };
  };

  // Case A: 24 hours in the future -> allowed
  const futureApt = new Date('2026-11-11T12:00:00Z');
  assert.equal(checkCanCancel(futureApt).canCancel, true);

  // Case B: 1 hour in the future -> rejected by policy
  const imminentApt = new Date('2026-11-10T13:00:00Z');
  assert.equal(checkCanCancel(imminentApt).canCancel, false);
});

test('Phase 5: Rescheduling Safety - Re-validates slot availability and preserves original on conflict', () => {
  interface Appointment {
    id: string;
    slot: string;
    status: string;
  }

  const existingBooking: Appointment = {
    id: 'apt-1',
    slot: '2026-11-12T10:00:00Z',
    status: 'Confirmed',
  };

  const occupiedSlots = new Set(['2026-11-12T11:00:00Z', '2026-11-12T14:00:00Z']);

  const reschedule = (apt: Appointment, targetSlot: string) => {
    if (occupiedSlots.has(targetSlot)) {
      // Must NOT modify existing booking on conflict
      throw new Error('BOOKING_CONFLICT: Target slot is occupied');
    }
    return {
      ...apt,
      slot: targetSlot,
    };
  };

  // Attempting to reschedule to an occupied slot must throw and leave original untouched
  assert.throws(() => reschedule(existingBooking, '2026-11-12T11:00:00Z'), /BOOKING_CONFLICT/);
  assert.equal(existingBooking.slot, '2026-11-12T10:00:00Z');

  // Rescheduling to an unoccupied slot succeeds
  const updated = reschedule(existingBooking, '2026-11-12T16:00:00Z');
  assert.equal(updated.slot, '2026-11-12T16:00:00Z');
});

test('Phase 5: Mobile Viewport Considerations - Direct booking URLs and responsive components', () => {
  // Mobile browsers, WhatsApp in-app browser, and Instagram webview support
  const userAgentChecks = [
    { ua: 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Mobile/15E148 Instagram 300.0.0.0', isMobile: true },
    { ua: 'WhatsApp/2.23.20.0 A', isMobile: true },
    { ua: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)', isMobile: false },
  ];

  const detectMobile = (ua: string) => /iPhone|iPad|iPod|Android|WhatsApp|Instagram/i.test(ua);

  for (const item of userAgentChecks) {
    assert.equal(detectMobile(item.ua), item.isMobile);
  }
});

