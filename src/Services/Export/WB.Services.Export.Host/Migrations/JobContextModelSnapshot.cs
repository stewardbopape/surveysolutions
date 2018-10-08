﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using WB.Services.Export.Host.Scheduler.PostgresWorkQueue;

namespace WB.Services.Export.Host.Migrations
{
    [DbContext(typeof(JobContext))]
    partial class JobContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("scheduler")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn)
                .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("WB.Services.Export.Host.Scheduler.PostgresWorkQueue.Model.JobItem", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id");

                    b.Property<string>("Args")
                        .IsRequired()
                        .HasColumnName("args");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("(now() at time zone 'utc')");

                    b.Property<DateTime?>("EndAt")
                        .HasColumnName("end_at");

                    b.Property<string>("ErrorMessage")
                        .HasColumnName("error_message");

                    b.Property<string>("ExportState")
                        .IsRequired()
                        .HasColumnName("export_state");

                    b.Property<DateTime>("LastUpdateAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("last_update_at")
                        .HasDefaultValueSql("(now() at time zone 'utc')");

                    b.Property<int>("Progress")
                        .HasColumnName("progress");

                    b.Property<DateTime?>("ScheduleAt")
                        .HasColumnName("schedule_at");

                    b.Property<DateTime?>("StartAt")
                        .HasColumnName("start_at");

                    b.Property<string>("Status")
                        .IsRequired()
                        .HasColumnName("status");

                    b.Property<string>("Tag")
                        .HasColumnName("tag");

                    b.Property<string>("Tenant")
                        .IsRequired()
                        .HasColumnName("tenant");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnName("type");

                    b.HasKey("Id")
                        .HasName("pk_jobs");

                    b.HasIndex("Tenant", "Status");

                    b.ToTable("jobs");
                });
#pragma warning restore 612, 618
        }
    }
}
