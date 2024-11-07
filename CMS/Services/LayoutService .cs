using CMS.Data;
using CMS.Entities;
using Microsoft.EntityFrameworkCore;

namespace CMS.Services
{
    public class LayoutService : ILayoutService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly IGetCurrentUserService _currentUserService;

        public LayoutService(IDbContextFactory<ApplicationDbContext> dbContextFactory, IGetCurrentUserService currentUserService)
        {
            _dbContextFactory = dbContextFactory;
            _currentUserService = currentUserService;
        }

        public async Task<WebPageLayout?> GetLayoutAsync(int webPageId)
        {
            await using var context = _dbContextFactory.CreateDbContext();
            return await context.WebPageLayouts.FirstOrDefaultAsync(l => l.WebPageIdForLayout == webPageId);
        }


        // Method to save new layout to the database
        public async Task SaveLayoutAsync(WebPageLayout layout)
        {
            await using var context = _dbContextFactory.CreateDbContext();

            // Ensure WebPageId exists in the WebPages table
            var webPageExists = await context.WebPages.AnyAsync(wp => wp.WebPageId == layout.WebPageIdForLayout);
            if (!webPageExists)
            {
                throw new InvalidOperationException($"WebPageId {layout.WebPageIdForLayout} does not exist.");
            }

            context.WebPageLayouts.Add(layout);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error: {ex.InnerException?.Message}");
                throw;
            }
        }
    

    // Method to update existing content in the database
    public async Task UpdateLayoutAsync(WebPageLayout layout)
        {
            await using var context = _dbContextFactory.CreateDbContext();

            var existingLayout = await context.WebPageLayouts.FirstOrDefaultAsync(l => l.Id == layout.Id);
            if (existingLayout == null)
            {
                throw new InvalidOperationException($"layout with ID {layout.Id} does not exist.");
            }

            existingLayout.LayoutCellsSerialized = layout.LayoutCellsSerialized;
            existingLayout.LastUpdated = DateOnly.FromDateTime(DateTime.Now);
            existingLayout.UserId = layout.UserId;
            existingLayout.Id = layout.Id;
            existingLayout.LayoutCells = layout.LayoutCells;
            context.WebPageLayouts.Update(existingLayout);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"Error: {ex.InnerException?.Message}");
                throw;
            }
        }
    }

}
