using Microsoft.EntityFrameworkCore;
using RegistrationFormProject.Models;

namespace RegistrationFormProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<UserMaster> UserMasters { get; set; }
        public DbSet<UserDocument> UserDocuments { get; set; }
        public DbSet<RoleMaster> RoleMasters { get; set; }
        public DbSet<PasswordResetOtp> PasswordResetOtps { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RoleMaster>()
      .HasKey(r => r.RoleId);

            modelBuilder.Entity<UserDocument>()
    .Property(x => x.UploadedDate)
    .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<UserDocument>()
      .Property(x => x.VerifiedDate)
      .HasColumnType("timestamp without time zone");

            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UserMaster>()
                .Property(x => x.DOB)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<UserMaster>()
                .Property(x => x.CreatedDate)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<ErrorLog>()
                .Property(x => x.LoggedDate)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<ActivityLog>()
                .Property(x => x.LoggedDate)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<UserMaster>()
                .HasIndex(x => x.UserName)
                .IsUnique();

            modelBuilder.Entity<UserMaster>()
                .HasIndex(x => x.EmailID)
                .IsUnique();

            modelBuilder.Entity<UserMaster>()
                .HasIndex(x => x.MobileNo)
                .IsUnique();
        }

    }

}
