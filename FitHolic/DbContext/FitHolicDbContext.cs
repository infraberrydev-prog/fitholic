using FitHolic.Models;
using Microsoft.EntityFrameworkCore;

namespace FitHolic
{
    public class FitHolicDbContext : DbContext
    {
        public FitHolicDbContext()
        {
        }

        public FitHolicDbContext(DbContextOptions<FitHolicDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserLogin> UserLogins { get; set; }
        public DbSet<UserOTP> UserOtps { get; set; }
        public DbSet<GymReport> GymReports => Set<GymReport>();
        public DbSet<GymReportRow> GymReportRows => Set<GymReportRow>();
        public DbSet<PackageDto> Package { get; set; }
        public DbSet<ContractDurationDto> ContractDuration { get; set; }
        public DbSet<GymExpensesReport> GymExpenses { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.

            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=fitholic;Username=postgres;Password=uatpostgres");
            }
        }
    }
}
