﻿// <auto-generated />
using System;
using CrudUsingAjax.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CrudUsingAjax.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20240703072756_AddViewModel")]
    partial class AddViewModel
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.6")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("CrudUsingAjax.Models.Department", b =>
                {
                    b.Property<int>("Department_Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Department_Id"));

                    b.Property<int?>("DepartmentName")
                        .HasColumnType("int");

                    b.HasKey("Department_Id");

                    b.ToTable("Department");
                });

            modelBuilder.Entity("CrudUsingAjax.Models.Employee", b =>
                {
                    b.Property<int>("Employee_Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Employee_Id"));

                    b.Property<string>("Address")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Department_Id")
                        .HasColumnType("int");

                    b.Property<int?>("Department_Id1")
                        .HasColumnType("int");

                    b.Property<string>("Email")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("First_Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Gender")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateOnly?>("Joining_Date")
                        .HasColumnType("date");

                    b.Property<string>("Last_Name")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Phone_Number")
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("Profile_Image")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Employee_Id");

                    b.HasIndex("Department_Id1");

                    b.ToTable("Employee");
                });

            modelBuilder.Entity("CrudUsingAjax.Models.ViewEmployeeModel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("Department_Id")
                        .HasColumnType("int");

                    b.Property<int?>("Employee_Id")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("Department_Id");

                    b.HasIndex("Employee_Id");

                    b.ToTable("ViewEmployeeModel");
                });

            modelBuilder.Entity("CrudUsingAjax.Models.Employee", b =>
                {
                    b.HasOne("CrudUsingAjax.Models.Department", "Department")
                        .WithMany("Employees")
                        .HasForeignKey("Department_Id1");

                    b.Navigation("Department");
                });

            modelBuilder.Entity("CrudUsingAjax.Models.ViewEmployeeModel", b =>
                {
                    b.HasOne("CrudUsingAjax.Models.Department", "Department")
                        .WithMany()
                        .HasForeignKey("Department_Id");

                    b.HasOne("CrudUsingAjax.Models.Employee", "Employee")
                        .WithMany()
                        .HasForeignKey("Employee_Id");

                    b.Navigation("Department");

                    b.Navigation("Employee");
                });

            modelBuilder.Entity("CrudUsingAjax.Models.Department", b =>
                {
                    b.Navigation("Employees");
                });
#pragma warning restore 612, 618
        }
    }
}
