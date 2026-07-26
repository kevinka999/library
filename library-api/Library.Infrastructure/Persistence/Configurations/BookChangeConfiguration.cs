using Library.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

internal sealed class BookChangeConfiguration : IEntityTypeConfiguration<BookChangeRecord>
{
    private static readonly string[] StableFieldNames =
        ["title", "shortDescription", "publishDate", "authors"];

    public void Configure(EntityTypeBuilder<BookChangeRecord> builder)
    {
        builder.ToTable("BookChanges", table =>
        {
            table.HasCheckConstraint(
                "CK_BookChanges_ChangedField",
                $"\"ChangedField\" IN ({string.Join(", ", StableFieldNames.Select(name => $"'{name}'"))})");
        });

        builder.HasKey(change => change.Id);
        builder.Property(change => change.Id).UseIdentityByDefaultColumn();
        builder.Property(change => change.ChangeSetId).IsRequired();
        builder.Property(change => change.ChangedField).HasMaxLength(32).IsRequired();
        builder.Property(change => change.OldValue).HasColumnType("jsonb");
        builder.Property(change => change.NewValue).HasColumnType("jsonb").IsRequired();
        builder.Property(change => change.ChangedAt).HasColumnType("timestamp with time zone").IsRequired();

        builder
            .HasOne(change => change.Book)
            .WithMany()
            .HasForeignKey(change => change.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(change => new { change.BookId, change.ChangeSetId, change.ChangedField })
            .IsUnique();

        builder.HasIndex(change => new
        {
            change.BookId,
            change.ChangedAt,
            change.ChangeSetId
        });

        builder.HasIndex(change => new
        {
            change.BookId,
            change.ChangedField,
            change.ChangedAt,
            change.ChangeSetId
        });
    }
}
