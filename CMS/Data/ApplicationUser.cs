using CMS.Entities;
using Microsoft.AspNetCore.Identity;

namespace CMS.Data
{
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        public ICollection<WebSite> WebSites { get; set; }
        public Profile Profile {get; set;}
    }

}
