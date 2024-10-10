﻿
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
        IQueryable<Content> contents = Enumerable.Empty<Content>().AsQueryable();
        [SupplyParameterFromQuery]
        public int? WebPageId { get; set; }
        private int? ContentForEditing { get; set; } = null;

        public int ContentId { get; set; } // Fetch ContentId from the query

        bool StopEditing { get; set; } = false;

        bool editPageinformation { get; set; } = false;

        bool Create { get; set; } = false;

        public Content? Content { get; set; }

        ApplicationDbContext context = default!;

        private ExecuteAction PageExecution { get; set; } = ExecuteAction.EditSelect;


        private enum ExecuteAction
        {
            Wait,
            EditSelect,
            StopEditing,
            EditPageinformation,
            CreateContent,
            Preview,
            Delete,
            EditContent
        }

        protected override async Task OnInitializedAsync()
        {
            context = DbFactory.CreateDbContext();

            if (WebPageId.HasValue)
            {
                // Fetch content filtered by WebPageId
                contents = context.Contents.Where(c => c.WebPageId == WebPageId.Value);
            }
            else
            {
                // Fetch all content if no WebPageId is provided
                //contents = context.Contents;
            }
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

        private void DeleteContent()
        {
            ContentForEditing = null;
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
            PageExecution = ExecuteAction.EditSelect;
            contents = context.Contents.Where(c => c.WebPageId == WebPageId);
            StateHasChanged();
        }
        private void CreateDone()
        {
            ContentForEditing = null;
            PageExecution = ExecuteAction.EditSelect;
            contents = context.Contents.Where(c => c.WebPageId == WebPageId);
            StateHasChanged();
        }

        private void ResumeEditContent()
        {
            PageExecution = ExecuteAction.EditSelect;
            StopEditing = false;
            contents = context.Contents.Where(c => c.WebPageId == WebPageId);
            StateHasChanged();
        }

        public async ValueTask DisposeAsync() => await context.DisposeAsync();
    }
}