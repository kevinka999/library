using Library.Domain.Books;
using Library.Infrastructure.Persistence.Records;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Infrastructure.Persistence.Configurations;

internal sealed class BookConfiguration : IEntityTypeConfiguration<BookRecord>
{
    public void Configure(EntityTypeBuilder<BookRecord> builder)
    {
        builder.ToTable("Books", table =>
        {
            table.HasCheckConstraint("CK_Books_Title_NotBlank", "length(btrim(\"Title\")) > 0");
            table.HasCheckConstraint("CK_Books_ShortDescription_NotBlank", "length(btrim(\"ShortDescription\")) > 0");
            table.HasCheckConstraint("CK_Books_Authors_NotEmpty", "cardinality(\"Authors\") > 0");
            table.HasCheckConstraint("CK_Books_Version_Positive", "\"Version\" > 0");
        });

        builder.HasKey(book => book.Id);
        builder.Property(book => book.Id).UseIdentityByDefaultColumn();
        builder.Property(book => book.Title).HasMaxLength(Book.MaxTitleLength).IsRequired();
        builder.Property(book => book.ShortDescription).HasMaxLength(Book.MaxShortDescriptionLength).IsRequired();
        builder.Property(book => book.PublishDate).HasColumnType("date").IsRequired();
        builder.Property(book => book.Version).IsRequired().IsConcurrencyToken();

        builder.Property(book => book.Authors)
            .HasColumnType("text[]")
            .IsRequired();
    }
}
