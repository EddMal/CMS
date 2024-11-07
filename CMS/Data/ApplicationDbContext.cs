using CMS.Entities;
using CMS.Models;
using System.Net.Mime;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace CMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Content> Contents { get; set; }
        public DbSet<WebPage> WebPages { get; set; }
        public DbSet<WebSite> WebSites { get; set; }
        public DbSet<WebSiteVisit> WebSiteVisits { get; set; }
        public DbSet<Template> Templates { get; set; }  // Map to the Templates table

        public DbSet<WebPageLayout> WebPageLayouts { get; set; }

        public DbSet<Profile> Profiles {get; set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<WebSite>(e =>
            {
                e.ToTable("WebSites");
                e.HasKey(e => e.WebSiteId);
                e.HasOne(e => e.ApplicationUser)
                  .WithMany(u => u.WebSites)
                  .HasForeignKey(e => e.UserId);
            });

            modelBuilder.Entity<WebPage>(p =>
            {
                p.ToTable("WebPages");
                p.HasKey(p => p.WebPageId);
                p.HasOne(p => p.WebSite)
                  .WithMany(w => w.WebPages)
                  .HasForeignKey(p => p.WebSiteId);

                // Reverse navigation to WebPageLayout
                p.HasOne(p => p.WebPageLayout)  // One-to-one with WebPageLayout
                    .WithOne(l => l.WebPage) // One-to-one with WebPage
                    .HasForeignKey<WebPageLayout>(l => l.WebPageIdForLayout);  // Foreign key property
            });

            modelBuilder.Entity<Content>(c =>
            {
                c.ToTable("Contents");
                c.HasKey(c => c.ContentId);
                c.HasOne(c => c.WebPages)
                    .WithMany(w => w.Contents)
                    .HasForeignKey(c => c.WebPageId);
                c.HasOne(c => c.Template)
                 .WithMany()  // No reverse navigation
                 .HasForeignKey(c => c.TemplateId);
            });

            modelBuilder.Entity<Profile>(p =>
            {
                p.ToTable("Profiles");
                p.HasKey(p => p.id);
                p.HasOne(c => c.User)
                    .WithOne(u => u.Profile)
                    .HasForeignKey<Profile>(c => c.UserId);
            });

            modelBuilder.Entity<WebPageLayout>(e =>
            {
                e.ToTable("WebPageLayouts");
                e.HasKey(e => e.Id);

                // Define foreign key relationship with WebPage
                e.HasOne(wpl => wpl.WebPage)
                    .WithOne() // WebPage has exactly one WebPageLayout
                    .HasForeignKey<WebPageLayout>(wpl => wpl.WebPageIdForLayout);  // Define WebPageId as the foreign key

                // Configure the serialized LayoutCells property
                e.Property(w => w.LayoutCellsSerialized)
                    .HasColumnType("nvarchar(max)"); // Using "nvarchar(max)" to store the serialized data as a string (JSON)

                // Optional: You could store the LayoutCells as JSON (if you're using a database that supports JSON columns, like PostgreSQL)
                // For example, in PostgreSQL you could do something like this:
                // e.Property(w => w.LayoutCellsSerialized).HasColumnType("jsonb"); // for Postgres
            });
        }


    }
}
