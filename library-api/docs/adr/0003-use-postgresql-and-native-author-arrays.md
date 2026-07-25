# Use PostgreSQL and native author arrays

Use PostgreSQL 18 through Npgsql instead of SQL Server because its official container runs natively on both ARM64 development machines and x86-64 deployment hosts. Store author names in a native `text[]` column and Book Change values in `jsonb`, preserving the two-table model while supporting relational transactions and queryable collection data.
