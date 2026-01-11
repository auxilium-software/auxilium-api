using AuxiliumAPI.Common.DataStructures;
using AuxiliumAPI.Common.EntityModels;
using AuxiliumAPI.Models.Case;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Reflection.Emit;

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





    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // users
        modelBuilder.Entity<UserModel>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);



            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)");
            entity.Property(e => e.LastUpdatedAt)                   .HasColumnName("last_updated_at")                           .HasColumnType("datetime");
            entity.Property(e => e.LastUpdatedBy)                   .HasColumnName("last_updated_by")                           .HasColumnType("char(36)");

            entity.Property(e => e.EmailAddress)                    .HasColumnName("email_address")                             .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.PasswordHash)                    .HasColumnName("password_hash")                             .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.FullName)                        .HasColumnName("full_name")                                 .HasColumnType("text")                                                                                                              .IsRequired();

            entity.Property(e => e.FullAddress)                     .HasColumnName("full_address")                              .HasColumnType("text");
            entity.Property(e => e.TelephoneNumber)                 .HasColumnName("telephone_number")                          .HasColumnType("text");
            entity.Property(e => e.Gender)                          .HasColumnName("gender")                                    .HasColumnType("text");
            entity.Property(e => e.DateOfBirth)                     .HasColumnName("date_of_birth")                             .HasColumnType("date");
            entity.Property(e => e.HowDidYouFindOutAboutOurService) .HasColumnName("how_did_you_find_out_about_our_service")    .HasColumnType("text");

            entity.Property(e => e.AllowLogin)                      .HasColumnName("allow_login")                               .HasColumnType("tinyint(1)")                                                    .HasDefaultValue(true)                              .IsRequired();
            entity.Property(e => e.IsAdmin)                         .HasColumnName("is_admin")                                  .HasColumnType("tinyint(1)")                                                    .HasDefaultValue(false)                             .IsRequired();
            entity.Property(e => e.IsCaseWorker)                    .HasColumnName("is_case_worker")                            .HasColumnType("tinyint(1)")                                                    .HasDefaultValue(false)                             .IsRequired();



            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser)                 .WithMany()                                                 .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);



            entity.HasIndex(e => e.EmailAddress).IsUnique();
        });

        // wemwbs
        modelBuilder.Entity<WEMWBSModel>(entity =>
        {
            entity.ToTable("wemwbs");
            entity.HasKey(e => e.Id);



            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)");
            
            entity.Property(e => e.OptimismScore)                   .HasColumnName("optimism_score")                            .HasColumnType("int")                                                                                                               .IsRequired();
            entity.Property(e => e.UsefulnessScore)                 .HasColumnName("usefulness_score")                          .HasColumnType("int")                                                                                                               .IsRequired();
            entity.Property(e => e.RelaxedScore)                    .HasColumnName("relaxed_score")                             .HasColumnType("int")                                                                                                               .IsRequired();
            entity.Property(e => e.InterestedInPeopleScore)         .HasColumnName("interested_in_people_score")                .HasColumnType("int")                                                                                                               .IsRequired();
            entity.Property(e => e.SpareEnergyScore)                .HasColumnName("spare_energy_score")                        .HasColumnType("int")                                                                                                               .IsRequired();
            entity.Property(e => e.ProblemHandlingScore)            .HasColumnName("problem_handling_score")                    .HasColumnType("int")                                                                                                               .IsRequired();
            entity.Property(e => e.ClearThoughtScore)               .HasColumnName("clear_thought_score")                       .HasColumnType("int")                                                                                                               .IsRequired();
            entity.Property(e => e.FeelingGoodSelfScore)            .HasColumnName("feeling_good_self_score")                   .HasColumnType("int")                                                                                                               .IsRequired();
            entity.Property(e => e.FeelingCloseToPeopleScore)       .HasColumnName("feeling_close_to_people_score")             .HasColumnType("int")                                                                                                               .IsRequired();
            entity.Property(e => e.ConfidenceScore)                 .HasColumnName("confidence_score")                          .HasColumnType("int")                                                                                                               .IsRequired();
            entity.Property(e => e.MakingUpOwnMindScore)            .HasColumnName("making_up_own_mind_score")                  .HasColumnType("int")                                                                                                               .IsRequired();
            entity.Property(e => e.FeelingLovedScore)               .HasColumnName("feeling_loved_score")                       .HasColumnType("int")                                                                                                               .IsRequired();
            entity.Property(e => e.InterestedInNewThingsScore)      .HasColumnName("interested_in_new_things_score")            .HasColumnType("int")                                                                                                               .IsRequired();
            entity.Property(e => e.FeelingCheerfulScore)            .HasColumnName("feeling_cheerful_score")                    .HasColumnType("int")                                                                                                               .IsRequired();
            
            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Cascade);
        });

        // cases
        modelBuilder.Entity<CaseModel>(entity =>
        {
            entity.ToTable("cases");
            entity.HasKey(e => e.Id);


            
            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)");
            entity.Property(e => e.LastUpdatedAt)                   .HasColumnName("last_updated_at")                           .HasColumnType("datetime");
            entity.Property(e => e.LastUpdatedBy)                   .HasColumnName("last_updated_by")                           .HasColumnType("char(36)");

            entity.Property(e => e.Title)                           .HasColumnName("title")                                     .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.Description)                     .HasColumnName("description")                               .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.Status)                          .HasColumnName("status")                                    .HasColumnType("text")                  .HasConversion<string>()                .HasDefaultValue(CaseStatusEnum.Open)               .IsRequired();
            entity.Property(e => e.Sensitivity)                     .HasColumnName("sensitivity")                               .HasColumnType("text")                  .HasConversion<string>()                .HasDefaultValue(CaseSensitivityEnum.Confidential)  .IsRequired();



            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser)                 .WithMany()                                                 .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(e => e.Workers)                          .WithOne(w => w.Case)                                       .HasForeignKey(w => w.CaseId)           .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Clients)                          .WithOne(c => c.Case)                                       .HasForeignKey(c => c.CaseId)           .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.AdditionalProperties)             .WithOne(p => p.Case)                                       .HasForeignKey(p => p.CaseId)           .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Messages)                         .WithOne(m => m.Case)                                       .HasForeignKey(m => m.CaseId)           .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Files)                            .WithOne(f => f.Case)                                       .HasForeignKey(f => f.CaseId)           .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Todos)                            .WithOne(t => t.Case)                                       .HasForeignKey(t => t.CaseId)           .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Timeline)                         .WithOne(t => t.Case)                                       .HasForeignKey(t => t.CaseId)           .OnDelete(DeleteBehavior.Cascade);
        });

        // case_workers
        modelBuilder.Entity<CaseWorkerModel>(entity =>
        {
            entity.ToTable("case_workers");
            entity.HasKey(e => e.Id);


            
            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)");

            entity.Property(e => e.CaseId)                          .HasColumnName("case_id")                                   .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.UserId)                          .HasColumnName("user_id")                                   .HasColumnType("char(36)")                                                                                                          .IsRequired();



            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)                              .WithMany(c => c.Workers)                                   .HasForeignKey(e => e.CaseId);
            entity.HasOne(e => e.User)                              .WithMany(u => u.WorkerOnCases)                             .HasForeignKey(e => e.UserId);
        });

        // case_clients
        modelBuilder.Entity<CaseClientModel>(entity =>
        {
            entity.ToTable("case_clients");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)");

            entity.Property(e => e.CaseId)                          .HasColumnName("case_id")                                   .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.UserId)                          .HasColumnName("user_id")                                   .HasColumnType("char(36)")                                                                                                          .IsRequired();

            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)                              .WithMany(c => c.Clients)                                   .HasForeignKey(e => e.CaseId);
            entity.HasOne(e => e.User)                              .WithMany(u => u.ClientOnCases)                             .HasForeignKey(e => e.UserId);
        });

        // case_additional_properties
        modelBuilder.Entity<CaseAdditionalPropertyModel>(entity =>
        {
            entity.ToTable("case_additional_properties");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)");

            entity.Property(e => e.CaseId)                          .HasColumnName("case_id")                                   .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.Name)                            .HasColumnName("name")                                      .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.Content)                         .HasColumnName("content")                                   .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.ContentType)                     .HasColumnName("content_type")                              .HasColumnType("text")                                                                                                              .IsRequired();

            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser)                 .WithMany()                                                 .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)                              .WithMany(c => c.AdditionalProperties)                      .HasForeignKey(e => e.CaseId);

            entity.HasIndex(e => new { e.CaseId, e.Name }).IsUnique();
        });

        // user_additional_properties
        modelBuilder.Entity<UserAdditionalPropertyModel>(entity =>
        {
            entity.ToTable("user_additional_properties");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)");

            entity.Property(e => e.UserId)                          .HasColumnName("user_id")                                   .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.Name)                            .HasColumnName("name")                                      .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.Content)                         .HasColumnName("content")                                   .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.ContentType)                     .HasColumnName("content_type")                              .HasColumnType("text")                                                                                                              .IsRequired();

            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser)                 .WithMany()                                                 .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.User)                              .WithMany(c => c.AdditionalProperties)                      .HasForeignKey(e => e.UserId);

            entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
        });

        // case_messages
        modelBuilder.Entity<CaseMessageModel>(entity =>
        {
            entity.ToTable("case_messages");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)");
            entity.Property(e => e.LastUpdatedAt)                   .HasColumnName("last_updated_at")                           .HasColumnType("datetime");
            entity.Property(e => e.LastUpdatedBy)                   .HasColumnName("last_updated_by")                           .HasColumnType("char(36)");

            entity.Property(e => e.CaseId)                          .HasColumnName("case_id")                                   .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.SenderId)                        .HasColumnName("sender_id")                                 .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.Subject)                         .HasColumnName("subject")                                   .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.Content)                         .HasColumnName("content")                                   .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.IsUrgent)                        .HasColumnName("is_urgent")                                 .HasColumnType("tinyint(1)")                                                    .HasDefaultValue(false)                             .IsRequired();

            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser)                 .WithMany()                                                 .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)                              .WithMany(c => c.Messages)                                  .HasForeignKey(e => e.CaseId);
            entity.HasOne(e => e.Sender)                            .WithMany()                                                 .HasForeignKey(e => e.SenderId);
        });

        // case_messages_read_bys
        modelBuilder.Entity<CaseMessageReadByModel>(entity =>
        {
            entity.ToTable("case_messages_read_bys");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)")                                                                                                          .IsRequired();

            entity.Property(e => e.MessageId)                       .HasColumnName("message_id")                                .HasColumnType("char(36)")                                                                                                          .IsRequired();

            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Message)                           .WithMany()                                                 .HasForeignKey(e => e.MessageId)        .OnDelete(DeleteBehavior.Cascade);
        });

        // case_todos
        modelBuilder.Entity<CaseTodoModel>(entity =>
        {
            entity.ToTable("case_todos");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)");
            entity.Property(e => e.LastUpdatedAt)                   .HasColumnName("last_updated_at")                           .HasColumnType("datetime");
            entity.Property(e => e.LastUpdatedBy)                   .HasColumnName("last_updated_by")                           .HasColumnType("char(36)");

            entity.Property(e => e.CaseId)                          .HasColumnName("case_id")                                   .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.Summary)                         .HasColumnName("summary")                                   .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.Description)                     .HasColumnName("description")                               .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.Status)                          .HasColumnName("status")                                    .HasColumnType("text")                  .HasConversion<string>()                                                                    .IsRequired();
            entity.Property(e => e.Priority)                        .HasColumnName("priority")                                  .HasColumnType("text")                  .HasConversion<string>()                                                                    .IsRequired();
            entity.Property(e => e.DueDate)                         .HasColumnName("due_date")                                  .HasColumnType("datetime");
            entity.Property(e => e.AssignedTo)                      .HasColumnName("assigned_to")                               .HasColumnType("char(36)");
            entity.Property(e => e.Reminder)                        .HasColumnName("reminder")                                  .HasColumnType("datetime");
            entity.Property(e => e.CompletedAt)                     .HasColumnName("completed_at")                              .HasColumnType("datetime");
            entity.Property(e => e.CompletedBy)                     .HasColumnName("completed_by")                              .HasColumnType("char(36)");
            entity.Property(e => e.CompletionNote)                  .HasColumnName("completion_note")                           .HasColumnType("text");

            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser)                 .WithMany()                                                 .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)                              .WithMany(c => c.Todos)                                     .HasForeignKey(e => e.CaseId)           .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedToUser)                    .WithMany()                                                 .HasForeignKey(e => e.AssignedTo)       .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CompletedByUser)                   .WithMany()                                                 .HasForeignKey(e => e.CompletedBy)      .OnDelete(DeleteBehavior.Restrict);
        });

        // case_timeline
        modelBuilder.Entity<CaseTimelineItemModel>(entity =>
        {
            entity.ToTable("case_timeline");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)");
            entity.Property(e => e.LastUpdatedAt)                   .HasColumnName("last_updated_at")                           .HasColumnType("datetime");
            entity.Property(e => e.LastUpdatedBy)                   .HasColumnName("last_updated_by")                           .HasColumnType("char(36)");

            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser)                 .WithMany()                                                 .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)                              .WithMany(c => c.Timeline)                                  .HasForeignKey(e => e.CaseId)           .OnDelete(DeleteBehavior.Restrict);
        });

        // case_files
        modelBuilder.Entity<CaseFileModel>(entity =>
        {
            entity.ToTable("case_files");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)");
            entity.Property(e => e.LastUpdatedAt)                   .HasColumnName("last_updated_at")                           .HasColumnType("datetime");
            entity.Property(e => e.LastUpdatedBy)                   .HasColumnName("last_updated_by")                           .HasColumnType("char(36)");

            entity.Property(e => e.CaseId)                          .HasColumnName("case_id")                                   .HasColumnType("char(36)");
            entity.Property(e => e.Filename)                        .HasColumnName("filename")                                  .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.ContentType)                     .HasColumnName("content_type")                              .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.Size)                            .HasColumnName("size")                                      .HasColumnType("bigint")                                                                                                            .IsRequired();
            entity.Property(e => e.Hash)                            .HasColumnName("hash")                                      .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.LfsPath)                         .HasColumnName("lfs_path")                                  .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.Description)                     .HasColumnName("description")                               .HasColumnType("text")                                                                                                              .IsRequired();

            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser)                 .WithMany()                                                 .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Case)                              .WithMany(c => c.Files)                                     .HasForeignKey(e => e.CaseId)           .OnDelete(DeleteBehavior.Cascade);
        });

        // user_files
        modelBuilder.Entity<UserFileModel>(entity =>
        {
            entity.ToTable("user_files");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)");
            entity.Property(e => e.LastUpdatedAt)                   .HasColumnName("last_updated_at")                           .HasColumnType("datetime");
            entity.Property(e => e.LastUpdatedBy)                   .HasColumnName("last_updated_by")                           .HasColumnType("char(36)");

            entity.Property(e => e.UserId)                          .HasColumnName("user_id")                                   .HasColumnType("char(36)");
            entity.Property(e => e.Filename)                        .HasColumnName("filename")                                  .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.ContentType)                     .HasColumnName("content_type")                              .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.Size)                            .HasColumnName("size")                                      .HasColumnType("bigint")                                                                                                            .IsRequired();
            entity.Property(e => e.Hash)                            .HasColumnName("hash")                                      .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.LfsPath)                         .HasColumnName("lfs_path")                                  .HasColumnType("text")                                                                                                              .IsRequired();
            entity.Property(e => e.Description)                     .HasColumnName("description")                               .HasColumnType("text")                                                                                                              .IsRequired();

            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.LastUpdatedByUser)                 .WithMany()                                                 .HasForeignKey(e => e.LastUpdatedBy)    .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.User)                              .WithMany(c => c.Files)                                     .HasForeignKey(e => e.UserId)           .OnDelete(DeleteBehavior.Cascade);
        });

        // refresh_tokens
        modelBuilder.Entity<RefreshTokenModel>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id)                              .HasColumnName("id")                                        .HasColumnType("char(36)")                                                                                                          .IsRequired();
            entity.Property(e => e.CreatedAt)                       .HasColumnName("created_at")                                .HasColumnType("datetime")                                                      .HasDefaultValueSql("UTC_TIMESTAMP()")              .IsRequired();
            entity.Property(e => e.CreatedBy)                       .HasColumnName("created_by")                                .HasColumnType("char(36)");

            entity.Property(e => e.TokenHash)                       .HasColumnName("token_hash")                                .HasColumnType("text")                                                                                                              .IsRequired();

            entity.HasOne(e => e.CreatedByUser)                     .WithMany()                                                 .HasForeignKey(e => e.CreatedBy)        .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
