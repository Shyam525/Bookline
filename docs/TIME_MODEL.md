# NodaTime Temporal Architecture

- All appointment start and end times are stored in UTC as NodaTime `Instant`.
- Staff and business operating hours are modeled as local date/time (`LocalDate`, `LocalTime`).
- Timezone conversions rely on `DateTimeZoneProviders.Tzdb` with IANA timezone strings (`Asia/Kolkata`, `America/New_York`).
