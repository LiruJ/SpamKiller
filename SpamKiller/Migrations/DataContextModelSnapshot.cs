﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SpamKiller.Data;

namespace SpamKiller.Migrations
{
    [DbContext(typeof(DataContext))]
    partial class DataContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.15");

            modelBuilder.Entity("SpamKiller.Data.ScamReporter", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("BanCount")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("LastBanTime")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("LastUnbanTime")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("ServerId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("ScamReporters");
                });

            modelBuilder.Entity("SpamKiller.Data.UserBan", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("BanDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("BanReason")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("BannedUserId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ReporterId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ReporterId");

                    b.ToTable("BannedUsers");
                });

            modelBuilder.Entity("SpamKiller.Data.UserPreviousBan", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("BanDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("BanReason")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("BannedUserId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("BanningReporterId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("UnbanDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("UnbanReason")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("UnbanningReporterId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("BanningReporterId");

                    b.HasIndex("UnbanningReporterId");

                    b.ToTable("PreviousBannedUsers");
                });

            modelBuilder.Entity("SpamKiller.Settings.ServerSettings", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("IsWhitelisted")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("LogChannelId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("ReporterRoleId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ServerId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("ServerSettings");
                });

            modelBuilder.Entity("SpamKiller.Data.UserBan", b =>
                {
                    b.HasOne("SpamKiller.Data.ScamReporter", "Reporter")
                        .WithMany()
                        .HasForeignKey("ReporterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Reporter");
                });

            modelBuilder.Entity("SpamKiller.Data.UserPreviousBan", b =>
                {
                    b.HasOne("SpamKiller.Data.ScamReporter", "BanningReporter")
                        .WithMany()
                        .HasForeignKey("BanningReporterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("SpamKiller.Data.ScamReporter", "UnbanningReporter")
                        .WithMany()
                        .HasForeignKey("UnbanningReporterId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BanningReporter");

                    b.Navigation("UnbanningReporter");
                });
#pragma warning restore 612, 618
        }
    }
}
