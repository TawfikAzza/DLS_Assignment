﻿using Domain;
using Microsoft.EntityFrameworkCore;

namespace HistoryService;

public class CalcContext : DbContext
{
    protected  override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=localhost:1433;Database=Calc;User Id=root;Password=test;");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Operation>()
            .Property(p => p.Id)
            .ValueGeneratedOnAdd();
    }
}