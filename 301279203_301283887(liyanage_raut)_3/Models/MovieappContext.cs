using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using _301279203_301283887_liyanage_raut__3.Models;

namespace _301279203_301283887_liyanage_raut__3.Models;

public partial class MovieappContext : DbContext
{
    public MovieappContext()
    {
    }

    public MovieappContext(DbContextOptions<MovieappContext> options)
        : base(options)
    {
    }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C016A8EDC");

            entity.HasIndex(e => e.Username, "UQ__Users__536C85E4E679887A").IsUnique();

            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.UserPassword).HasMaxLength(255);
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

public DbSet<_301279203_301283887_liyanage_raut__3.Models.Movie> Movie { get; set; } = default!;
}
