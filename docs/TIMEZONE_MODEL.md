# Bookline Timezone & NodaTime Domain Model

## Core Philosophy

Time is a fundamental domain concept in appointment scheduling. Bookline uses NodaTime to avoid naive `DateTime` bugs, timezone shifts, or daylight saving time errors.

## NodaTime Data Types

- **`Instant`**: Universal point in time stored canonical in UTC (`StartUtc`, `EndUtc`).
- **`LocalDate`**: Local calendar date (e.g. `2026-10-05`) for date pickers and day queries.
- **`LocalTime`**: Wall-clock time (e.g. `09:00:00`) for staff working hours and breaks.
- **`DateTimeZone`**: Location timezone (e.g. `America/New_York`, `Asia/Kolkata`, `Europe/London`).
- **`Duration`**: Interval lengths (service duration, buffer minutes).

## JSON Serialization

API responses map NodaTime types to standard ISO-8601 strings:
- `Instant` $\rightarrow$ `2026-10-05T09:00:00Z`
- `LocalDate` $\rightarrow$ `2026-10-05`

The custom `InstantJsonConverter` handles standard ISO-8601 formatting across all public endpoints.
