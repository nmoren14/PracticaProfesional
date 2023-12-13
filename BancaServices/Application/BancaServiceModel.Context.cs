using Microsoft.EntityFrameworkCore;
using BancaServices.ContextoTablas.Desembolso;

namespace BancaServices
{
    public partial class BancaServicesLogsEntities : DbContext
    {
        public DbSet<AvanceTcoLog> AvanceTcoLogs { get; set; }
        public DbSet<DTS_RETEFUENTE_CDT> DTS_RETEFUENTE_CDTs { get; set; }
        public DbSet<PagosEibsCms> PagosEibsCms { get; set; }
        public DbSet<HomologacionTarjetaLog> HomologacionTarjetaLogs { get; set; }
        public DbSet<TransaccionesPSE> TransaccionesPSEs { get; set; }
        public DbSet<CreditoRotativoLogs> CreditoRotativoLogs { get; set; }
        public DbSet<TasaVigente> TasaVigentes { get; set; }
        public DbSet<RediferidosLog> RediferidosLogs { get; set; }
        public DbSet<CampCuentasAhorros> CampCuentasAhorros { get; set; }
        public DbSet<UsuariosBloqueadosLog> UsuariosBloqueadosLogs { get; set; }
        public DbSet<TerminosYCondicionesRediferidos> TerminosYCondicionesRediferidos { get; set; }
        public DbSet<PagosProductosTereceros> PagosProductosTereceros { get; set; }
        public DbSet<DTS_RETEFUENTE_AHO> DTS_RETEFUENTE_AHOs { get; set; }
        public DbSet<ValidacioneRediferidos> ValidacioneRediferidos { get; set; }
        public DbSet<CalificacionClientes> CalificacionClientes { get; set; }
        public DbSet<ClientesPeriodoGracia> ClientesPeriodoGracia { get; set; }
        public DbSet<ParametrosGenerales> ParametrosGenerales { get; set; }
        public DbSet<CreditCardBin> CreditCardBins { get; set; }
        public DbSet<BloquearProductosLog> BloquearProductosLogs { get; set; }
        public DbSet<NotificacionAgilLog> NotificacionAgilLogs { get; set; }
        public DbSet<RenovacionTC> RenovacionTCs { get; set; }
        public DbSet<Parametros> Parametros { get; set; }
        public DbSet<CertificadosLog> CertificadosLogs { get; set; }
        public DbSet<TarjetaVirtualLog> TarjetaVirtualLogs { get; set; }
        public DbSet<PrimerAvanceTCOLog> PrimerAvanceTCOLogs { get; set; }
        public DbSet<CuerpoRenovacionCargue> CuerpoRenovacionCargues { get; set; }
        public DbSet<RenovacionCargue> RenovacionCargues { get; set; }
        public DbSet<RenovacionCargueLog> RenovacionCargueLogs { get; set; }
        public DbSet<RenovacionMasivoLog> RenovacionMasivoLogs { get; set; }
        public DbSet<Condicion> Condiciones { get; set; }
        public DbSet<DesembolsoLog> DesembolsoLogs { get; set; }
        //public DbSet<DTS_RETEFUENTE> DTS_RETEFUENTE { get; set; }
        public BancaServicesLogsEntities(DbContextOptions<BancaServicesLogsEntities> options) : base(options)
        {
        }
        public BancaServicesLogsEntities()
        {
            // Constructor sin parámetros
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Define relaciones y configuraciones aquí si es necesario.
        }
    }
}