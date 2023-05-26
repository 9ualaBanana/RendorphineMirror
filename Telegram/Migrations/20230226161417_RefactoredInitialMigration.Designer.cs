﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Telegram.Infrastructure.Persistence;

#nullable disable

namespace Telegram.Migrations.TelegramBotUsersDb
{
    [DbContext(typeof(TelegramBotDbContext))]
    [Migration("20230226161417_RefactoredInitialMigration")]
    partial class RefactoredInitialMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.9");

            modelBuilder.Entity("Telegram.Security.Authentication.Persistence.MPlusIdentityEntity", b =>
                {
                    b.Property<string>("UserId")
                        .HasColumnType("TEXT");

                    b.Property<int>("AccessLevel")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SessionId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<long?>("TelegramBotUserChatId")
                        .HasColumnType("INTEGER");

                    b.HasKey("UserId");

                    b.HasIndex("TelegramBotUserChatId")
                        .IsUnique();

                    b.ToTable("MPlusIdentityEntity");
                });

            modelBuilder.Entity("Telegram.Security.Authentication.Persistence.TelegramBotUserEntity", b =>
                {
                    b.Property<long>("ChatId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ChatId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Telegram.Security.Authentication.Persistence.MPlusIdentityEntity", b =>
                {
                    b.HasOne("Telegram.Security.Authentication.Persistence.TelegramBotUserEntity", "TelegramBotUser")
                        .WithOne("MPlusIdentity")
                        .HasForeignKey("Telegram.Security.Authentication.Persistence.MPlusIdentityEntity", "TelegramBotUserChatId");

                    b.Navigation("TelegramBotUser");
                });

            modelBuilder.Entity("Telegram.Security.Authentication.Persistence.TelegramBotUserEntity", b =>
                {
                    b.Navigation("MPlusIdentity");
                });
#pragma warning restore 612, 618
        }
    }
}
