using yayasanApi.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using yayasanApi.Model.MasterData;
using yayasanApi.Model.Transaksi;


namespace yayasanApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }


        public DbSet<MasterUnit> MasterUnit { get; set; }
        public DbSet<MasterCoa> MasterCoa { get; set; }
        public DbSet<LaporanKeuangan> LaporanKeuangan { get; set; }
        public DbSet<MapingCoa> MapingCoa { get; set; }
        public DbSet<EleminasiKeuangan> EleminasiKeuangan { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Unit -> Laporan (one-to-many)
            builder.Entity<MasterUnit>()
                .HasMany(d => d.LaporanKeuangan)
                .WithOne(k => k.MasterUnit)
                .HasForeignKey(k => k.UnitId)
                .OnDelete(DeleteBehavior.Restrict); // atur sesuai kebutuhan: Restrict / Cascade

            builder.Entity<MapingCoa>()
                .HasOne(m => m.MasterUnit)
                .WithMany(u => u.MappingCoa)
                .HasForeignKey(m => m.UnitId)
                .OnDelete(DeleteBehavior.Restrict);


            builder.Entity<MasterCoa>()
                .Property(x => x.Tipe)
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Entity<MasterUnit>()
                .Property(x => x.Jenis)
                .HasConversion<string>()
                .HasMaxLength(50);

            // Tambahkan index untuk performa query
            builder.Entity<MasterUnit>()
                .HasIndex(x => x.Kode);

            builder.Entity<MasterUnit>()
                .HasIndex(x => x.Nama);

            builder.Entity<MasterCoa>()
                .HasIndex(x => x.Kode);

            builder.Entity<MasterCoa>()
                .HasIndex(x => x.Nama);

            builder.Entity<LaporanKeuangan>()
                .HasIndex(x => x.Periode);

            builder.Entity<LaporanKeuangan>()
                .HasIndex(x => x.UnitId);

            builder.Entity<LaporanKeuangan>()
                .HasIndex(x => x.Kode);

            builder.Entity<EleminasiKeuangan>()
               .HasIndex(x => x.Periode);

            builder.Entity<EleminasiKeuangan>()
               .HasIndex(x => x.Kode);
        }

    }
}
