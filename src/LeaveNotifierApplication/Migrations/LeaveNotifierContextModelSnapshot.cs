﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using LeaveNotifierApplication.Models;

namespace LeaveNotifierApplication.Migrations
{
    [DbContext(typeof(LeaveNotifierContext))]
    partial class LeaveNotifierContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.1")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("LeaveNotifierApplication.Models.Leave", b =>
                {
                    b.Property<int>("id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("dateCreated");

                    b.Property<DateTime>("from");

                    b.Property<string>("justification");

                    b.Property<int>("means");

                    b.Property<int>("status");

                    b.Property<DateTime>("to");

                    b.Property<string>("user");

                    b.HasKey("id");

                    b.ToTable("Leaves");
                });
        }
    }
}
