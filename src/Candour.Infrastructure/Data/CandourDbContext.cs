namespace Candour.Infrastructure.Data;

using Candour.Core.Entities;
using Microsoft.EntityFrameworkCore;

public class CandourDbContext : DbContext
{
    public CandourDbContext(DbContextOptions<CandourDbContext> options) : base(options) { }

    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<SurveyResponse> Responses => Set<SurveyResponse>();
    public DbSet<UsedToken> UsedTokens => Set<UsedToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Survey
        modelBuilder.Entity<Survey>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Title).HasMaxLength(500).IsRequired();
            e.Property(s => s.Description).HasMaxLength(5000);
            e.Property(s => s.Settings).HasColumnType("jsonb");
            e.Property(s => s.BatchSecret).HasMaxLength(500);
            e.HasMany(s => s.Questions).WithOne(q => q.Survey).HasForeignKey(q => q.SurveyId);
        });

        // Question
        modelBuilder.Entity<Question>(e =>
        {
            e.HasKey(q => q.Id);
            e.Property(q => q.Text).HasMaxLength(2000).IsRequired();
            e.Property(q => q.Options).HasColumnType("jsonb");
            e.Property(q => q.Settings).HasColumnType("jsonb");
        });

        // SurveyResponse — DELIBERATELY NO identity fields, NO navigation to UsedToken
        modelBuilder.Entity<SurveyResponse>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Answers).HasColumnType("jsonb").IsRequired();
            e.HasIndex(r => r.SurveyId); // For aggregate queries only
            // NO navigation property to any identity table
            // NO foreign key to UsedToken
        });

        // UsedToken — ISOLATED from SurveyResponse
        modelBuilder.Entity<UsedToken>(e =>
        {
            e.HasKey(t => t.TokenHash);
            e.Property(t => t.TokenHash).HasMaxLength(64); // SHA256 hex
            e.HasIndex(t => new { t.TokenHash, t.SurveyId }).IsUnique();
            // NO navigation property to SurveyResponse
        });
    }
}
