﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TrialUsersMediator.Persistence;

#nullable disable

namespace TrialUsersMediator.Migrations
{
    [DbContext(typeof(TrialUsersDbContext))]
    [Migration("20230614174732_InitialMigration")]
    partial class InitialMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.5");

            modelBuilder.Entity("TrialUsersMediator.Persistence.TrialUser+Entity", b =>
                {
                    b.Property<long>("Identifier")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Platform")
                        .HasColumnType("INTEGER");

                    b.HasKey("Identifier", "Platform");

                    b.ToTable("AuthenticatedUsers");
                });

            modelBuilder.Entity("TrialUsersMediator.Persistence.TrialUser+Quota<NodeCommon.Tasks.TaskAction>+Entity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<long>("EntityIdentifier")
                        .HasColumnType("INTEGER");

                    b.Property<int>("EntityPlatform")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("EntityIdentifier", "EntityPlatform")
                        .IsUnique();

                    b.ToTable("Quotas", (string)null);
                });

            modelBuilder.Entity("TrialUsersMediator.Persistence.TrialUser+Quota<NodeCommon.Tasks.TaskAction>+Entity", b =>
                {
                    b.HasOne("TrialUsersMediator.Persistence.TrialUser+Entity", null)
                        .WithOne("Quota_")
                        .HasForeignKey("TrialUsersMediator.Persistence.TrialUser+Quota<NodeCommon.Tasks.TaskAction>+Entity", "EntityIdentifier", "EntityPlatform")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsMany("TrialUsersMediator.Persistence.TrialUser+Quota<NodeCommon.Tasks.TaskAction>+Entity+Entry", "Entries", b1 =>
                        {
                            b1.Property<int>("Id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("INTEGER");

                            b1.Property<int>("QuotaId")
                                .HasColumnType("INTEGER");

                            b1.Property<int>("Type")
                                .HasColumnType("INTEGER");

                            b1.Property<int>("Value")
                                .HasColumnType("INTEGER");

                            b1.HasKey("Id");

                            b1.HasIndex("QuotaId");

                            b1.ToTable("QuotaEntries", (string)null);

                            b1.WithOwner("Quota")
                                .HasForeignKey("QuotaId");

                            b1.Navigation("Quota");
                        });

                    b.Navigation("Entries");
                });

            modelBuilder.Entity("TrialUsersMediator.Persistence.TrialUser+Entity", b =>
                {
                    b.Navigation("Quota_")
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
