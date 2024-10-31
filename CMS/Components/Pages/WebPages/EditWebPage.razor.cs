
using CMS.Data;
using CMS.Entities;
using Microsoft.AspNetCore.Components;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
namespace CMS.Components.Pages.WebPages
{
    public partial class EditWebPage
    {
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        List<Content> Contents { get; set; } = new List<Content>();
        [SupplyParameterFromQuery]
        public int? WebPageId { get; set; }
        private int? WebSiteId { get; set; }
        private int? ContentForEditing { get; set; } = null;

        public int ContentId { get; set; }

        public Content? Content { get; set; }

        ApplicationDbContext context = default!;

        private bool HideToolbar = false;

        private ExecuteAction PageExecution { get; set; } = ExecuteAction.EditSelect;
        private bool drawNow { get; set; }

        private enum ExecuteAction
        {
            Wait,
            EditSelect,
            StopEditing,
            EditPageinformation,
            CreateContent,
            Preview,
            Delete,
            Draw,
            EditContent
        }

        protected override async Task OnInitializedAsync()
        {
            context = DbFactory.CreateDbContext();

           var  WebPage = await context.WebPages.FirstOrDefaultAsync(m => m.WebPageId == WebPageId);
           
            if (WebPage is null)
            {
                NavigationManager.NavigateTo("/error");
            }

            if (WebPageId.HasValue)
            {
                // Fetch content filtered by WebPageId
                Contents = context.Contents.Where(c => c.WebPageId == WebPageId.Value).ToList();
            }
            else
            {
                NavigationManager.NavigateTo("/error");
                // Fetch all content if no WebPageId is provided
                //contents = context.Contents;
            }

            WebSiteId = WebPage.WebSiteId;
          
        }
        private void EditContent(Content content)
        {
            ContentForEditing = content.ContentId;
            ContentId = content.ContentId;
            Content = content;
            PageExecution = ExecuteAction.EditContent;
        }
        private void AddContent()
        {

            ContentForEditing = null;
            PageExecution = ExecuteAction.CreateContent;
        }

        private void Draw()
        {

            if (HideToolbar)
            {
                drawNow = false;
            }
            else
            {
                drawNow = true;
                PageExecution = ExecuteAction.Draw;
            }
        }

        private void DeleteContent(int contentId)
        {
            ContentForEditing = contentId;
            PageExecution = ExecuteAction.Delete;
        }

        private void PauseEditContent()
        {
            ContentForEditing = null;
            PageExecution = ExecuteAction.Preview;
        }
        private void EditPageinformation()
        {
            PageExecution = ExecuteAction.EditPageinformation;
        }

        private void EditPageinformationDone()
        {
            ContentForEditing = null;
            PageExecution = ExecuteAction.EditSelect;
            Contents = context.Contents.Where(c => c.WebPageId == WebPageId).ToList();
            StateHasChanged();
        }
        private void CreateDone()
        {
            ContentForEditing = null;
            PageExecution = ExecuteAction.EditSelect;
            Contents = context.Contents.Where(c => c.WebPageId == WebPageId).ToList();
            StateHasChanged();
        }

        private void ResumeEditContent()
        {
            ContentForEditing = null;
            PageExecution = ExecuteAction.EditSelect;
            Contents = context.Contents.Where(c => c.WebPageId == WebPageId).ToList();
            StateHasChanged();
        }

        private void HideTools()
        {
            if (HideToolbar)
            {
                HideToolbar = false;
            }
            else
            { 
                HideToolbar = true;
            }
        }

        public async ValueTask DisposeAsync() => await context.DisposeAsync();
    }
}
