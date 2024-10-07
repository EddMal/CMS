﻿using CMS.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;

namespace CMS.Components.Pages.WebPages
{
    public partial class EditWebpage
    {
        [Parameter] public int WebPageId { get; set; }

        private List<Content> contentList;
        private int VisitCount { get; set; }
        private string CurrentPageUrl => NavigationManager.Uri;
        private int WebSiteId { get; set; }

        protected override async Task OnInitializedAsync()
        {
            using var dbContext = DbContextFactory.CreateDbContext();

            contentList = await dbContext.Contents
                                         .Where(c => c.WebPageId == WebPageId)
                                         .ToListAsync();

            // Get the WebSiteId associated with this WebPage
            var webPage = await dbContext.WebPages.FirstOrDefaultAsync(wp => wp.WebPageId == WebPageId);
            if (webPage != null)
            {
                WebSiteId = webPage.WebSiteId;

                // Check if the user is logged in
                var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (!user.Identity.IsAuthenticated)
                {
                    // Only increment the visit count if the user is not logged in
                    await VisitorCounterService.IncrementPageVisitAsync(WebSiteId, CurrentPageUrl);
                }

                // Get the updated visit count for this page
                VisitCount = await VisitorCounterService.GetPageVisitCountAsync(WebSiteId, CurrentPageUrl);
            }
        }
    }
}