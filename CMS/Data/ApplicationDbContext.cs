using CMS.Entities;
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
        public DbSet<WebSite> Websites { get; set; }
        //public DbSet<ContentType> ContentTypes { get; set; } Denna skall inte anv�ndas l�ngre, den klassen �r nu abstract class
        public DbSet<StringData> StringData { get; set; }
        public DbSet<ArrayData> ArrayData { get; set; }
        public DbSet<ImageData> ImageData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<WebSite>(e =>
            {
                e.HasKey(e => e.WebSiteId);
                e.HasOne(e => e.ApplicationUser)
                  .WithMany(u => u.WebSites)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WebPage>(p =>
            {
                p.HasKey(p => p.WebPageId);
                p.HasOne(p => p.WebSites)
                  .WithMany(w => w.WebPages)
                  .HasForeignKey(p => p.WebSiteId)
                  .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<Content>(c =>
            {
                c.HasKey(c => c.ContentId);
                c.HasOne(c => c.WebPages)
                    .WithMany(w => w.Contents)
                    .HasForeignKey(c => c.WebPageId)
                    .OnDelete(DeleteBehavior.Cascade);

                c.HasMany(c => c.ContentTypes)
                    .WithOne(ct => ct.Content)
                    .HasForeignKey(ct => ct.ContentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // TPH arv h�r
            modelBuilder.Entity<ContentType>()
                .HasDiscriminator<string>("ContentTypeDiscriminator") // Discriminator �r f�ltet som h�ller reda p� vilken utav subklasserna som Content har (string, array, image)
                .HasValue<StringData>("StringData")
                .HasValue<ArrayData>("ArrayData")
                .HasValue<ImageData>("ImageData");
        }
    }
}
