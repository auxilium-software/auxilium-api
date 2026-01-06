using AuxiliumAPI.Common.EntityModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.IO;

namespace AuxiliumAPI.Common.EF;

public class AuxiliumDbContext : DbContext
{
    public AuxiliumDbContext(DbContextOptions<AuxiliumDbContext> options)
        : base(options)
    {
    }



    public DbSet<UserModel> Users { get; set; }
    public DbSet<CaseModel> Cases { get; set; }
    public DbSet<CaseWorkerModel> CaseWorkers { get; set; }
    public DbSet<CaseClientModel> CaseClients { get; set; }
    public DbSet<CaseAdditionalPropertyModel> CaseAdditionalProperties { get; set; }
    public DbSet<UserAdditionalPropertyModel> UserAdditionalProperties { get; set; }
    public DbSet<CaseMessageModel> CaseMessages { get; set; }
    public DbSet<CaseMessageReadByModel> CaseMessagesReadBys { get; set; }
    public DbSet<CaseFileModel> CaseFiles { get; set; }
    public DbSet<UserFileModel> UserFiles { get; set; }
    public DbSet<CaseTodoModel> CaseTodos { get; set; }
    public DbSet<CaseTimelineItemModel> CaseTimeline { get; set; }
    public DbSet<RefreshTokenModel> RefreshTokens { get; set; }



    private static readonly ValueConverter<Guid, string> GuidToStringConverter =
        new ValueConverter<Guid, string>(
            g => g.ToString(),
            s => Guid.Parse(s)
        );



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // users
        modelBuilder.Entity<UserModel>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            


