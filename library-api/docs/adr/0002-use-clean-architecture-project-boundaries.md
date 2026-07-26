# Use Clean Architecture project boundaries

Structure the service as Domain, Application, Infrastructure, and API projects. Application owns its outbound interfaces in a single `Application/Abstractions` folder and organizes use cases vertically under `Application/Handlers`; Infrastructure implements those interfaces with EF Core and PostgreSQL, while API remains the composition root.
