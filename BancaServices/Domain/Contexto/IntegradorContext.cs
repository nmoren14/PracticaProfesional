namespace BancaServices.Contexto
{
    using BancaServices.ContextoTablas.Integrador;
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration.Conventions;

    public partial class IntegradorContext : DbContext
    {
        public IntegradorContext() : base("IntegradorContext")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
        }

        public virtual DbSet<ParametrosDomiciliacion> ParametrosDomiciliacion { get; set; }
        public virtual DbSet<entidades> entidades { get; set; }
    }
}