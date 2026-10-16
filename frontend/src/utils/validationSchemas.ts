import { z } from 'zod';

/**
 * Universal Frontend Validation Schemas using Zod (Section 83).
 *
 * Responsibilities:
 * - Validate early on the client for instant UX feedback.
 * - Required fields, string formats, length constraints, numeric ranges, and date formats.
 * - Clear field-level error messages.
 * - Format alignment with backend FluentValidation field names.
 */

// Common reusable primitives
export const emailSchema = z
  .string()
  .trim()
  .min(1, 'Email address is required.')
  .email('Please enter a valid email address.');

export const phoneSchema = z
  .string()
  .trim()
  .regex(/^[+]?[(]?[0-9]{1,4}[)]?[-\s./0-9]{7,15}$/, 'Please enter a valid phone number.')
  .optional()
  .or(z.literal(''));

export const passwordSchema = z
  .string()
  .min(8, 'Password must be at least 8 characters long.')
  .regex(/[A-Z]/, 'Password must contain at least one uppercase letter.')
  .regex(/[0-9]/, 'Password must contain at least one number.');

export const uuidSchema = z
  .string()
  .uuid('Invalid identifier format.');

export const currencySchema = z
  .string()
  .trim()
  .length(3, 'Currency must be a 3-letter ISO code.')
  .toUpperCase();

export const hexColorSchema = z
  .string()
  .regex(/^#(?:[0-9a-fA-F]{3}){1,2}$/, 'Please enter a valid hexadecimal color code (e.g. #E8546A).')
  .optional()
  .or(z.literal(''));

/* =========================================================================
 * 1. PUBLIC BOOKING FLOW SCHEMAS
 * ========================================================================= */
export const bookingFormSchema = z.object({
  customerName: z
    .string()
    .trim()
    .min(2, 'Full name must be at least 2 characters.')
    .max(100, 'Full name cannot exceed 100 characters.'),
  customerEmail: emailSchema,
  customerPhone: phoneSchema,
  customerNotes: z
    .string()
    .max(500, 'Notes cannot exceed 500 characters.')
    .optional()
    .or(z.literal('')),
  serviceId: uuidSchema,
  staffId: uuidSchema,
  startUtc: z
    .string()
    .datetime({ message: 'Invalid appointment start time format.' })
    .refine((val) => new Date(val).getTime() > Date.now() - 5 * 60 * 1000, {
      message: 'Appointment start time must be in the future.',
    }),
  holdId: uuidSchema,
});

export type BookingFormData = z.infer<typeof bookingFormSchema>;

/* =========================================================================
 * 2. AUTHENTICATION & ONBOARDING SCHEMAS
 * ========================================================================= */
export const loginSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, 'Password is required.'),
});

export type LoginFormData = z.infer<typeof loginSchema>;

export const registerCustomerSchema = z.object({
  firstName: z.string().trim().min(1, 'First name is required.').max(50, 'First name too long.'),
  lastName: z.string().trim().min(1, 'Last name is required.').max(50, 'Last name too long.'),
  email: emailSchema,
  password: passwordSchema,
  phone: phoneSchema,
});

export type RegisterCustomerFormData = z.infer<typeof registerCustomerSchema>;

export const registerTenantSchema = z.object({
  tenantName: z
    .string()
    .trim()
    .min(3, 'Business name must be at least 3 characters.')
    .max(100, 'Business name cannot exceed 100 characters.'),
  tenantSlug: z
    .string()
    .trim()
    .min(2, 'Slug must be at least 2 characters.')
    .max(60, 'Slug cannot exceed 60 characters.')
    .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Slug must be lowercase alphanumeric with hyphens (e.g. glow-studio).'),
  ownerEmail: emailSchema,
  password: passwordSchema,
  firstName: z.string().trim().min(1, 'First name is required.'),
  lastName: z.string().trim().min(1, 'Last name is required.'),
});

export type RegisterTenantFormData = z.infer<typeof registerTenantSchema>;

export const forgotPasswordSchema = z.object({
  email: emailSchema,
});

export type ForgotPasswordFormData = z.infer<typeof forgotPasswordSchema>;

export const resetPasswordSchema = z
  .object({
    password: passwordSchema,
    confirmPassword: z.string().min(1, 'Please confirm your password.'),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: 'Passwords do not match.',
    path: ['confirmPassword'],
  });

export type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>;

/* =========================================================================
 * 3. SERVICES & RESOURCES SCHEMAS
 * ========================================================================= */
export const serviceCategorySchema = z.object({
  name: z.string().trim().min(2, 'Category name must be at least 2 characters.').max(100),
  description: z.string().max(500).optional().or(z.literal('')),
  sortOrder: z.number().int().min(0).default(0),
});

