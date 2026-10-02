# Testing Strategy

1. **Unit Tests**: Domain model state machine, slot engine calculations, timezone logic, validation rules.
2. **Integration Tests**: EF Core query filters, repository implementations, MediatR command handlers, WebApplicationFactory integration endpoints.
3. **Architecture Tests**: Pure domain isolation enforcement (`Bookline.Domain` has 0 package references).
4. **Concurrency & E2E**: Double-booking race condition validation under concurrent requests.
