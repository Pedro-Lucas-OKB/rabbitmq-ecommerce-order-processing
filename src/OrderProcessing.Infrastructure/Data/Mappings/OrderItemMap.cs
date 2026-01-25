using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderProcessing.Core.Entities;

namespace OrderProcessing.Infrastructure.Data.Mappings;

public class OrderItemMap : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        
        builder.HasKey(item => item.Id);       
        
        builder.Property(item => item.Id)
            .ValueGeneratedOnAdd();       
        
        builder.Property(item => item.ProductName)
            .IsRequired()
            .HasMaxLength(200);       
        
        builder.Property(item => item.ProductSku)
            .IsRequired()
            .HasMaxLength(50);       
        
        builder.Property(item => item.Quantity)
            .IsRequired();       
        
        builder.Property(item => item.UnitPrice)
            .IsRequired();       
        
        builder.Property(item => item.TotalPrice)
            .IsRequired();       
    }
}