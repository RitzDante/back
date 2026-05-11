using ActiveCitizen.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ActiveCitizen.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<ViolationType> ViolationTypes { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<Claim> Claims { get; set; }   
        

        protected override void  OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId);

            modelBuilder.Entity<User>()
                .HasOne(u => u.District)
                .WithMany()
                .HasForeignKey(u => u.DistrictId)
                .IsRequired(false);

            modelBuilder.Entity<Claim>()
                .HasOne(c => c.User)
                .WithMany(u => u.Claims)
                .HasForeignKey(c => c.UserId);
        }
    }
}
