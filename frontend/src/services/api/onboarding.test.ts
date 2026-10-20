import test from 'node:test';
import assert from 'node:assert/strict';
import { onboardingApi } from './onboarding.ts';
import type { OnboardingStatus, OnboardingReadiness, PublishResult } from './onboarding.ts';

test('Section 110: onboardingApi.getStatus parses status and returns server-persisted records', async () => {
  const originalFetch = globalThis.fetch;
  try {
    const mockStatus: OnboardingStatus = {
      tenantId: '00000000-0000-0000-0000-000000000001',
      businessName: 'Luxe Hair Studio',
      businessSlug: 'luxe-hair-studio',
      businessType: 'Hair Salon',
      category: 'Hair Salon',
      description: 'Artisanal haircutting and colouring.',
      address: '100 Broadway',
      city: 'New York',
      state: 'NY',
      postalCode: '10001',
      country: 'USA',
      timeZoneId: 'America/New_York',
      currency: 'USD',
      currentStep: 6,
      isCompleted: false,
      isPublished: false,
      verificationStatus: 'Pending',
      moderationStatus: 'Pending',
      holdDurationMinutes: 5,
      minimumNoticeHours: 2,
      bookingHorizonDays: 30,
      cancellationWindowHours: 4,
      completedSteps: [1, 2, 3, 4, 5, 6],
      ownerName: 'Elena Vance',
      ownerEmail: 'elena@luxe.example.com',
      ownerPhone: '+1 555 123 4567',
      membershipRole: 'Owner',
    };

    globalThis.fetch = async (url: any, init: any) => {
      assert.equal(url, '/api/v1/onboarding/status');
      assert.equal(init?.headers?.Authorization, 'Bearer test-token-123');
      return new Response(JSON.stringify(mockStatus), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    };

    const status = await onboardingApi.getStatus('test-token-123');
    assert.equal(status.businessName, 'Luxe Hair Studio');
    assert.equal(status.businessSlug, 'luxe-hair-studio');
    assert.equal(status.currentStep, 6);
    assert.equal(status.ownerName, 'Elena Vance');
    assert.equal(status.membershipRole, 'Owner');
    assert.deepEqual(status.completedSteps, [1, 2, 3, 4, 5, 6]);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test('Section 110: onboardingApi.saveStep serializes step data and Authorization header', async () => {
  const originalFetch = globalThis.fetch;
  try {
    let capturedBody: any = null;

    globalThis.fetch = async (url: any, init: any) => {
      assert.equal(url, '/api/v1/onboarding/step');
      assert.equal(init?.method, 'POST');
      assert.equal(init?.headers?.['Content-Type'], 'application/json');
      assert.equal(init?.headers?.Authorization, 'Bearer jwt-token-abc');
      capturedBody = JSON.parse(init.body);

      return new Response(
        JSON.stringify({
          tenantId: '00000000-0000-0000-0000-000000000001',
          businessName: 'Luxe Hair Studio',
          businessSlug: 'luxe-hair-studio',
          businessType: 'Hair Salon',
          currentStep: 3,
          isCompleted: false,
          isPublished: false,
          completedSteps: [1, 2],
        }),
        { status: 200, headers: { 'Content-Type': 'application/json' } }
      );
    };

    const result = await onboardingApi.saveStep('jwt-token-abc', {
      step: 2,
      businessName: 'Luxe Hair Studio',
      businessSlug: 'luxe-hair-studio',
    });

    assert.equal(capturedBody.step, 2);
    assert.equal(capturedBody.businessName, 'Luxe Hair Studio');
    assert.equal(capturedBody.businessSlug, 'luxe-hair-studio');
    assert.equal(result.currentStep, 3);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test('Section 110: onboardingApi.getReadiness evaluates mandatory vs optional requirements', async () => {
  const originalFetch = globalThis.fetch;
  try {
    const mockReadiness: OnboardingReadiness = {
      isReady: false,
      totalMandatory: 9,
      satisfiedMandatory: 6,
      allMandatorySatisfied: false,
      requirements: [
        {
          key: 'business_name',
          title: 'Business Identity & Name',
          description: 'A registered business name.',
          stepNumber: 2,
          isSatisfied: true,
          isMandatory: true,
          actionRoute: '/provider/onboarding?step=2',
        },
        {
          key: 'retail_products',
          title: 'Retail Products',
          description: 'Physical merchandise.',
          stepNumber: 8,
          isSatisfied: false,
          isMandatory: false,
          actionRoute: '/provider/onboarding?step=8',
        },
        {
          key: 'active_service',
          title: 'Active Bookable Service',
          description: 'At least one published service.',
          stepNumber: 7,
          isSatisfied: false,
          isMandatory: true,
          actionRoute: '/provider/onboarding?step=7',
        },
      ],
    };

    globalThis.fetch = async (url: any, init: any) => {
      assert.equal(url, '/api/v1/onboarding/readiness');
      assert.equal(init?.headers?.Authorization, 'Bearer token-readiness');
      return new Response(JSON.stringify(mockReadiness), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    };

    const readiness = await onboardingApi.getReadiness('token-readiness');
    assert.equal(readiness.isReady, false);
    assert.equal(readiness.totalMandatory, 9);
    assert.equal(readiness.satisfiedMandatory, 6);

    const optionalReq = readiness.requirements.find((r) => r.key === 'retail_products');
    assert.equal(optionalReq?.isMandatory, false);

    const mandatoryServiceReq = readiness.requirements.find((r) => r.key === 'active_service');
    assert.equal(mandatoryServiceReq?.isMandatory, true);
    assert.equal(mandatoryServiceReq?.isSatisfied, false);
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test('Section 110: onboardingApi.publishStorefront throws descriptive error when mandatory prerequisites missing', async () => {
  const originalFetch = globalThis.fetch;
  try {
    globalThis.fetch = async () => {
      return new Response(
        JSON.stringify({
          success: false,
          isPublished: false,
          storefrontUrl: '',
          message: 'Storefront publication readiness check failed.',
          missingRequirements: ['Active Bookable Service', 'Operational Physical Location'],
        }),
        { status: 400, headers: { 'Content-Type': 'application/json' } }
      );
    };

    await assert.rejects(
      async () => {
        await onboardingApi.publishStorefront('test-token');
      },
      (err: any) => {
        assert.match(err.message, /Storefront publication readiness check failed/);
        assert.match(err.message, /Active Bookable Service/);
        assert.match(err.message, /Operational Physical Location/);
        return true;
      }
    );
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test('Section 110: onboardingApi.publishStorefront succeeds when storefront is ready', async () => {
  const originalFetch = globalThis.fetch;
  try {
    const successResult: PublishResult = {
      success: true,
      isPublished: true,
      storefrontUrl: '/business/aura-wellness',
      message: 'Provider storefront published successfully!',
    };

    globalThis.fetch = async (url: any, init: any) => {
      assert.equal(url, '/api/v1/onboarding/publish');
      assert.equal(init?.method, 'POST');
      assert.equal(init?.headers?.Authorization, 'Bearer valid-jwt');
      return new Response(JSON.stringify(successResult), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      });
    };

    const res = await onboardingApi.publishStorefront('valid-jwt');
    assert.equal(res.success, true);
    assert.equal(res.isPublished, true);
    assert.equal(res.storefrontUrl, '/business/aura-wellness');
  } finally {
    globalThis.fetch = originalFetch;
  }
});
