import test from 'node:test';
import assert from 'node:assert/strict';
import {
  bookingFormSchema,
  loginSchema,
  registerTenantSchema,
  serviceFormSchema,
  productFormSchema,
  createOrderSchema,
  reviewFormSchema,
  formatZodErrors,
} from './validationSchemas.ts';

test('Section 83: bookingFormSchema validates valid booking submission', () => {
  const futureUtc = new Date(Date.now() + 2 * 60 * 60 * 1000).toISOString();
  const validData = {
    customerName: 'Sarah Connor',
    customerEmail: 'sarah.connor@example.com',
    customerPhone: '+1-555-0199',
    customerNotes: 'Allergic to specific hair products.',
    serviceId: '11111111-1111-1111-1111-111111111111',
    staffId: '22222222-2222-2222-2222-222222222222',
    startUtc: futureUtc,
    holdId: '33333333-3333-3333-3333-333333333333',
  };

  const result = bookingFormSchema.safeParse(validData);
  assert.equal(result.success, true);
});

test('Section 83: bookingFormSchema rejects invalid email, empty name, and past date', () => {
  const pastUtc = new Date(Date.now() - 60 * 60 * 1000).toISOString();
  const invalidData = {
    customerName: '',
    customerEmail: 'not-an-email',
    serviceId: 'not-a-uuid',
    staffId: '22222222-2222-2222-2222-222222222222',
    startUtc: pastUtc,
    holdId: '33333333-3333-3333-3333-333333333333',
  };

  const result = bookingFormSchema.safeParse(invalidData);
  assert.equal(result.success, false);

  if (!result.success) {
    const fieldErrors = formatZodErrors(result.error);
    assert.ok(fieldErrors.customerName?.length);
    assert.ok(fieldErrors.customerEmail?.length);
    assert.ok(fieldErrors.serviceId?.length);
    assert.ok(fieldErrors.startUtc?.length);
    assert.equal(fieldErrors.customerEmail[0], 'Please enter a valid email address.');
    assert.equal(fieldErrors.startUtc[0], 'Appointment start time must be in the future.');
  }
});

test('Section 83: registerTenantSchema enforces slug formatting and password strength', () => {
  const invalidTenant = {
    tenantName: 'Hi', // too short (<3)
    tenantSlug: 'Invalid Slug With Spaces!',
    ownerEmail: 'owner@business.com',
    password: 'weak', // too short (<8) and no number/uppercase
    firstName: 'Alice',
    lastName: 'Smith',
  };

  const result = registerTenantSchema.safeParse(invalidTenant);
  assert.equal(result.success, false);

  if (!result.success) {
    const errors = formatZodErrors(result.error);
    assert.ok(errors.tenantName?.length);
    assert.ok(errors.tenantSlug?.length);
    assert.ok(errors.password?.length);
  }
});

test('Section 83: serviceFormSchema rejects negative price, out-of-range duration, and invalid currency', () => {
  const invalidService = {
    name: 'Quick Trim',
    categoryId: '11111111-1111-1111-1111-111111111111',
    durationMinutes: 2, // < 5 min minimum
    price: -15.50, // negative price
    currency: 'US', // not 3-letter ISO code
  };

  const result = serviceFormSchema.safeParse(invalidService);
  assert.equal(result.success, false);

  if (!result.success) {
    const errors = formatZodErrors(result.error);
    assert.ok(errors.durationMinutes?.length);
    assert.ok(errors.price?.length);
    assert.ok(errors.currency?.length);
    assert.equal(errors.durationMinutes[0], 'Duration must be at least 5 minutes.');
    assert.equal(errors.price[0], 'Price cannot be negative.');
  }
});

test('Section 83: productFormSchema enforces inventory constraints', () => {
  const validProduct = {
    name: 'Organic Argan Oil',
    price: 24.99,
    currency: 'USD',
    stockQuantity: 15,
    lowStockThreshold: 3,
    trackInventory: true,
  };

  const validResult = productFormSchema.safeParse(validProduct);
  assert.equal(validResult.success, true);

  const invalidProduct = {
    name: 'Organic Argan Oil',
    price: 24.99,
    stockQuantity: -5, // negative stock
  };

  const invalidResult = productFormSchema.safeParse(invalidProduct);
  assert.equal(invalidResult.success, false);
  if (!invalidResult.success) {
    const errors = formatZodErrors(invalidResult.error);
    assert.ok(errors.stockQuantity?.length);
    assert.equal(errors.stockQuantity[0], 'Stock quantity cannot be negative.');
  }
});

test('Section 83: createOrderSchema requires at least one item with positive quantity', () => {
  const emptyOrder = {
    tenantId: '11111111-1111-1111-1111-111111111111',
    customerName: 'Bob',
    customerEmail: 'bob@example.com',
    items: [],
  };

  const resultEmpty = createOrderSchema.safeParse(emptyOrder);
  assert.equal(resultEmpty.success, false);
  if (!resultEmpty.success) {
    const errors = formatZodErrors(resultEmpty.error);
    assert.ok(errors.items?.length);
  }

  const zeroQtyOrder = {
    tenantId: '11111111-1111-1111-1111-111111111111',
    customerName: 'Bob',
    customerEmail: 'bob@example.com',
    items: [
      { productId: '22222222-2222-2222-2222-222222222222', quantity: 0 },
    ],
  };

  const resultZero = createOrderSchema.safeParse(zeroQtyOrder);
  assert.equal(resultZero.success, false);
  if (!resultZero.success) {
    const errors = formatZodErrors(resultZero.error);
    assert.ok(errors['items.0.quantity']?.length);
  }
});

test('Section 83: reviewFormSchema enforces 1 to 5 star rating and comment length', () => {
  const invalidReview = {
    tenantId: '11111111-1111-1111-1111-111111111111',
    rating: 6, // > 5
    comment: 'Bad', // < 5 chars
  };

  const result = reviewFormSchema.safeParse(invalidReview);
  assert.equal(result.success, false);

  if (!result.success) {
    const errors = formatZodErrors(result.error);
    assert.ok(errors.rating?.length);
    assert.ok(errors.comment?.length);
    assert.equal(errors.rating[0], 'Rating cannot exceed 5 stars.');
    assert.equal(errors.comment[0], 'Review comment must be at least 5 characters.');
  }
});

test('Section 83: formatZodErrors maps Zod issues to ApiErrorResponse structured errors shape', () => {
  const schema = bookingFormSchema;
  const result = schema.safeParse({});
  assert.equal(result.success, false);

  if (!result.success) {
    const formatted = formatZodErrors(result.error);
    assert.equal(typeof formatted, 'object');
    assert.ok(Array.isArray(formatted.customerName));
    assert.ok(Array.isArray(formatted.customerEmail));
    assert.ok(Array.isArray(formatted.serviceId));
  }
});
