using CMS.Entities;

namespace CMS.Services
{
    public interface ILayoutService
    {
        Task<WebPageLayout?> GetLayoutAsync(int webPageId);
        Task SaveLayoutAsync(WebPageLayout layout);
        Task UpdateLayoutAsync(WebPageLayout layout);

    }
}