export const serviceFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(2, 'Service name must be at least 2 characters.')
    .max(150, 'Service name cannot exceed 150 characters.'),
  categoryId: uuidSchema,
  description: z.string().max(1000, 'Description cannot exceed 1000 characters.').optional().or(z.literal('')),
  durationMinutes: z
    .number({ invalid_type_error: 'Duration must be a number.' })
    .int('Duration must be a whole number of minutes.')
    .min(5, 'Duration must be at least 5 minutes.')
    .max(1440, 'Duration cannot exceed 24 hours (1440 minutes).'),
  bufferBeforeMinutes: z
    .number({ invalid_type_error: 'Buffer must be a number.' })
    .int()
    .min(0, 'Buffer cannot be negative.')
    .max(240, 'Buffer cannot exceed 240 minutes.')
    .default(0),
  bufferAfterMinutes: z
    .number({ invalid_type_error: 'Buffer must be a number.' })
    .int()
    .min(0, 'Buffer cannot be negative.')
    .max(240, 'Buffer cannot exceed 240 minutes.')
    .default(0),
  price: z
    .number({ invalid_type_error: 'Price must be a valid number.' })
    .min(0, 'Price cannot be negative.')
    .max(1000000, 'Price exceeds maximum allowed value.'),
  currency: currencySchema.default('USD'),
  colorHex: hexColorSchema.default('#E8546A'),
  isOnlineBookingEnabled: z.boolean().default(true),
  isPublished: z.boolean().default(true),
  requiresResource: z.boolean().default(false),
  requiredResourceType: z.string().optional().or(z.literal('')),
  requiredResourceId: uuidSchema.optional().nullable(),
});

export type ServiceFormData = z.infer<typeof serviceFormSchema>;

export const resourceFormSchema = z.object({
  name: z.string().trim().min(2, 'Resource name must be at least 2 characters.').max(100),
  description: z.string().max(500).optional().or(z.literal('')),
  type: z.string().trim().min(1, 'Resource type is required.').max(50),
  capacity: z.number().int().min(1, 'Capacity must be at least 1.').max(1000),
  locationId: uuidSchema.optional().nullable(),
});

export type ResourceFormData = z.infer<typeof resourceFormSchema>;

/* =========================================================================
 * 4. COMMERCE / PRODUCTS SCHEMAS
 * ========================================================================= */
export const productFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(2, 'Product name must be at least 2 characters.')
    .max(200, 'Product name cannot exceed 200 characters.'),
  description: z.string().max(2000).optional().or(z.literal('')),
  price: z
    .number({ invalid_type_error: 'Price must be a number.' })
    .min(0, 'Price cannot be negative.')
    .max(1000000, 'Price exceeds maximum allowed value.'),
  currency: currencySchema.default('USD'),
  sku: z.string().trim().max(50).optional().or(z.literal('')),
  categoryId: uuidSchema.optional().nullable(),
  trackInventory: z.boolean().default(true),
  stockQuantity: z
    .number({ invalid_type_error: 'Stock quantity must be a number.' })
    .int('Stock quantity must be an integer.')
    .min(0, 'Stock quantity cannot be negative.')
    .default(0),
  lowStockThreshold: z
    .number({ invalid_type_error: 'Low stock threshold must be a number.' })
    .int()
    .min(0, 'Low stock threshold cannot be negative.')
    .default(5),
  isPurchasableOnline: z.boolean().default(true),
  isPublished: z.boolean().default(true),
});

export type ProductFormData = z.infer<typeof productFormSchema>;

export const createOrderSchema = z.object({
  tenantId: uuidSchema,
  customerName: z.string().trim().min(2, 'Customer name is required.'),
  customerEmail: emailSchema,
  customerPhone: phoneSchema,
  notes: z.string().max(500).optional().or(z.literal('')),
  relatedBookingId: uuidSchema.optional().nullable(),
  items: z
    .array(
      z.object({
        productId: uuidSchema,
        quantity: z.number().int().min(1, 'Quantity must be at least 1.').max(1000),
      })
    )
    .min(1, 'Order must contain at least one product.'),
});

export type CreateOrderFormData = z.infer<typeof createOrderSchema>;

/* =========================================================================
 * 5. REVIEWS & RATINGS SCHEMAS
 * ========================================================================= */
export const reviewFormSchema = z.object({
  tenantId: uuidSchema,
  rating: z
    .number({ invalid_type_error: 'Rating must be selected.' })
    .int()
    .min(1, 'Rating must be at least 1 star.')
    .max(5, 'Rating cannot exceed 5 stars.'),
  title: z.string().trim().max(100, 'Title cannot exceed 100 characters.').optional().or(z.literal('')),
  comment: z
    .string()
    .trim()
    .min(5, 'Review comment must be at least 5 characters.')
    .max(1000, 'Review comment cannot exceed 1000 characters.'),
  customerName: z.string().trim().max(100).optional().or(z.literal('')),
  bookingId: uuidSchema.optional().nullable(),
  orderId: uuidSchema.optional().nullable(),
});

export type ReviewFormData = z.infer<typeof reviewFormSchema>;

/* =========================================================================
 * 6. ZOD ERROR FORMATTER TO API ERROR ENVELOPE FORMAT
 * ========================================================================= */
/**
 * Transforms a ZodError into the standard field errors dictionary: Record<string, string[]>
 * with camelCase keys matching the backend ApiErrorResponse.Errors format.
 */
export function formatZodErrors(error: z.ZodError): Record<string, string[]> {
  const result: Record<string, string[]> = {};

  for (const issue of error.issues) {
    const path = issue.path.join('.');
    const fieldName = path || 'general';

    if (!result[fieldName]) {
      result[fieldName] = [];
    }
    result[fieldName].push(issue.message);
  }

  return result;
}
