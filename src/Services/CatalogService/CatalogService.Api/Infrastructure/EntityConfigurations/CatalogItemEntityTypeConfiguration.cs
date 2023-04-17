using CatalogService.Api.Core.Domain;
using CatalogService.Api.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CatalogService.Api.Infrastructure.EntityConfigurations
{
    public class CatalogItemEntityTypeConfiguration : IEntityTypeConfiguration<CatalogItem>
    {
        public void Configure(EntityTypeBuilder<CatalogItem> builder)
        {
            builder.ToTable("Catalog", CatalogContext.DEFAULT_SCHEMA);

            builder.HasKey(ci => ci.Id);

            builder.Property(ci => ci.Id).UseHiLo("catalog_hilo").IsRequired();

            builder.Property(ci => ci.Name).IsRequired().HasMaxLength(50);

            builder.Property(ci => ci.Name).IsRequired().HasMaxLength(100);

            builder.Property(ci => ci.Price);

            builder.Ignore(ci=>ci.PictureUrl);

            builder.HasOne(ci => ci.CatalogBrand).WithMany().HasForeignKey(ci=>ci.CatalogBrandId);

            builder.HasOne(ci => ci.CatalogType).WithMany().HasForeignKey(ci => ci.CatalogTypeId);
        }
    }
}
