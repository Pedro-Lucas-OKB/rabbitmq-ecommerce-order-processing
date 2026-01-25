using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderProcessing.Core.Entities;

namespace OrderProcessing.Infrastructure.Data.Mappings;

public class OrderMap : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(order => order.Id);
        
        builder.Property(order => order.Id)
            .ValueGeneratedOnAdd();       
        
        builder.Property(order => order.CustomerName)
            .IsRequired()
            .HasMaxLength(100);       
        
        builder.Property(order => order.CustomerEmail)
            .IsRequired();       
        
        builder.Property(order => order.TotalAmount)
            .IsRequired();       
        
        builder.Property(order => order.OrderStatus)
            .IsRequired()
            .HasConversion<string>();       
        
        builder.Property(order => order.PaymentStatus)
            .IsRequired()
            .HasConversion<string>();       
        
        builder.Property(order => order.InventoryStatus)
            .IsRequired()
            .HasConversion<string>();       
        
        // Relacionamentos
        builder.HasMany(order => order.Items)
            .WithOne(orderItem => orderItem.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);       
    }
}