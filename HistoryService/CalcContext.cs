using Domain;
using Microsoft.EntityFrameworkCore;

namespace HistoryService;

public class CalcContext : DbContext
{
    public CalcContext(DbContextOptions<CalcContext> options) : base(options)
    {
        
    }
    protected  override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        //optionsBuilder.UseSqlServer("Server=localhost:1433;Database=Calc;User Id=root;Password=test;");
        optionsBuilder.UseSqlite(
            "Data source=./db.db"
        );
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Operation>()
            .Property(p => p.Id)
            .ValueGeneratedOnAdd();
    }
    
    public DbSet<Operation> OperationTable { get; set; }
}