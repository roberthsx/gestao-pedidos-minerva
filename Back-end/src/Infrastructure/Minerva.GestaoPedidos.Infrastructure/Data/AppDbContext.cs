using Microsoft.EntityFrameworkCore;
using Minerva.GestaoPedidos.Domain.Entities;

namespace Minerva.GestaoPedidos.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PaymentCondition> PaymentConditions => Set<PaymentCondition>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<DeliveryTerm> DeliveryTerms => Set<DeliveryTerm>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Profile>(builder =>
        {
            builder.ToTable("Profiles");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
            builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        });

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("Users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
            builder.Property(u => u.RegistrationNumber).HasMaxLength(20);
            builder.Property(u => u.PasswordHash).HasMaxLength(255);
            builder.HasOne(u => u.Profile)
                .WithMany()
                .HasForeignKey(u => u.ProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(u => u.RegistrationNumber).IsUnique().HasFilter("\"RegistrationNumber\" IS NOT NULL");
        });

        modelBuilder.Entity<Customer>(builder =>
        {
            builder.ToTable("Customers");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(c => c.Email)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(c => c.CreatedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<PaymentCondition>(builder =>
        {
            builder.ToTable("PaymentConditions");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Description)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.NumberOfInstallments)
                .IsRequired();

            builder.Property(p => p.CreatedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("Orders");

            builder.HasKey(o => o.Id);

            builder.HasIndex(o => o.IdempotencyKey)
                .IsUnique();

            builder.Property(o => o.IdempotencyKey)
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(o => o.RequiresManualApproval)
                .IsRequired();

            builder.Property(o => o.OrderDate)
                .IsRequired();

            builder.Property(o => o.CreatedAt)
                .IsRequired();

            builder.Property(o => o.ApprovedBy)
                .HasMaxLength(20);

            builder.Property(o => o.ApprovedAt);

            builder.HasOne(o => o.Customer)
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.PaymentCondition)
                .WithMany()
                .HasForeignKey(o => o.PaymentConditionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(o => o.DeliveryTerm)
                .WithOne()
                .HasForeignKey<DeliveryTerm>(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(builder =>
        {
            builder.ToTable("OrderItems");

            builder.HasKey(oi => oi.Id);

            builder.Property(oi => oi.ProductName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(oi => oi.Quantity)
                .IsRequired();

            builder.Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(oi => oi.TotalPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
        });

        modelBuilder.Entity<DeliveryTerm>(builder =>
        {
            builder.ToTable("DeliveryTerms");

            builder.HasKey(d => d.Id);

            builder.HasIndex(d => d.OrderId)
                .IsUnique();

            builder.Property(d => d.DeliveryDays)
                .IsRequired();

            builder.Property(d => d.EstimatedDeliveryDate)
                .IsRequired();

            builder.Property(d => d.CreatedAt)
                .IsRequired();
        });

    }
}

