using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace CSharpAbilities
{
    public class Entity<T>
    {
        public virtual int Id { get; set; }
        public virtual T? Value { get; set; }
    }
    public class EntityFrameworkDbContext:DbContext
    {
        public virtual DbSet<Entity<string>>? Entities { get; }
        public EntityFrameworkDbContext(DbContextOptions<EntityFrameworkDbContext> options):base(options)
        {
            Database.EnsureCreated();
        }
        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            if (!builder.IsConfigured)
            {
                builder.UseLazyLoadingProxies().UseSqlServer($"Database=Generated_{Guid.NewGuid()};Server={Environment.MachineName}\\CSharpAbilities;User ID={File.ReadAllText(Path.Combine(DriveInfo.GetDrives().FirstOrDefault()!.RootDirectory.FullName, "login.txt"))};Password={File.ReadAllText(Path.Combine(DriveInfo.GetDrives().FirstOrDefault()!.RootDirectory.FullName, "password.txt"))};TrustServerCertificate=True;");
            }
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new EntityConfiguration());
        }
    }
    public class EntityConfiguration : IEntityTypeConfiguration<Entity<string>>
    {
        public void Configure(EntityTypeBuilder<Entity<string>> builder)
        {
            builder.Property(ent => ent.Value).HasColumnName("Value");
            builder.HasQueryFilter(ent => ent.GetHashCode() > 0);
            builder.HasKey(ent => ent.Id);
            builder.HasIndex(ent => ent.Value);
        }
    }
}