            entity.Property(e => e.Id)              .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CreatedAt)       .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.CreatedBy)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.LastUpdatedAt)   .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.LastUpdatedBy)   .HasColumnType("char(36)")                                              .HasConversion(GuidToStringConverter)                   .IsRequired();

            entity.Property(e => e.EmailAddress)    .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.PasswordHash)    .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.IsAdmin)         .HasColumnType("tinyint(1)")    .HasDefaultValue(false)                                                                         .IsRequired();
            entity.Property(e => e.IsCaseWorker)    .HasColumnType("tinyint(1)")    .HasDefaultValue(false)                                                                         .IsRequired();



            entity.HasIndex(
                e => e.EmailAddress
            ).IsUnique();
        });

        // cases
        modelBuilder.Entity<CaseModel>(entity =>
        {
            entity.ToTable("cases");
            entity.HasKey(e => e.Id);
            


            entity.Property(e => e.Id)              .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CreatedAt)       .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.CreatedBy)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.LastUpdatedAt)   .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.LastUpdatedBy)   .HasColumnType("char(36)")                                              .HasConversion(GuidToStringConverter)                   .IsRequired();

            entity.Property(e => e.Title)           .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.Description)     .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.Status)          .HasColumnType("text")          .HasDefaultValueSql("open")                                                                     .IsRequired();
            entity.Property(e => e.Sensitivity)     .HasColumnType("text")          .HasDefaultValueSql("confidential")                                                             .IsRequired();



            entity.HasOne(e => e.CreatedByUser)     .WithMany()                     .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser) .WithMany()                     .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Workers)          .WithOne(w => w.Case)           .HasForeignKey(w => w.CaseId)           .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Clients)          .WithOne(c => c.Case)           .HasForeignKey(c => c.CaseId)           .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.AdditionalProperties).WithOne(p => p.Case)        .HasForeignKey(p => p.CaseId)           .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Messages)         .WithOne(m => m.Case)           .HasForeignKey(m => m.CaseId)           .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Files)            .WithOne(f => f.Case)           .HasForeignKey(f => f.CaseId)           .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Todos)            .WithOne(t => t.Case)           .HasForeignKey(t => t.CaseId)           .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Timeline)         .WithOne()                      .HasForeignKey("CaseId")                .OnDelete(DeleteBehavior.Cascade);
        });

        // case_workers
        modelBuilder.Entity<CaseWorkerModel>(entity =>
        {
            entity.ToTable("case_workers");
            entity.HasKey(e => e.Id);
            


            entity.Property(e => e.Id)              .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CreatedAt)       .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.CreatedBy)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();

            entity.Property(e => e.CaseId)          .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.UserId)          .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();



            entity.HasOne(e => e.CreatedByUser)     .WithMany()                     .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)              .WithMany(c => c.Workers)       .HasForeignKey(e => e.CaseId);
            entity.HasOne(e => e.User)              .WithMany(u => u.WorkerOnCases) .HasForeignKey(e => e.UserId);
        });

        // case_clients
        modelBuilder.Entity<CaseClientModel>(entity =>
        {
            entity.ToTable("case_clients");
            entity.HasKey(e => e.Id);
            


            entity.Property(e => e.Id)              .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CreatedAt)       .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.CreatedBy)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();

            entity.Property(e => e.CaseId)          .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.UserId)          .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();



            entity.HasOne(e => e.CreatedByUser)     .WithMany()                     .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)              .WithMany(c => c.Clients)       .HasForeignKey(e => e.CaseId);
            entity.HasOne(e => e.User)              .WithMany(u => u.ClientOnCases) .HasForeignKey(e => e.UserId);
        });

        // case_additional_properties
        modelBuilder.Entity<CaseAdditionalPropertyModel>(entity =>
        {
            entity.ToTable("case_additional_properties");
            entity.HasKey(e => e.Id);
            


            entity.Property(e => e.Id)              .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CreatedAt)       .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.CreatedBy)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.LastUpdatedAt)   .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.LastUpdatedBy)   .HasColumnType("char(36)")                                              .HasConversion(GuidToStringConverter)   .IsRequired();

            entity.Property(e => e.CaseId)          .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.Name)            .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.Content)         .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.ContentType)     .HasColumnType("text")                                                                                                          .IsRequired();



            entity.HasOne(e => e.CreatedByUser)     .WithMany()                     .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser) .WithMany()                     .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)              .WithMany(c => c.AdditionalProperties).HasForeignKey(e => e.CaseId);



            entity.HasIndex(e => new {
                e.CaseId,
                e.Name
            }).IsUnique();
        });

        // user_additional_properties
        modelBuilder.Entity<UserAdditionalPropertyModel>(entity =>
        {
            entity.ToTable("user_additional_properties");
            entity.HasKey(e => e.Id);
            


            entity.Property(e => e.Id)              .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CreatedAt)       .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.CreatedBy)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.LastUpdatedAt)   .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.LastUpdatedBy)   .HasColumnType("char(36)")                                              .HasConversion(GuidToStringConverter)                   .IsRequired();
            
            entity.Property(e => e.UserId)          .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.Name)            .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.Content)         .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.ContentType)     .HasColumnType("text")                                                                                                          .IsRequired();



            entity.HasOne(e => e.CreatedByUser)     .WithMany()                     .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser) .WithMany()                     .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.User)              .WithMany(c => c.AdditionalProperties) .HasForeignKey(e => e.UserId);



            entity.HasIndex(e => new {
                e.UserId,
                e.Name
            }).IsUnique();
        });

        // case_messages
        modelBuilder.Entity<CaseMessageModel>(entity =>
        {
            entity.ToTable("case_messages");
            entity.HasKey(e => e.Id);



            entity.Property(e => e.Id)              .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CreatedAt)       .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.CreatedBy)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.LastUpdatedAt)   .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.LastUpdatedBy)   .HasColumnType("char(36)")                                              .HasConversion(GuidToStringConverter)                   .IsRequired();

            entity.Property(e => e.CaseId)          .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.SenderId)        .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.Subject)         .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.Content)         .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.IsUrgent)        .HasColumnType("tinyint(1)")    .HasDefaultValue(false)                                                                         .IsRequired();



            entity.HasOne(e => e.CreatedByUser)     .WithMany()                     .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser) .WithMany()                     .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)              .WithMany(c => c.Messages)      .HasForeignKey(e => e.CaseId);
            entity.HasOne(e => e.Sender)            .WithMany()                     .HasForeignKey(e => e.SenderId);
        });

        // case_messages_read_bys
        modelBuilder.Entity<CaseMessageReadByModel>(entity =>
        {
            entity.ToTable("case_messages_read_bys");
            entity.HasKey(e => e.Id);



            entity.Property(e => e.Id)              .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CreatedAt)       .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.CreatedBy)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();

            entity.Property(e => e.MessageId)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();



            entity.HasOne(e => e.CreatedByUser)     .WithMany()                     .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Message)           .WithMany()                     .HasForeignKey(e => e.MessageId)        .OnDelete(DeleteBehavior.Cascade);
        });

        // case_todos
        modelBuilder.Entity<CaseTodoModel>(entity =>
        {
            entity.ToTable("case_todos");
            entity.HasKey(e => e.Id);
            


            entity.Property(e => e.Id)              .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CreatedAt)       .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.CreatedBy)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.LastUpdatedAt)   .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.LastUpdatedBy)   .HasColumnType("char(36)")                                              .HasConversion(GuidToStringConverter)                   .IsRequired();

            entity.Property(e => e.CaseId)          .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.Summary)         .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.Description)     .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.Status)          .HasColumnType("text")                                                  .HasConversion<string>()                                .IsRequired();
            entity.Property(e => e.Priority)        .HasColumnType("text")                                                  .HasConversion<string>()                                .IsRequired();
            entity.Property(e => e.DueDate)         .HasColumnType("datetime");
            entity.Property(e => e.AssignedTo)      .HasColumnType("char(36)");
            entity.Property(e => e.Reminder)        .HasColumnType("datetime");
            entity.Property(e => e.CompletedAt)     .HasColumnType("datetime");
            entity.Property(e => e.CompletedBy)     .HasColumnType("char(36)");
            entity.Property(e => e.CompletionNote)  .HasColumnType("text");



            entity.HasOne(e => e.CreatedByUser)     .WithMany()                     .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser) .WithMany()                     .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)              .WithMany()                     .HasForeignKey(e => e.CaseId)           .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedToUser)    .WithMany()                     .HasForeignKey(e => e.AssignedTo)       .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CompletedByUser)   .WithMany()                     .HasForeignKey(e => e.CompletedBy)      .OnDelete(DeleteBehavior.Restrict);
        });

        // case_timeline
        modelBuilder.Entity<CaseTimelineItemModel>(entity =>
        {
            entity.ToTable("case_timeline");
            entity.HasKey(e => e.Id);
            


            entity.Property(e => e.Id)              .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CaseId)          .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CreatedAt)       .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.CreatedBy)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.LastUpdatedAt)   .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.LastUpdatedBy)   .HasColumnType("char(36)")                                              .HasConversion(GuidToStringConverter)                   .IsRequired();



            entity.HasOne(e => e.CreatedByUser)     .WithMany()                     .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser) .WithMany()                     .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)              .WithMany(c => c.Timeline)      .HasForeignKey(e => e.CaseId)           .OnDelete(DeleteBehavior.Restrict);
        });

        // case_files
        modelBuilder.Entity<CaseFileModel>(entity =>
        {
            entity.ToTable("case_files");
            entity.HasKey(e => e.Id);
            


            entity.Property(e => e.Id)              .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CreatedAt)       .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.CreatedBy)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.LastUpdatedAt)   .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.LastUpdatedBy)   .HasColumnType("char(36)")                                              .HasConversion(GuidToStringConverter)                   .IsRequired();

            entity.Property(e => e.CaseId)          .HasColumnType("char(36)");
            entity.Property(e => e.Filename)        .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.ContentType)     .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.Size)            .HasColumnType("bigint")                                                                                                        .IsRequired();
            entity.Property(e => e.Hash)            .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.LfsPath)         .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.Description)     .HasColumnType("text")                                                                                                          .IsRequired();



            entity.HasOne(e => e.CreatedByUser)     .WithMany()                     .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser) .WithMany()                     .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)              .WithMany(c => c.Files)         .HasForeignKey(e => e.CaseId)           .OnDelete(DeleteBehavior.Cascade);
        });

        // user_files
        modelBuilder.Entity<UserFileModel>(entity =>
        {
            entity.ToTable("user_files");
            entity.HasKey(e => e.Id);



            entity.Property(e => e.Id)              .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CreatedAt)       .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.CreatedBy)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.LastUpdatedAt)   .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.LastUpdatedBy)   .HasColumnType("char(36)")                                              .HasConversion(GuidToStringConverter)                   .IsRequired();

            entity.Property(e => e.UserId)          .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s));
            entity.Property(e => e.Filename)        .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.ContentType)     .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.Size)            .HasColumnType("bigint")                                                                                                        .IsRequired();
            entity.Property(e => e.Hash)            .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.LfsPath)         .HasColumnType("text")                                                                                                          .IsRequired();
            entity.Property(e => e.Description)     .HasColumnType("text")                                                                                                          .IsRequired();



            entity.HasOne(e => e.CreatedByUser)     .WithMany()                     .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser) .WithMany()                     .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.User)              .WithMany(c => c.Files)         .HasForeignKey(e => e.UserId)           .OnDelete(DeleteBehavior.Cascade);
        });

        // refresh_tokens
        modelBuilder.Entity<RefreshTokenModel>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);
            


            entity.Property(e => e.Id)              .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();
            entity.Property(e => e.CreatedAt)       .HasColumnType("datetime")      .HasDefaultValueSql("UTC_TIMESTAMP()")                                                          .IsRequired();
            entity.Property(e => e.CreatedBy)       .HasColumnType("char(36)")                                              .HasConversion(g => g.ToString(), s => Guid.Parse(s))   .IsRequired();

            entity.Property(e => e.TokenHash)       .HasColumnType("text")                                                                                                          .IsRequired();



            entity.HasOne(e => e.CreatedByUser)     .WithMany()                     .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
