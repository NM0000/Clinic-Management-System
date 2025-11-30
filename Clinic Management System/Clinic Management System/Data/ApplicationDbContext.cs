using Clinic_Management_System.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;


namespace Clinic_Management_System.Data
{
    /// <summary>
    /// EF Core <see cref="DbContext"/> for the Clinic Management System that includes Identity and application entities.
    /// </summary>
    public class ApplicationDbContext: IdentityDbContext<AppUser>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDbContext"/> class.
        /// </summary>
        /// <param name="options">The options used by a <see cref="DbContext"/>.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext>options): base(options)
        { 
        }

        /// <summary>
        /// Gets or sets the doctors in the system.
        /// </summary>
        public DbSet<Doctor> Doctors { get; set; }

        /// <summary>
        /// Gets or sets the patients in the system.
        /// </summary>
        public DbSet<Patient> Patients { get; set; }

        /// <summary>
        /// Gets or sets the Doctor schedules in the system.
        /// </summary>
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; }

        /// <summary>
        /// Gets or sets the Appointments in the system.
        /// </summary>
        public DbSet<Appointment> Appointments { get; set; }

        /// <summary>
        /// Configure the EF Core model for the application.
        /// </summary>
        /// <param name="builder">The <see cref="ModelBuilder"/> used to configure entity mappings.</param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                            v => v.ToUniversalTime(),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                    }
                }
            }

            base.OnModelCreating(builder);
            // Configure Doctor-User relationship
            builder.Entity<Doctor>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Global Query Filter for Soft Delete
            builder.Entity<Patient>()
                .HasQueryFilter(p => !p.IsDeleted);

            // Configure DoctorSchedule-Doctor relationship
            builder.Entity<DoctorSchedule>()
                .HasOne(ds => ds.Doctor)
                .WithMany()
                .HasForeignKey(ds => ds.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Appointment-Patient relationship
            builder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // Global Query Filter for Appointments - exclude appointments with soft-deleted patients
            builder.Entity<Appointment>()
                .HasQueryFilter(a => !a.Patient!.IsDeleted);

            // Configure Appointment-Doctor relationship
            builder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for faster appointment queries
            builder.Entity<Appointment>()
                .HasIndex(a => new { a.DoctorId, a.AppointmentDate });


        }
    }
}
