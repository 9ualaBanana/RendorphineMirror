﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Telegram.Persistence;

#nullable disable

namespace Telegram.Migrations.TelegramBotUsersDb
{
    [DbContext(typeof(TelegramBotDbContext))]
    partial class TelegramBotUsersDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.9");

            modelBuilder.Entity("Telegram.Persistence.MPlusIdentityEntity", b =>
                {
                    b.Property<long>("TelegramBotUserChatId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("AccessLevel")
                        .HasColumnType("INTEGER");

                    b.Property<string>("SessionId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("UserId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("TelegramBotUserChatId");

                    b.ToTable("MPlusIdentityEntity");
                });

            modelBuilder.Entity("Telegram.Persistence.TelegramBotUserEntity", b =>
                {
                    b.Property<long>("ChatId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ChatId");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Telegram.Persistence.MPlusIdentityEntity", b =>
                {
                    b.HasOne("Telegram.Persistence.TelegramBotUserEntity", "TelegramBotUser")
                        .WithOne("MPlusIdentity")
                        .HasForeignKey("Telegram.Persistence.MPlusIdentityEntity", "TelegramBotUserChatId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("TelegramBotUser");
                });

            modelBuilder.Entity("Telegram.Persistence.TelegramBotUserEntity", b =>
                {
                    b.Navigation("MPlusIdentity");
                });
#pragma warning restore 612, 618
        }
    }
}
