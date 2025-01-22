using DataAccesLayer.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;

namespace DataAccesLayer
{
    public class RubiksCubeDatabaseContext : DbContext
    {
        public DbSet<GameEntity> GameEntities { get; set; }
        public DbSet<CubeStateEntity> CubeStateEntities { get; set; }
        public DbSet<SideStateEntity> SideStateEntities { get; set; }
        public DbSet<SidePointEntity> SidePointEntities { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Configure SQLite as the database provider
            string baseDir = AppContext.BaseDirectory;
            string dbFilePath = Path.Combine(baseDir, "RubiskCube.db");

            // Configure SQLite as the database provider using the relative path
            optionsBuilder.UseSqlite($"Data Source={dbFilePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<CubeStateEntity>()
                .HasOne<GameEntity>()
                .WithMany()
                .HasForeignKey(c => c.GameId);

            modelBuilder.Entity<SideStateEntity>()
                .HasOne<CubeStateEntity>()
                .WithMany()
                .HasForeignKey(s => s.StateId);

            modelBuilder.Entity<SidePointEntity>()
                .HasOne<SideStateEntity>()
                .WithMany()
                .HasForeignKey(p => p.SideId);
        }

        public RubiksCubeDatabaseContext()
        {
            try
            {
                this.Database.EnsureCreated();
            }
            catch { }
            this.Database.OpenConnection();
        }

        public override void Dispose()
        {
            this.SaveChanges();
            this.Database.CloseConnection();
            base.Dispose();
        }
    }
}

