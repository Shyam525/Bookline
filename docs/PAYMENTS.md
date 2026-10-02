# Payment Integration Architecture

- `IPaymentProvider` abstraction supports optional payment gateway integration (Stripe, Razorpay).
- Local operation works with payment processing optional or disabled.
- Deposit tracking and status transitions are recorded against appointment records.
