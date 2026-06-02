using System.Data.Entity;

namespace SnowTrolleyProduction.Models
{
    public class MaterialesDbContext : DbContext
    {
        public MaterialesDbContext() : base("name=BAESystemsGuaymasEntitiesSmtPlan") { }

        public DbSet<Material> Materiales { get; set; }

        protected override void OnModelCreating(DbModelBuilder mb)
        {
            mb.Entity<Material>()
                .ToTable("Materiales", "Process")
                .HasKey(m => m.Id);

            mb.Entity<Material>()
                .Ignore(m => m.Programa);
        }
    }
}
