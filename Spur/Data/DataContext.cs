using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Spur.Model;

namespace Spur.Data;

public class DataContext : DbContext, IDataContext
{
    public DbSet<Activity> Activities { get; set; }
    public DbSet<Athlete> Athletes { get; set; }

    public string DbPath { get; }

    public DataContext()
    {
        // var folder = Environment.SpecialFolder.LocalApplicationData;
        // var path = Environment.GetFolderPath(folder);
        var path = Environment.CurrentDirectory;
        DbPath = Path.Join(path, "spur.db");
    }

    // The following configures EF to create a Sqlite database file in the
    // special "local" folder for your platform.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Activity>()
            .HasOne(a => a.Athlete)
            .WithMany(a => a.Activities);

        modelBuilder.Entity<Athlete>()
            .HasMany(a => a.Challenges)
            .WithMany(c => c.Athletes);
    }

    public Task Init()
    {
        //Database.EnsureCreated();
        return Database.MigrateAsync();
    }
}
