# Bookline Availability Engine

The availability engine computes precise time slots based on:
1. Staff working hours intervals for the requested date.
2. Active time-off records and breaks.
3. Existing bookings (`Pending`, `Confirmed`).
4. Service duration + preparation/clean-up buffer times.
5. Active 5-minute Redis slot holds.
