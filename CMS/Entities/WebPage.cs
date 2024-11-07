using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace CMS.Entities
{
    public class WebPage
    {
        public int WebPageId { get; set; }
        public int WebSiteId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; }
        public string BackgroundColor { get; set; } = "white";
        public string Description { get; set; } = string.Empty;
        public DateOnly CreationDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public DateOnly? LastUpdated { get; set; }
        public string VisitorUrl { get; set; } = string.Empty;
        public WebSite WebSite { get; set; }
        public ICollection<Content> Contents { get; set; }

        public WebPageLayout? WebPageLayout { get; set; } = null;

    }
}
