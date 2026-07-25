# Use Clean Architecture project boundaries

Structure the service as Domain, Application, Infrastructure, and API projects. Application owns its outbound interfaces in a single `Application/Abstractions` folder and organizes use cases vertically under `Application/Books`; Infrastructure implements those interfaces with EF Core and SQL Server, while API remains the composition root.
