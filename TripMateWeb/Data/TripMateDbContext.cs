using Microsoft.EntityFrameworkCore;
using TripMateWeb.Models;

namespace TripMateWeb.Data
{
    public class TripMateDbContext : DbContext
    {
        public TripMateDbContext(DbContextOptions<TripMateDbContext> options) : base(options)
        {
        }

        public DbSet<Profile> Profiles { get; set; }
        public DbSet<TourTemplate> TourTemplates { get; set; }
        public DbSet<GuideTour> GuideTours { get; set; }
        public DbSet<TourAvailability> TourAvailabilities { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<GuideCertificate> GuideCertificates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Profile
            modelBuilder.Entity<Profile>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Role).HasDefaultValue("traveler");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("NOW()");
            });

            // Configure GuideTour relationships
            modelBuilder.Entity<GuideTour>(entity =>
            {
                entity.HasOne(d => d.TourTemplate)
                    .WithMany(p => p.GuideTours)
                    .HasForeignKey(d => d.TourTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Guide)
                    .WithMany(p => p.GuideTours)
                    .HasForeignKey(d => d.GuideId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Status).HasDefaultValue("active");
                entity.Property(e => e.Rating).HasDefaultValue(0);
                entity.Property(e => e.TotalReviews).HasDefaultValue(0);
                entity.Property(e => e.MaxParticipants).HasDefaultValue(10);
            });

            // Configure Booking relationships
            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasOne(d => d.GuideTour)
                    .WithMany(p => p.Bookings)
                    .HasForeignKey(d => d.GuideTourId);

                entity.HasOne(d => d.Traveler)
                    .WithMany(p => p.TravelerBookings)
                    .HasForeignKey(d => d.TravelerId);

                entity.Property(e => e.Status).HasDefaultValue("pending");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // Configure Payment relationship
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasOne(d => d.Booking)
                    .WithOne(p => p.Payment)
                    .HasForeignKey<Payment>(d => d.BookingId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Status).HasDefaultValue("pending");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // Configure Review relationships
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasOne(d => d.GuideTour)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(d => d.GuideTourId);

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Reviews)
                    .HasForeignKey(d => d.UserId);

                entity.HasOne(d => d.Booking)
                    .WithOne(p => p.Review)
                    .HasForeignKey<Review>(d => d.BookingId);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // Configure Conversation relationships
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasOne(d => d.Traveler)
                    .WithMany(p => p.TravelerConversations)
                    .HasForeignKey(d => d.TravelerId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Guide)
                    .WithMany(p => p.GuideConversations)
                    .HasForeignKey(d => d.GuideId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Booking)
                    .WithOne(p => p.Conversation)
                    .HasForeignKey<Conversation>(d => d.BookingId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // Configure Message relationships
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasOne(d => d.Conversation)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(d => d.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Sender)
                    .WithMany(p => p.Messages)
                    .HasForeignKey(d => d.SenderId);

                entity.Property(e => e.IsRead).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // Configure GuideCertificate relationships
            modelBuilder.Entity<GuideCertificate>(entity =>
            {
                entity.HasOne(d => d.Guide)
                    .WithMany(p => p.GuideCertificates)
                    .HasForeignKey(d => d.GuideId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Status).HasDefaultValue("pending");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
            });

            // Configure TourAvailability relationships
            modelBuilder.Entity<TourAvailability>(entity =>
            {
                entity.HasOne(d => d.GuideTour)
                    .WithMany(p => p.Availabilities)
                    .HasForeignKey(d => d.GuideTourId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure array types for PostgreSQL
            modelBuilder.Entity<TourTemplate>()
                .Property(e => e.Images)
                .HasColumnType("text[]");
        }
    }
}