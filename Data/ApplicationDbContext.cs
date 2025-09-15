using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BussinessCupApi.Models;

namespace BussinessCupApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<League> Leagues { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Match> Matches { get; set; }
        public DbSet<Goal> Goals { get; set; }
        public DbSet<Card> Cards { get; set; }
        public DbSet<Week> Weeks { get; set; }
        public DbSet<MatchSquad> MatchSquads { get; set; }
        public DbSet<MatchSquadFormation> MatchSquadFormations { get; set; }
        public DbSet<Settings> Settings { get; set; }
        public DbSet<Season> Season { get; set; }
        public DbSet<Group> Group { get; set; }
        public DbSet<WeekBestTeams> WeekBestTeams { get; set; }
        public DbSet<WeekBestTeamPlayers> WeekBestTeamPlayers { get; set; }
        public DbSet<PlayerSuspensions> PlayerSuspension { get; set; }
        public DbSet<Advertise> Advertisements { get; set; }
        public DbSet<City> City { get; set; }
        public DbSet<MatchNews> MatchNews { get; set; }
        public DbSet<MatchNewsPhoto> MatchNewsPhotos { get; set; }
        public DbSet<PlayerTransferRequest> PlayerTransferRequest { get; set; }
        public DbSet<FavouriteTeams> FavouriteTeams { get; set; }
        public DbSet<MatchSquadSubstitution> MatchSquadSubstitutions { get; set; }
        public DbSet<LeagueRule> LeagueRules { get; set; }
        public DbSet<CityRestriction> CityRestrictions { get; set; }
      		public DbSet<LeagueRankingStatus> LeagueRankingStatus { get; set; }
		public DbSet<PhotoGallery> PhotoGalleries { get; set; }
		public DbSet<StaticKeyValue> StaticKeyValues { get; set; }
        		public DbSet<RichStaticContent> RichStaticContents { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User tablosu için konfigürasyon
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Firstname).IsRequired();
                entity.Property(e => e.Lastname).IsRequired();
            });

            modelBuilder
                .Entity<PlayerSuspensions>()
                .Property(e => e.SuspensionType)
                .HasConversion<string>();

            // Advertise için özel konfigürasyonlar (gerekirse)
            modelBuilder.Entity<Advertise>()
                .Property(a => a.UploadDate)
                .HasDefaultValueSql("GETUTCDATE()"); // SQL Server'da varsayılan UTC tarihi 

            modelBuilder.Entity<Goal>()
                .HasOne(g => g.Player)
                .WithMany(p => p.Goals)
                .HasForeignKey(g => g.PlayerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.HomeTeam)
                .WithMany(t => t.HomeMatches)
                .HasForeignKey(m => m.HomeTeamID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.AwayTeam)
                .WithMany(t => t.AwayMatches)
                .HasForeignKey(m => m.AwayTeamID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Match>()
                .HasOne(m => m.ManOfTheMatch)
                .WithMany()
                .HasForeignKey(m => m.ManOfTheMatchID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PlayerTransferRequest>()
                 .HasOne(p => p.User)
                 .WithMany()
                 .HasForeignKey(p => p.UserID)
                 .OnDelete(DeleteBehavior.Restrict); // ya da NoAction

            modelBuilder.Entity<PlayerTransferRequest>()
                .HasOne(p => p.RequestedCaptainUser)
                .WithMany()
                .HasForeignKey(p => p.RequestedCaptainUserID)
                .OnDelete(DeleteBehavior.Restrict); // ya da NoAction

            modelBuilder.Entity<PlayerTransferRequest>()
                .HasOne(p => p.ApprovalCaptainUser)
                .WithMany()
                .HasForeignKey(p => p.ApprovalCaptainUserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Week>()
                .HasOne(w => w.League)
                .WithMany(l => l.Weeks)
                .HasForeignKey(w => w.LeagueID)
                .OnDelete(DeleteBehavior.Restrict);


            // Week → Season
            modelBuilder.Entity<Week>()
                .HasOne(w => w.Season)
                .WithMany()
                .HasForeignKey(w => w.SeasonID)
                .OnDelete(DeleteBehavior.Restrict);




            modelBuilder.Entity<Card>()
                 .HasOne(c => c.Player)
                 .WithMany(p => p.Cards)
                 .HasForeignKey(c => c.PlayerID)
                 .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Card>()
                .HasOne(c => c.Match)
                .WithMany(m => m.Cards)
                .HasForeignKey(c => c.MatchID)
                .OnDelete(DeleteBehavior.Restrict);



            modelBuilder.Entity<MatchSquad>()
                .HasOne(ms => ms.Match)
                .WithMany(m => m.MatchSquads)
                .HasForeignKey(ms => ms.MatchID)
                .OnDelete(DeleteBehavior.Restrict);  // Cascade değil!

            modelBuilder.Entity<MatchSquad>()
                .HasOne(ms => ms.Player)
                .WithMany()
                .HasForeignKey(ms => ms.PlayerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MatchSquad>()
                .HasOne(ms => ms.Team)
                .WithMany()
                .HasForeignKey(ms => ms.TeamID)
                .OnDelete(DeleteBehavior.Restrict);


            modelBuilder.Entity<PlayerSuspensions>()
             .HasOne(ps => ps.Week)
             .WithMany()
             .HasForeignKey(ps => ps.WeekID)
             .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlayerSuspensions>()
                .HasOne(ps => ps.Player)
                .WithMany()
                .HasForeignKey(ps => ps.PlayerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlayerSuspensions>()
                .HasOne(ps => ps.League)
                .WithMany()
                .HasForeignKey(ps => ps.LeagueID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PlayerSuspensions>()
                .HasOne(ps => ps.Season)
                .WithMany()
                .HasForeignKey(ps => ps.SeasonID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MatchSquadSubstitution>()
                  .HasOne(ms => ms.Match)
                  .WithMany()
                  .HasForeignKey(ms => ms.MatchID)
                  .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MatchSquadSubstitution>()
                .HasOne(ms => ms.PlayerIn)
                .WithMany()
                .HasForeignKey(ms => ms.PlayerInID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MatchSquadSubstitution>()
                .HasOne(ms => ms.PlayerOut)
                .WithMany()
                .HasForeignKey(ms => ms.PlayerOutID)
                .OnDelete(DeleteBehavior.Restrict);

            // WeekBestTeams ilişkileri
            modelBuilder.Entity<WeekBestTeams>()
                .HasOne(wbt => wbt.Week)
                .WithMany()
                .HasForeignKey(wbt => wbt.WeekID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WeekBestTeams>()
                .HasOne(wbt => wbt.League)
                .WithMany()
                .HasForeignKey(wbt => wbt.LeagueID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WeekBestTeams>()
                .HasOne(wbt => wbt.Season)
                .WithMany()
                .HasForeignKey(wbt => wbt.SeasonID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WeekBestTeams>()
                .HasOne(wbt => wbt.BestPlayer)
                .WithMany()
                .HasForeignKey(wbt => wbt.BestPlayerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WeekBestTeams>()
                .HasOne(wbt => wbt.BestTeam)
                .WithMany()
                .HasForeignKey(wbt => wbt.BestTeamID)
                .OnDelete(DeleteBehavior.Restrict);

            // WeekBestTeamPlayers ilişkileri
            modelBuilder.Entity<WeekBestTeamPlayers>()
                .HasOne(wbp => wbp.WeekBestTeam)
                .WithMany(wbt => wbt.Players)
                .HasForeignKey(wbp => wbp.WeekBestTeamID)
                .OnDelete(DeleteBehavior.Cascade); // Burada Cascade olabilir

            modelBuilder.Entity<WeekBestTeamPlayers>()
                .HasOne(wbp => wbp.Player)
                .WithMany()
                .HasForeignKey(wbp => wbp.PlayerID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Goal>(entity =>
            {

                entity.ToTable("Goals", tb => tb.HasTrigger("TR_UpdatePlayerValueOnGoal"));
            });
            modelBuilder.Entity<Card>(entity =>
            {

                entity.ToTable("Cards", tb => tb.HasTrigger("TR_UpdatePlayerValueOnCard"));
            });
            modelBuilder.Entity<City>(entity =>
            {

                entity.ToTable("City", tb => tb.HasTrigger("trg_AfterInsert_City"));
            });

            modelBuilder.Entity<PhotoGallery>(entity =>
            {
                entity.Property(p => p.Category).IsRequired();
                entity.Property(p => p.FileName).IsRequired();
                entity.Property(p => p.FilePath).IsRequired();
                entity.Property(p => p.UploadedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
}