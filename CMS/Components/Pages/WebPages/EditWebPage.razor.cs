
using CMS.Data;
using CMS.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using CMS.Models;
using Microsoft.AspNetCore.Components.Web;
using Newtonsoft.Json;
using Microsoft.JSInterop;
using CMS.Services;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using System.Drawing;
using System;
using System.Linq;
using Markdig.Syntax;
using System.Data;
using CMS.Migrations;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.Extensions.Logging;
using NuGet.Protocol;
using System.Diagnostics;
using System.Numerics;
using Bogus.DataSets;
using System.Xml.Linq;
namespace CMS.Components.Pages.WebPages
{
    //ToDO: Chanfe variables name from ec. WebSiteId to webSiteId
    public partial class EditWebPage
    {
        [Inject] private ILayoutService LayoutService { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        List<Content> Contents { get; set; } = new List<Content>();
        List<Content> webPageContents { get; set; } = new List<Content>();
        [SupplyParameterFromQuery]
        public int? WebPageId { get; set; }
        private int? WebSiteId { get; set; }

        public string webPageBackgroundColor { get; set; } = "white";
        private int? contentForEditing { get; set; } = null;

        public int ContentId { get; set; }

        public Content? Content { get; set; }

        ApplicationDbContext context = default!;

        private bool hideToolbar = false;

        private bool addRowActive = false;

        private bool moveRowActive = false;

        private bool moveCellsActive = false;

        private bool infoMessage = false;

        private bool deleteContentActive = false;


        private bool resizeCellColumnSpanActive = false;

        private bool deleteRowActive = false;

        private string userInfoMessage = "";
        private bool isScrollSaved = false;

        private LayoutCell? draggedCell { get; set; } = null;

        private LayoutCell? hoveredCell { get; set; } = null;

        private int? draggedRow { get; set; } = null;

        private int? hoveredRow { get; set; } = null;
        private int hoveredRowDelete { get; set; } = 0;
        public WebPageLayout? layout { get; set; } = new WebPageLayout();

        public LayoutCell? layoutCell { get; set; } = null;

        private string userId { get; set; } = string.Empty;


        //ToDo: move to separate file and use everywhere instead of existing usage of determining  length for rows.
        public const int rowLength = 12;


        private ExecuteAction pageExecution { get; set; } = ExecuteAction.EditSelect;

        private enum ExecuteAction
        {
            Wait,
            EditSelect,
            StopEditing,
            EditPageinformation,
            CreateContent,
            Preview,
            Delete,
            SelectCellCreate,
            EditContent,
            DeleteSelect,
            Resize,
            DragRow,
            DragCell,
            DeleteRowSelect,
            AddRowSelect
        }

        protected override async Task OnInitializedAsync()
        {
            await GetUserID();
            context = DbFactory.CreateDbContext();

            var webPage = await context.WebPages.FirstOrDefaultAsync(m => m.WebPageId == WebPageId);

            if (webPage is null)
            {
                NavigationManager.NavigateTo("/error");
            }
            webPageBackgroundColor = webPage.BackgroundColor ?? "white"; // Default to white if null

            if (WebPageId.HasValue)
            {
                // Fetch content filtered by WebPageId
                Contents = context.Contents.Where(c => c.WebPageId == WebPageId.Value).ToList();

                if (Contents.Count > 0)
                {
                    var CurrentWebPageLayout = await LayoutService.GetLayoutAsync(WebPageId.Value);
                    // Initial population of layout cells and content
                    await GetLayout(CurrentWebPageLayout);
                }
                else
                {
                    await CreateNewRowAsync();
                }
            }
            else
            {
                NavigationManager.NavigateTo("/error");
                // Fetch all content if no WebPageId is provided
                //contents = context.Contents;
            }

            WebSiteId = webPage.WebSiteId;
        }

        //ToDo: move to separate class used in several files in the application.
        private async Task GetUserID()
        {
            var user = await GetCurrentUserService.GetCurrentUserAsync();
            if (user.Id == null)
            {
                throw new InvalidOperationException($"user Id {user.Id} does not exist.");
            }
            userId = user.Id;
        }

        private async Task GetLayout(WebPageLayout webPageLayout)
        {
            // If layout or LayoutCells is null or empty, generate new layout using Contents
            if (webPageLayout == null || webPageLayout.LayoutCells == null || !webPageLayout.LayoutCells.Any())
            {
                //ToDo: optimize and alter CreateNewRowAsync() method and replace the seeding code eith method:
                // Create a new list to hold layout cells
                var newLayoutCells = new List<LayoutCell>();

                int cellsPerRow = 12; // Number of cells per rowShift
                int totalContents = Contents.Count;

                int contentIndex = 0;
                int row = 1;
                int column = 1;

                // Loop through each content and create a rowShift for each one
                for (int i = 0; i < totalContents; i++)
                {
                    //If header navigation bar or footer set content for entire rowShift
                    if (Contents[contentIndex].TemplateId == 6 ||
                        Contents[contentIndex].TemplateId == 7 ||
                        Contents[contentIndex].TemplateId == 9)
                    {
                        newLayoutCells.Add(new LayoutCell
                        {
                            ContentId = Contents[contentIndex++].ContentId, // Add the content for the first column
                            Row = row,
                            Column = column,
                            ColumnSpan = cellsPerRow
                        });
                        // Move to the next rowShift
                        row++;
                        column = 1; // Reset column to 1 for the next rowShift
                    }
                    else
                    {
                        // First column in each rowShift will hold content
                        newLayoutCells.Add(new LayoutCell
                        {
                            ContentId = Contents[contentIndex++].ContentId, // Add the content for the first column
                            Row = row,
                            Column = column
                        });
                        // Fill the remaining 11 columns with null ContentId
                        for (int j = 1; j < cellsPerRow; j++)  // Start from column 2 to 12
                        {
                            newLayoutCells.Add(new LayoutCell
                            {
                                ContentId = null, // Empty cell
                                Row = row,
                                Column = column + j
                            });
                        }

                        // Move to the next rowShift
                        row++;
                        column = 1; // Reset column to 1 for the next rowShift
                    }

                }

                // Reassign the new list to layout.LayoutCells to trigger a state change
                layout.LayoutCells = newLayoutCells;
                await SaveLayoutChangesAsync();
            }
            else
            {
                // If layout already has cells, set the layout's LayoutCells to the provided layout
                layout.LayoutCells = webPageLayout.LayoutCells.ToList(); // Make sure to use a new list to trigger the state change
            }

            // Trigger UI refresh to reflect changes
            StateHasChanged();
        }

        // ToDo: Centralize use for all messages and create service for messages, used in multiple files NavBarInputForm etc.
        private void UserInformationMessageHide()
        {
            infoMessage = false;
            userInfoMessage = "";
        }

        private void UserInformationMessage(string Message)
        {
            infoMessage = true;
            userInfoMessage = Message;
        }

        private void DragCells()
        {
            RestoreScrollPosition();
            if (moveCellsActive)
            {
                ResetMenu();
                moveCellsActive = false;
                UserInformationMessage("Redigera innehåll");
                pageExecution = ExecuteAction.EditSelect;
            }
            else
            {
                ResetMenu();
                UserInformationMessage("Flytta innehåll.");
                moveCellsActive = true;
                pageExecution = ExecuteAction.DragCell;
            }
        }
        private async Task AddRowAsync()
        {
            //ToDo: select for add row.
            //RestoreScrollPosition();

            if (addRowActive)
            {
                ResetMenu();
                UserInformationMessage("Redigera innehåll");
                addRowActive = false;
            }
            else
            {
                ResetMenu();
                UserInformationMessage("Ny rad tillagd.");
                addRowActive = true;
                await CreateNewRowAsync();
            }
        }

        private void DragRows()
        {
            RestoreScrollPosition();

            if (moveRowActive)
            {
                ResetMenu();
                UserInformationMessage("Redigera innehåll");
                pageExecution = ExecuteAction.EditSelect;
                moveRowActive = false;
            }
            else
            {
                ResetMenu();
                UserInformationMessage("Flytta rad.");
                moveRowActive = true;
                pageExecution = ExecuteAction.DragRow;
            }
        }

        private void DeleteContentSelect()
        {
            RestoreScrollPosition();

            if (deleteContentActive)
            {
                ResetMenu();
                UserInformationMessage("Redigera innehåll");
                deleteContentActive = false;
                pageExecution = ExecuteAction.EditSelect;
            }
            else
            {
                ResetMenu();
                UserInformationMessage("Radera innehåll.");
                deleteContentActive = true;
                pageExecution = ExecuteAction.DeleteSelect;
            }
        }

        private void ResizeCell()
        {
            RestoreScrollPosition();

            if (resizeCellColumnSpanActive)
            {
                ResetMenu();
                UserInformationMessage("Redigera innehåll");
                resizeCellColumnSpanActive = false;
                pageExecution = ExecuteAction.EditSelect;
            }
            else
            {
                ResetMenu();
                resizeCellColumnSpanActive = true;
                UserInformationMessage("Ändra storlek.");
                pageExecution = ExecuteAction.Resize;
            }
        }

        private void EditContent(Content content)
        {
            UserInformationMessage("Redigera innehåll.");
            RestoreScrollPosition();

            if (content == null || content.ContentId == null)
            {
                Console.WriteLine("No content Selected, edit aborted.");
                return;
            }
            contentForEditing = content.ContentId;
            ContentId = content.ContentId;
            Content = content;
            pageExecution = ExecuteAction.EditContent;
        }
        private void AddContent()
        {
            UserInformationMessage("Skapa nytt innehåll.");
            RestoreScrollPosition();
            contentForEditing = null;
            pageExecution = ExecuteAction.CreateContent;
        }

        private void SelectCell()
        {
            UserInformationMessage("Välj plats för innehåll");
            RestoreScrollPosition();
            contentForEditing = null;
            pageExecution = ExecuteAction.SelectCellCreate;

        }

        private void DeleteContent(int? contentId)
        {
            if (contentId == null)
            {
                UserInformationMessage("Inget innehåll hittat att radera.");
                RestoreScrollPosition();
                return;
            }
            RestoreScrollPosition();
            contentForEditing = contentId;
            pageExecution = ExecuteAction.Delete;
        }

        private void PauseEditContent()
        {
            RestoreScrollPosition();
            contentForEditing = null;
            pageExecution = ExecuteAction.Preview;
        }
        private void EditPageinformation()
        {
            UserInformationMessage("Redigera sidans information.");
            RestoreScrollPosition();
            pageExecution = ExecuteAction.EditPageinformation;
        }

        private void EditPageinformationDone()
        {
            UserInformationMessage("Redigera innehåll.");
            RestoreScrollPosition();
            ResetMenu();
            contentForEditing = null;
            pageExecution = ExecuteAction.EditSelect;
            Contents = context.Contents.Where(c => c.WebPageId == WebPageId).ToList();
            StateHasChanged();
        }

        private async Task SelectCellForNewContent(LayoutCell cell)
        {
            UserInformationMessage("Välj plats för innehållets placering.");
            RestoreScrollPosition();
            // If not Empty cell is choosen for new content, return.
            if (cell.ContentId != null)
            {
                UserInformationMessage("Upptagen plats vald, välj en ledig plats för innehållet..");
                return;
            }

            int? newContentId = null;
            AddNewContentToLayout(out newContentId, webPageContents, cell);
            UserInformationMessage("Nytt innehåll skapat.");

            await InsertNewContentInLayoutAsync(cell, webPageContents, newContentId);

        }

        private async Task InsertNewContentInLayoutAsync(LayoutCell cell, List<Content> webPageContents, int? newContentId)
        {
            RestoreScrollPosition();
            var newCell = new LayoutCell
            {
                Column = cell.Column,
                Row = cell.Row,
                ColumnSpan = 1,
                RowSpan = 1,
                ContentId = newContentId
            };

            var newCellIndex = GetCellIndex(cell);
            ReinsertCellInLayout(newCellIndex, newCell);

            Contents = webPageContents;

            await SaveLayoutChangesAsync();

            UserInformationMessage("Nytt innehåll har lagts till i layout:en.");
            pageExecution = ExecuteAction.EditSelect;
        }

        private void AddNewContentToLayout(out int? newContentId, List<Content> webPageContents, LayoutCell cell)
        {
            newContentId = webPageContents
            .Where(c => !layout.LayoutCells.Any(cell => cell.ContentId == c.ContentId))
            .Select(c => c.ContentId)
            .FirstOrDefault();  // Retrieves the first (and presumably only) matching ContentID
        }

        private void CreateContentDone()
        {
            RestoreScrollPosition();
            contentForEditing = null;

            // Get current content.
            // ToDo: evaluate, if use of callback from create component will lead to less DB calls.
            webPageContents = context.Contents.Where(c => c.WebPageId == WebPageId).ToList();

            // No content added, return to editing of webpage.
            if (webPageContents.Count() == Contents.Count())
            {
                Console.WriteLine($"No new content found, creating aborted");
                pageExecution = ExecuteAction.EditSelect;
                return;
            }

            pageExecution = ExecuteAction.SelectCellCreate;

        }

        private void Done()
        {
            RestoreScrollPosition();
            contentForEditing = null;
            pageExecution = ExecuteAction.EditSelect;
            UserInformationMessage("Redigera innehåll");
            StateHasChanged();
        }

        private async Task DeleteDoneAsync()
        {
            RestoreScrollPosition();
            if (contentForEditing != null)
            {


                webPageContents = context.Contents.Where(c => c.WebPageId == WebPageId).ToList();
                if (webPageContents.Count() == Contents.Count())
                {
                    Console.WriteLine("No content removed, operation aborted.");
                    pageExecution = ExecuteAction.DeleteSelect;
                    return;

                }

                await UpdateRowAfterRemovalOfContentAsync();

                Contents = webPageContents;
                contentForEditing = null;
                await SaveLayoutChangesAsync();
                StateHasChanged();
            }
            else
            {
                UserInformationMessage("Tom yta vald, Det finns inget att radera. ");
            }
            pageExecution = ExecuteAction.DeleteSelect;
        }

        private async Task UpdateRowAfterRemovalOfContentAsync()
        {
            if (contentForEditing == null)
            {   // ToDo: evaluate, if actions is needed.
                Console.WriteLine("Content Id for removal is null, operation aborted.");
                return;
            }

            LayoutCell clearedCell = layout.LayoutCells.FirstOrDefault(c => c.ContentId == contentForEditing);

            await ClearCellsContentAsync(clearedCell);

        }

        private async Task ClearCellsContentAsync(LayoutCell clearedCell)
        {
            int? clearedCellIndex = GetCellIndex(clearedCell);

            if (clearedCellIndex == null)
            {
                UserInformationMessage("Inget innhåll valt.");
                Console.WriteLine("Could not find cell in layout, operation aborted.");
                return;
            }

            var oldColumnSpan = clearedCell.ColumnSpan;

            // Control size and format row when needed after new size is set for a cell,
            await UpdateColumnSpan(clearedCell, 1);

            // Remove cell content 
            clearedCell.ContentId = null;

            // Insert Updated cell in layout.
            ReinsertCellInLayout(clearedCellIndex, clearedCell);
        }

        private void ResumeEditContent()
        {
            contentForEditing = null;
            pageExecution = ExecuteAction.EditSelect;
            Contents = context.Contents.Where(c => c.WebPageId == WebPageId).ToList();
            StateHasChanged();
        }

        private void DeleteSelectRow()
        {
            RestoreScrollPosition();
            if (deleteRowActive)
            {
                ResetMenu();
                UserInformationMessage("Redigera innehåll");
                deleteRowActive = false;
                pageExecution = ExecuteAction.EditSelect;
            }
            else
            {
                ResetMenu();
                UserInformationMessage("Radera rad.");
                deleteRowActive = true;
                pageExecution = ExecuteAction.DeleteRowSelect;
            }
        }
        private async Task DeleteRowAsync(int? row)
        {
            RestoreScrollPosition();
            if (row == null)
            {
                UserInformationMessage("Ingen rad vald att radera.");
                return;
            }

            if (layout.LayoutCells.Any(c => c.ContentId != null && c.Row == row))
            {
                UserInformationMessage("Kan inte radera rad med innehåll, flytta eller radera innehåll först.");
                return;
            }

            DeleteRowInLayout(row.Value);
            await SaveLayoutChangesAsync();

            UserInformationMessage("Rad raderad");

        }

        // Hides tool bar
        private void HideToolsAsync()
        {
            RestoreScrollPosition();
            if (hideToolbar)
            {
                hideToolbar = false;
            }
            else
            {
                hideToolbar = true;
            }
        }
        // ToDo: use pageExecution states instead.
        private void ResetMenu()
        {
            addRowActive = false;

            moveRowActive = false;

            moveCellsActive = false;

            deleteContentActive = false;

            resizeCellColumnSpanActive = false;

            deleteRowActive = false;
        }

        public async ValueTask DisposeAsync() => await context.DisposeAsync();

        //Start drag and drop content order:
        //Todo:Verifications and best practises needs to be handled.
        private async Task InitializeDrag()
        {
            await JSRuntime.InvokeVoidAsync("eval", @"
                if (!window.setupDragPreview) {
                    window.setupDragPreview = function(contentId) {
                        // Select the element using data-content-id attribute
                        var element = document.querySelector('[data-content-id=""' + contentId + '""]'); 

                        // Check if the element exists
                        if (!element) {
                            console.error('Element with ContentId ' + contentId + ' not found.');
                            return; // Exit if the element is not found
                        }

                        // Make the original element fully transparent and disable interaction
                        element.style.opacity = '0'; // Make the original element fully transparent
                        element.style.pointerEvents = 'none'; // Prevent interaction with the original element during drag

                        // Clone the element to create a custom preview
                        var dragPreview = element.cloneNode(true); // Create a clone with the same content and styles
                        dragPreview.style.position = 'absolute'; // Absolute positioning for the drag preview
                        dragPreview.style.zIndex = '9999'; // Make sure the preview is above other elements
                        dragPreview.style.pointerEvents = 'none'; // Prevent interaction with the preview
                        dragPreview.style.opacity = '1'; // Make the preview fully visible
                        dragPreview.style.width = '60%';
                        dragPreview.style.height = '60%';
                        dragPreview.style.border = 'none';

                        // Append the preview to the body
                        document.body.appendChild(dragPreview);

                        // Function to move the preview with the mouse
                        var movePreview = function(event) {
                            // Get the size of the preview element
                            var previewWidth = dragPreview.offsetWidth;
                            var previewHeight = dragPreview.offsetHeight;

                            // Get the current scroll position to adjust the preview position
                            var scrollTop = window.scrollY;

                            // Adjust the preview's position based on the mouse position and scroll position
                            dragPreview.style.top = (event.clientY + scrollTop) + 'px'//(event.clientY + scrollTop - previewHeight / 2) + 'px';  // Center vertically
                            dragPreview.style.left = (event.clientX - previewWidth / 2) + 'px';  // Center horizontally
                        };

                        // Immediately position the preview at the mouse cursor's position (centered)
                        movePreview({ clientX: window.event.clientX, clientY: window.event.clientY });

                        // Listen for the mousemove event to update the preview position
                        document.addEventListener('mousemove', movePreview);

                        // Store the dragPreview and the original element in global variables for later use
                        window.dragPreviewElement = dragPreview;
                        window.originalElement = element;

                        // Clean up the preview and reset the original element when drag ends (on mouseup)
                        var cleanupOnMouseUp = function() {
                            window.removeDragPreview(); // Call the cleanup function

                            // Remove the mousemove event listener when drag ends
                            document.removeEventListener('mousemove', movePreview);
                            document.removeEventListener('mouseup', cleanupOnMouseUp); // Remove the mouseup listener
                        };

                        // Add a mouseup event to clean up the preview
                        document.addEventListener('mouseup', cleanupOnMouseUp);
                    };

                    // Cleanup function to remove the drag preview and reset the original element
                    window.removeDragPreview = function() {
                        if (window.dragPreviewElement) {
                            document.body.removeChild(window.dragPreviewElement); // Remove the preview
                            window.dragPreviewElement = null; // Clear the preview reference
                        }

                        if (window.originalElement) {
                            window.originalElement.style.opacity = '1'; // Reset the original element's opacity
                            window.originalElement.style.pointerEvents = ''; // Re-enable interaction with the original element
                            window.originalElement = null; // Clear the reference to the original element
                        }
                    };
                }

            ");
        }

        //Primitive methods for restoring the scrollY after reset to top after rewrite/rerendering of html.

        //Todo:Verifications and best practises needs to be handled, see git projects scrumboard.
        //private void RestoreScrollPosition()
        //{
        //    // Save the current scroll position using localStorage in JavaScript (store as floating point number)
        //    JSRuntime.InvokeVoidAsync("eval", @"
        //        localStorage.setItem('scrollPosition', window.scrollY); // Store the exact scrollY value
        //        console.log('scroll position saved:', window.scrollY);
        //        localStorage.setItem('retries', 0); // Initialize retries in localStorage
        //        console.log('retries initialized:', 0);

        //    ");
        //}

        ////Todo:Verifications and best practises needs to be handled, see git projects scrumboard.
        //private void LoadScrollPosition()
        //{
        //    //InitializeScrollTracking();
        //    //Run JavaScript to attempt restoring the scroll position
        //    JSRuntime.InvokeVoidAsync("eval", @"
        //        (function attemptToRestoreScrollPosition() {
        //             Retrieve retries from localStorage.
        //            let retries = parseInt(localStorage.getItem('retries'), 10) || 0;

        //             If retrying is not allowed, stop the function.
        //            if (retries > 3) {
        //                console.log('Restoration already attempted. Aborting further retries.');
        //                return; // Stop further execution.
        //            }
        //            console.log('retries:', retries);

        //             Get stored scroll position from localStorage.
        //            var storedScrollPosition = localStorage.getItem('scrollPosition');

        //             Ensure stored position exists and convert it to a floating-point number.
        //            storedScrollPosition = parseFloat(storedScrollPosition);

        //             Retrieve the current scroll position (window.scrollY) as a floating-point number.
        //            var currentScrollPosition = window.scrollY;
        //            console.log('current scroll position:', currentScrollPosition);

        //             Check if stored position exists and retries.
        //            if (!isNaN(storedScrollPosition) && retries < 2) {
        //                window.scrollTo(0, storedScrollPosition); // Scroll to the saved position.
        //                console.log('Scroll position restored:', storedScrollPosition);
        //                console.log('Current window.scrollY:', window.scrollY);

        //                retries++;  // Increment the retry counter.
        //                localStorage.setItem('retries', retries); // Update retries count in localStorage.

        //                 Retry after 50ms, delay for timing issues
        //                setTimeout(attemptToRestoreScrollPosition, 50); // Retry with a 50ms delay.
        //            } else {
        //                 If position is restored or matches, stop further retries
        //                localStorage.setItem('retries', retries); // Store the retries count in localStorage.
        //                console.log('Scroll position is already at or restored to:', storedScrollPosition);
        //                localStorage.setItem('retries', 0); //Reset
        //            }
        //        })();
        //    ");
        //}

        //Primitive time intense/agressive short intervals, restoring repeat  during a timespan restoring for determining if it is a
        //    viable fallback approach for
        //removing flickering at redraw/rerender of content(page reset scrollY to top).
        //Results: greate improvement with ocational/glitches/twitch/flickering.

        //    private void RestoreScrollPosition()
        //    {
        //        // Save the current scroll position using localStorage in JavaScript
        //        JSRuntime.InvokeVoidAsync("eval", @"
        //    localStorage.setItem('scrollPosition', window.scrollY); // Store the exact scrollY value
        //    localStorage.setItem('retries', 0); // Initialize retries in localStorage
        //    console.log('scroll position saved:', window.scrollY);
        //");

        //        // Show the transitional div when starting the scroll restoration
        //        JSRuntime.InvokeVoidAsync("eval", @"
        //    addFullScreenDiv(); // Show full-screen overlay
        //");

        //        // Add a retry mechanism for restoring the scroll position
        //        JSRuntime.InvokeVoidAsync("eval", @"
        //    (function attemptRestoreScrollPosition() {
        //        // Get the stored scroll position
        //        var storedScrollPosition = localStorage.getItem('scrollPosition');
        //        storedScrollPosition = parseFloat(storedScrollPosition);

        //        if (!isNaN(storedScrollPosition)) {
        //            var retries = 0;
        //            var maxRetries = 500; // Maximum retries
        //            var interval = 1; // Retry every 1ms

        //            // Retry restoring the scroll position every 1ms
        //            function restoreScroll() {
        //                // Check if the scroll position needs to be restored
        //                var currentScrollPosition = window.scrollY;
        //                if (currentScrollPosition !== storedScrollPosition) {
        //                    window.scrollTo(0, storedScrollPosition);
        //                    console.log('Scroll position restored to:', storedScrollPosition);
        //                } else {
        //                    console.log('Scroll position already at:', storedScrollPosition);
        //                }

        //                retries++;
        //                if (retries < maxRetries) {
        //                    // Retry after 1ms
        //                    setTimeout(restoreScroll, interval);
        //                } else {
        //                    // After retries stop, remove the full-screen overlay
        //                    removeFullScreenDiv(); // Hide full-screen overlay
        //                }
        //            }

        //            // Start the restoration process
        //            restoreScroll();
        //        } else {
        //            console.log('No valid scroll position to restore.');
        //            // Remove the full-screen overlay in case no scroll position is found
        //            removeFullScreenDiv();
        //        }
        //    })();
        //");
        //    }



        //Primitive time intense/agressive short intervals v2
        // Added transit div to soften the rendering
        //, restoring repeat during a timespan restoring for determining if it is a
        //    viable fallback approach for
        //removing flickering at redraw/rerender of content(page reset scrollY to top).
        //Results: greate improvement with ocational/glitches/twitch/flickering.
        
    //    private void ProbeDragcellContainer()
    //    {
    //        JSRuntime.InvokeVoidAsync("eval", $@"
    //    console.log('Grid drag cell container width:', document.querySelector('.container-content-layout-grid-drag-cell').offsetHeight);
    //");
    //    }

    //    private void ProbeContainer()
    //    {
    //        JSRuntime.InvokeVoidAsync("eval", $@"
    //        console.log('Grid container width:', document.querySelector('.container-content-layout-grid').offsetHeight);
    //");
    //    }

        private void RestoreScrollPosition(bool coverTransition = true)
        {

            if(coverTransition)
            {
                // Transition overlay
                TransitionCoverDiv(150, 70, webPageBackgroundColor);
            }


            // Save the current scroll position using localStorage in JavaScript
            JSRuntime.InvokeVoidAsync("eval", $@"

        // Save the scroll position and initialize retries
        localStorage.setItem('scrollPosition', window.scrollY);
        localStorage.setItem('retries', 0);
        console.log('scroll position saved:', window.scrollY);

        // Add a retry mechanism for restoring the scroll position
        (function attemptRestoreScrollPosition() {{
            var storedScrollPosition = localStorage.getItem('scrollPosition');
            storedScrollPosition = parseFloat(storedScrollPosition);

            if (!isNaN(storedScrollPosition)) {{
                var retries = 0;
                var maxRetries = 2; // Maximum retries
                var interval = 45; // Retry every 1ms

                // Retry restoring the scroll position every 1ms
                function restoreScroll() {{
                    var currentScrollPosition = window.scrollY;
                    if (currentScrollPosition !== storedScrollPosition) {{
                        window.scrollTo(0, storedScrollPosition);
                        console.log('Scroll position restored to:', storedScrollPosition);
                    }} else {{
                        console.log('Scroll position already at:', storedScrollPosition);
                    }}

                    retries++;
                    if (retries < maxRetries) {{
                        setTimeout(restoreScroll, interval);
                    }}
                }}

                // Start the restoration process
                restoreScroll();
            }} else {{
                console.log('No valid scroll position to restore.');
            }}
        }})();
    ");
        }

        // This method adds a full-screen transition overlay
        public void TransitionCoverDiv(int displayDurationMs, int fadeOutDurationMs, string backgroundColor)
        {
            JSRuntime.InvokeVoidAsync("eval", $@"
        function addFullScreenDiv(displayDurationMs, fadeOutDurationMs) {{
            // Create the div element
            const div = document.createElement('div');
            div.classList.add('transitional-overlay'); // Adding a class instead of an ID

            // Set the styles to cover the viewport with a solid color
            div.style.position = 'fixed';  // Position it fixed to the viewport
            div.style.top = '0';
            div.style.left = '0';
            div.style.width = '100vw';  // Cover full viewport width
            div.style.height = '100vh'; // Cover full viewport height
            div.style.backgroundColor = '{backgroundColor}'; // Div color
            div.style.zIndex = '9999';  // Ensure it appears above other content
            div.style.opacity = '1';  // Set initial opacity to 1 (visible)
            div.style.transition = 'opacity ' + fadeOutDurationMs + 'ms ease'; // Smooth fade-out transition
            div.style.border ='none';

            // Append the div to the body
            document.body.appendChild(div);

            // Set a timer to remove the div after the specified displayDurationMs
            setTimeout(function() {{
                removeFullScreenDiv(fadeOutDurationMs);
            }}, displayDurationMs); // e.g., 2000ms = 2 seconds
        }}

        function removeFullScreenDiv(fadeOutDurationMs) {{
            // Select the first div with the class 'transitional-overlay' to remove it
            const div = document.querySelector('.transitional-overlay');  // Use class to select the div
            if (div) {{
                // Add smooth fade-out before removal
                div.style.opacity = '0'; // Fade out
                setTimeout(function() {{
                    document.body.removeChild(div); // Remove div after fade-out
                }}, fadeOutDurationMs); // Use the provided fade-out duration
            }}
        }}

        // Call function to show the div with dynamic display and fade-out times
        addFullScreenDiv({displayDurationMs}, {fadeOutDurationMs});
    ");
        }




        // Attempt to log the time for reset: inconsisten results with flickering during restoring the scrollY .

        //    private void LoadScrollPosition()
        //    {
        //        InitializeScrollTracking();

        //        // Run JavaScript to attempt restoring the scroll position after the page layout is updated
        //        JSRuntime.InvokeVoidAsync("eval", @"
        //    (function attemptToRestoreScrollPosition() {
        //        // Retrieve retries from localStorage.
        //        let retries = parseInt(localStorage.getItem('retries'), 10) || 0;

        //        // If retrying is not allowed, stop the function.
        //        if (retries > 3) {
        //            console.log('Restoration attempts exceeded. Aborting further retries.');
        //            return; // Stop further execution.
        //        }

        //        // Get stored scroll position from localStorage.
        //        var storedScrollPosition = localStorage.getItem('scrollPosition');

        //        // Ensure stored position exists and convert it to a floating-point number.
        //        storedScrollPosition = parseFloat(storedScrollPosition);

        //        // Check if stored position exists and handle the restoration attempt.
        //        if (!isNaN(storedScrollPosition)) {
        //            // Request the next frame to restore scroll position after layout has stabilized
        //            function restoreScroll() {
        //                // Get the current scroll position
        //                var currentScrollPosition = window.scrollY;

        //                // Only restore scroll position if it's different from the current position
        //                if (currentScrollPosition !== storedScrollPosition) {
        //                    window.scrollTo(0, storedScrollPosition);
        //                    console.log('Scroll position restored to:', storedScrollPosition);
        //                } else {
        //                    console.log('Scroll position is already restored to:', storedScrollPosition);
        //                }

        //                // Increment retry counter
        //                retries++;
        //                localStorage.setItem('retries', retries);

        //                // Retry if necessary (with a 50ms delay)
        //                if (retries < 3) {
        //                    setTimeout(restoreScroll, 50);
        //                }
        //            }

        //            // Use requestAnimationFrame for smoother transition
        //            requestAnimationFrame(restoreScroll);
        //        } else {
        //            console.log('No valid scroll position to restore.');
        //        }
        //    })();
        //");
        //    }

        //version 2 time logging. Results: cuts down the missed restorations to top position of page, still high amount of results with flickering.
        //    private void RestoreScrollPosition()
        //    {
        //        // Save the current scroll position using localStorage in JavaScript (store as floating point number)
        //        JSRuntime.InvokeVoidAsync("eval", @"
        //    localStorage.setItem('scrollPosition', window.scrollY); // Store the exact scrollY value
        //    localStorage.setItem('retries', 0); // Initialize retries in localStorage
        //    console.log('scroll position saved:', window.scrollY);
        //");

        //        // Add a retry mechanism for restoring the scroll position with reasonable intervals
        //        JSRuntime.InvokeVoidAsync("eval", @"
        //    (function attemptRestoreScrollPosition() {
        //        var storedScrollPosition = localStorage.getItem('scrollPosition');
        //        storedScrollPosition = parseFloat(storedScrollPosition);

        //        if (!isNaN(storedScrollPosition)) {
        //            var retries = 0;
        //            var maxRetries = 500;
        //            var interval = 5; // Increase interval to 5ms
        //            var startTime = Date.now();
        //            var matchCount = 0;

        //            function restoreScroll() {
        //                // First attempt to restore
        //                window.scrollTo(0, storedScrollPosition);
        //                console.log('First restore attempt to:', storedScrollPosition);

        //                // Wait 5ms before the second attempt
        //                setTimeout(function() {
        //                    window.scrollTo(0, storedScrollPosition);
        //                    console.log('Second restore attempt to:', storedScrollPosition);

        //                    // Check if scroll position matches after second attempt
        //                    if (window.scrollY === storedScrollPosition) {
        //                        matchCount++;
        //                        console.log('Scroll position matched at:', window.scrollY);

        //                        // Stop retrying if the condition is met twice
        //                        if (matchCount >= 2) {
        //                            var endTime = Date.now();
        //                            console.log('Restoration successful after', retries, 'retries.');
        //                            console.log('Time taken:', endTime - startTime, 'ms');
        //                            return; // Stop retrying
        //                        }
        //                    }

        //                    retries++;
        //                    if (retries < maxRetries) {
        //                        setTimeout(restoreScroll, interval); // Retry every 5ms
        //                    } else {
        //                        var endTime = Date.now();
        //                        console.log('Max retries reached. Time taken:', endTime - startTime, 'ms');
        //                    }
        //                }, 10); // Add a small delay between first and second attempt
        //            }

        //            // Start the restoration process
        //            restoreScroll();
        //        } else {
        //            console.log('No valid scroll position to restore.');
        //        }
        //    })();
        //");
        //    }

        // version 3 logging little better timing,  not much better than original primitive method.
        //    private void RestoreScrollPosition()
        //    {
        //        // Save the current scroll position using localStorage in JavaScript
        //        JSRuntime.InvokeVoidAsync("eval", @"
        //    localStorage.setItem('scrollPosition', window.scrollY);
        //    localStorage.setItem('retries', 0);
        //    console.log('scroll position saved:', window.scrollY);
        //");

        //        // Retry mechanism for restoring scroll position
        //        JSRuntime.InvokeVoidAsync("eval", @"
        //    (function attemptRestoreScrollPosition() {
        //        var storedScrollPosition = localStorage.getItem('scrollPosition');
        //        storedScrollPosition = parseFloat(storedScrollPosition);

        //        if (!isNaN(storedScrollPosition)) {
        //            var retries = 0;
        //            var maxRetries = 1000; // Increase retries to handle edge cases
        //            var interval = 5; // Retry every 5ms
        //            var matchCount = 0;
        //            var startTime = Date.now();

        //            function restoreScroll() {
        //                // First attempt to restore scroll position
        //                window.scrollTo(0, storedScrollPosition);
        //                console.log('First restore attempt to:', storedScrollPosition);

        //                // Wait 20ms before second attempt to ensure page stabilization
        //                setTimeout(function() {
        //                    window.scrollTo(0, storedScrollPosition);
        //                    console.log('Second restore attempt to:', storedScrollPosition);

        //                    // Check if scroll position matches
        //                    if (window.scrollY === storedScrollPosition) {
        //                        matchCount++;
        //                        console.log('Scroll position matched at:', window.scrollY);
        //                    }

        //                    // Stop retrying if the condition is met twice
        //                    if (matchCount >= 2) {
        //                        var endTime = Date.now();
        //                        console.log('Restoration successful after', retries, 'retries.');
        //                        console.log('Time taken:', endTime - startTime, 'ms');
        //                        return; // Stop retrying
        //                    }

        //                    retries++;
        //                    if (retries < maxRetries) {
        //                        setTimeout(restoreScroll, interval); // Retry after 5ms
        //                    } else {
        //                        var endTime = Date.now();
        //                        console.log('Max retries reached. Time taken:', endTime - startTime, 'ms');
        //                    }
        //                }, 20); // Delay of 20ms between attempts
        //            }

        //            // Start the restoration process
        //            restoreScroll();
        //        } else {
        //            console.log('No valid scroll position to restore.');
        //        }
        //    })();
        //");
        //    }

        //Not improvments:
        // You can suppress or throttle scroll events during the restoration process to prevent them from interfering.
        // Setting a flag during scroll restoration and temporarily disabling scroll handling until the restoration is complete.
        //    private void RestoreScrollPosition()
        //    {
        //        JSRuntime.InvokeVoidAsync("eval", @"
        //var isRestoringScroll = false;
        //function restoreScrollPosition()
        //{
        //    if (isRestoringScroll) return; // Prevent nested restore calls
        //    isRestoringScroll = true;

        //    var storedScrollPosition = localStorage.getItem('scrollPosition');
        //    storedScrollPosition = parseFloat(storedScrollPosition);

        //    if (!isNaN(storedScrollPosition))
        //    {
        //        var retries = 0;
        //        var maxRetries = 500;
        //        var interval = 10;

        //        var restoreInterval = setInterval(function() {
        //            requestAnimationFrame(function() {
        //                window.scrollTo(0, storedScrollPosition);
        //            });

        //            retries++;
        //            if (retries > maxRetries || window.scrollY === storedScrollPosition)
        //            {
        //                clearInterval(restoreInterval);
        //                isRestoringScroll = false; // Allow further restores
        //                if (window.scrollY === storedScrollPosition)
        //                {
        //                    console.log('Scroll position successfully restored after', retries, 'attempts.');
        //                }
        //                else
        //                {
        //                    console.log('Max retries reached, scroll position not restored.');
        //                }
        //            }
        //        }, interval);
        //    }
        //    else
        //    {
        //        console.log('No scroll position saved.');
        //    }
        //}
        //restoreScrollPosition(); //invoke 
        //");
        //    }



        //potential fallback solution use agressive primitive savefunction when  clicked,
        //clicked or the actions/rewrite rerender after click seems to trigger the flickering.

        //TESTING PROBE:
        //<button @onclick="InitializeScrollTracking">Start Scroll Tracking</button>
        // C# method to initialize scroll tracking
        //    private void InitializeScrollTracking()
        //    {
        //        JSRuntime.InvokeVoidAsync("eval", @"
        //    function initializeScrollTracking() {
        //        console.log('PROBE: Script is running');  // Log script start

        //        let resetCause = '';

        //        // Function to log the scroll position and reset cause
        //        function logScrollPosition(message) {
        //            console.log('PROBE:', message);
        //            console.log('PROBE: Current Scroll Position:', window.scrollY);
        //            console.log('PROBE: Reset Cause:', resetCause);  // Log reset cause
        //        }

        //        // Function to probe scroll position regularly
        //        function probeScrollPosition() {
        //            const scrollY = window.scrollY;

        //            // Log when the viewport is reset to top (scrollY === 0)
        //            if (scrollY === 0) {
        //                logScrollPosition('Viewport reset to top');
        //            }

        //            // Continue probing the scroll position every 100ms
        //            setTimeout(probeScrollPosition, 100);
        //        }

        //        // Start probing scroll position immediately after page load
        //        window.addEventListener('load', function() {
        //            resetCause = 'Page Loaded';  // Page load triggers the reset cause
        //            probeScrollPosition();
        //            logScrollPosition('Page Loaded and Probing Started');

        //            // Restore scroll position if it's saved in localStorage
        //            const storedScrollPosition = localStorage.getItem('scrollPosition');
        //            if (storedScrollPosition !== null) {
        //                resetCause = 'Restoring scroll position';
        //                console.log('PROBE: Restoring scroll position:', storedScrollPosition);
        //                window.scrollTo(0, storedScrollPosition);  // Smooth scroll to restored position
        //                logScrollPosition('Restoring scroll position');
        //            }
        //        });

        //        // Detect page unload (refresh or navigation)
        //        window.addEventListener('beforeunload', function() {
        //            resetCause = 'Before Unload: Saving scroll position';  // Save cause before unload
        //            logScrollPosition('Before Unload: Saving scroll position');
        //            localStorage.setItem('scrollPosition', window.scrollY);  // Save scroll position
        //        });

        //        // Detect SPA page navigation or re-renders (popstate event)
        //        window.addEventListener('popstate', function() {
        //            resetCause = 'SPA Navigation or Re-render Detected';
        //            logScrollPosition('SPA Navigation or Re-render triggered');
        //        });

        //        // Detect toggling between Edit and Drag actions
        //        window.addEventListener('click', function(event) {
        //            if (event.target && event.target.closest('.btn')) {
        //                resetCause = 'Button Clicked: Action toggled';
        //                logScrollPosition('Button clicked, action toggled');
        //            }
        //        });

        //        // Listen for changes in the layout (drag or edit actions)
        //        document.getElementById('myButton')?.addEventListener('click', function() {
        //            resetCause = 'Button Clicked: Resetting scroll position';
        //            window.scrollTo(0, 0);  // Example: Reset scroll to top
        //            logScrollPosition('Button clicked, scroll reset');
        //        });

        //        // Listen for other actions (content change, modal opening, etc.)
        //        document.getElementById('contentChanged')?.addEventListener('click', function() {
        //            resetCause = 'Content Changed: Resetting scroll position';
        //            window.scrollTo(0, 0);  // Reset scroll to top after content changes
        //            logScrollPosition('Content changed, scroll reset');
        //        });
        //    }

        //    // Initialize the scroll tracking functionality
        //    initializeScrollTracking();
        //");
        //    }

        //End testing probe

        //private void LeaveRow() 
        //{
        //    RestoreScrollPosition(false);

        //    hoveredRowDelete = 0;
        //}

        // Change bacgkroundcolor opacity for the row mouse enters:

        //    private async Task InitializeMouseEnterOverlay()
        //    {
        //        await JSRuntime.InvokeVoidAsync("eval", @"
        //    if (!window.setupMouseEnterOverlay) {
        //        window.setupMouseEnterOverlay = function(contentId, color) {
        //            // Select the element using data-content-id attribute
        //            var element = document.querySelector('[data-content-id=""' + contentId + '""]'); 

        //            // Check if the element exists
        //            if (!element) {
        //                console.error('Element with ContentId ' + contentId + ' not found.');
        //                return; // Exit if the element is not found
        //            }

        //            // Create the overlay
        //            var overlay = document.createElement('div');
        //            overlay.style.position = 'absolute';
        //            overlay.style.top = element.offsetTop + 'px';  // Position the overlay relative to the element
        //            overlay.style.left = element.offsetLeft + 'px';
        //            overlay.style.width = '100%';
        //            overlay.style.height = element.offsetHeight + 'px';
        //            overlay.style.backgroundColor = color;  // Set the background color passed as an argument
        //            overlay.style.opacity = '0.3';  // Semi-transparent overlay
        //            overlay.style.zIndex = '9998';  // Place overlay above the element
        //            overlay.style.pointerEvents = 'none';  // Prevent the overlay from interfering with mouse events
        //            document.body.appendChild(overlay);

        //            //// Change the background color of the original element on mouseenter
        //            //element.addEventListener('mouseenter', function() {
        //            //    element.style.backgroundColor = color; // Change the background color
        //            //});

        //            //// Reset the background color of the original element when mouse leaves
        //            //element.addEventListener('mouseleave', function() {
        //            //    element.style.backgroundColor = ''; // Reset to the original background color
        //            //});

        //            // Cleanup the overlay when the mouse leaves
        //            element.addEventListener('mouseleave', function() {
        //                document.body.removeChild(overlay); // Remove the overlay
        //            });
        //        };
        //    }
        //");
        //    }


        //Change bacgkroundcolor opacity for the row mouse enters v.2:
        private async Task InitializeMouseEnterOverlay()
        {
            // JS call to setup the overlay functionality (this is your method now)
            await JSRuntime.InvokeVoidAsync("eval", @"
                window.setupMouseEnterOverlay = function(contentId, row, column, color) {
                    var element;

                    // If contentId exists, use it to find the element
                    if (contentId) {
                        // Correct syntax for JavaScript: double quotes inside single quotes for attribute selectors
                        element = document.querySelector('[data-content-id=""' + contentId + '""]');
                    }

                    // If contentId is not found, try finding the element with row and column attributes
                    if (!element) {
                        element = document.querySelector('[data-row=""' + row + '""][data-column=""' + column + '""]');
                    }

                    // If the element is still not found, log an error and return
                    if (!element) {
                        console.error('Element not found using contentId, row, or column.');
                        return;
                    }

                    // Create the overlay
                    var overlay = document.createElement('div');
                    overlay.style.position = 'absolute';
                    overlay.style.top = element.offsetTop + 'px'; // Position the overlay relative to the element
                    overlay.style.left = '0px';
                    overlay.style.width = '100%';
                    overlay.style.height = element.offsetHeight + 'px';
                    overlay.style.backgroundColor = color; // Set the background color passed as an argument
                    overlay.style.opacity = '0.3'; // Semi-transparent overlay
                    overlay.style.zIndex = '8500'; // Ensure overlay appears above other elements
                    overlay.style.pointerEvents = 'none'; // Prevent the overlay from interfering with mouse events
                    overlay.style.borderTop = '2px dotted #ccc'; // Top dotted border
                    overlay.style.borderBottom = '2px dotted #ccc'; // Bottom dotted border            

                    document.body.appendChild(overlay);

                    // Cleanup the overlay when the mouse leaves
                    element.addEventListener('mouseleave', function() {
                        document.body.removeChild(overlay);
                    });
                };
            ");
        }




        private async Task GetCellsRow(LayoutCell cell) 
        {
            await InitializeMouseEnterOverlay();  // Ensure the JS function is initialized
            await JSRuntime.InvokeVoidAsync("setupMouseEnterOverlay", cell.ContentId,cell.Row,cell.Column, "red");
            RestoreScrollPosition(false);
            if (cell == null)
            {
                hoveredRowDelete = 0;
                Console.WriteLine("Hovered cell is null.");
                return;
            }

            if(cell.Row == hoveredRowDelete)
            {
                //Console.WriteLine("Hovered cell is the same as last hovered row.");
                return;
            }
                hoveredRowDelete = cell.Row;
                Console.WriteLine($"Hovered row is {hoveredRowDelete}");
        }

        // Drag cell/content
        private async Task OnDragStart( LayoutCell layoutCell)
        {
            if (layoutCell == null)
            {
                Console.WriteLine("Cell null, operation aborted.");
                return;
            }
            if (layoutCell.ContentId != null)
            {
                draggedCell = layoutCell;

                // First, ensure that the JavaScript is initialized and ready
                await InitializeDrag();

                // Get the element to be dragged (you can use `document.querySelector` or pass the element directly)
                var elementId = layoutCell.ContentId; // Or use any identifier for the draggable element
                await JSRuntime.InvokeVoidAsync("setupDragPreview", layoutCell.ContentId);

                // Optionally, store any other information or state
                Console.WriteLine($"Started dragging: {layoutCell.ContentId}");

            }
            else
            {
                Console.WriteLine("Empty cell, operation aborted.");
            }

        }

        //private async Task OnDragStart(DragEventArgs e, LayoutCell layoutCell)
        //{
            
        //    if (layoutCell.ContentId != null)
        //    {
        //        await InitializeDrag();
        //        // Store the dragged cell
        //        draggedCell = layoutCell;
        //        // Optional: you can use DataTransfer here, but Blazor doesn't expose it directly.
        //        // If needed, we can store some information in a custom attribute or pass via JS.
        //        Console.WriteLine($"Started dragging: {layoutCell.ContentId}");
        //    }
        //    else 
        //    {
        //        Console.WriteLine($"Empty cell, operation aborted.");
        //    }
        //}

        private async Task OnDragEndAsync()
        {
            //ToDo: Add column span:
            // If the dragged cell is set, update layout
            if (draggedCell != null )
            {
                if (hoveredCell != null )
                {
                    // cellForAdjustments indexes for reverting.
                    int? draggedCellIndex;
                    int? hoveredCellIndex;

                    //Get cells indexes.
                    GetCellsIndexesForDragAndHovered(out hoveredCellIndex , out draggedCellIndex);

                    // Check if swap is legit.
                    if (draggedCellIndex.Value != hoveredCellIndex.Value)
                    {
                            Console.WriteLine("Drag ended, updating layout.");
                            
                            // Swap positions in layout
                            SwapCellsPositions(draggedCellIndex, hoveredCellIndex, draggedCell, hoveredCell);
   
                            // Save the new layout order
                            await SaveLayoutChangesAsync();
                            StateHasChanged();  // To refresh the UI

                            // Reset dragged cell after layout update
                            draggedCell = null;
                            hoveredCell = null;
                        
                    }
                    else
                    {
                        Console.WriteLine("Dragged cell is already in the target position. No swap needed.");
                    }
                }
                else
                {
                    Console.WriteLine("Found no cell for drop (null)");
                }
            }

            UserInformationMessage("Innehåll flyttat.");
        }

        private void GetCellsIndexesForDragAndHovered(out int? hoveredCellIndex, out int? draggedCellIndex)
        {
            // Find the index of the cell in LayoutCells
            draggedCellIndex = layout.LayoutCells
            .Select((cell, index) => new { cell, index })
            .FirstOrDefault(x => x.cell.Row == draggedCell.Row && x.cell.Column == draggedCell.Column)?.index;

            // Find the index of the cell in LayoutCells
            hoveredCellIndex = layout.LayoutCells
            .Select((cell, index) => new { cell, index })
            .FirstOrDefault(x => x.cell.Row == hoveredCell.Row && x.cell.Column == hoveredCell.Column)?.index;
        }

        private int? GetCellIndex(LayoutCell cell)
        {
            // Find the index of the cell in LayoutCells
            return layout.LayoutCells
            .Select((cell, index) => new { cell, index })
            .FirstOrDefault(x => x.cell.Row == cell.Row && x.cell.Column == cell.Column)?.index;

        }

        // Method to handle drag over events
        private void OnDragOver( int row, int column, int? contentId)
        {
            //ToDo: Add column span:
            // Update the hovered cell using the rowShift, column, and ContentId passed
            hoveredCell = layout.LayoutCells.FirstOrDefault(cell =>
                cell.Row == row && cell.Column == column && cell.ContentId == contentId);

            // Optionally, log or handle the hovered cell
            if (hoveredCell != null)
            {
                Console.WriteLine($"Hovered over: Row {row}, Column {column}, ContentId: {contentId}");
            }
        }



        // Method to update ColumnSpan for a cell
        private async Task UpdateColumnSpan(LayoutCell cell, int newColumnSpan)
        {
            RestoreScrollPosition();
            var targetCell = layout.LayoutCells.FirstOrDefault(c => c.ContentId == cell.ContentId);
            if (targetCell != null)
            {
                // Store old ColumnSpan to calculate affected area
                //ToDo: Use targetcell.ColumnSpan instead?
                int oldColumnSpan = targetCell.ColumnSpan;


                // Shift cells that will be pushed to the right due to the expanded column span
                ShiftCellsAfterResize(targetCell, oldColumnSpan, newColumnSpan);

                // Optionally, save the new layout order
                await SaveLayoutChangesAsync(); // Save the layout changes to persistent storage

                StateHasChanged(); // Refresh the UI
            }
            else
            {
                Console.WriteLine("Update aborted, cell id was null(empty cell)");
            }
        }

        // Overload Method to update ColumnSpan for a cell

        private async Task UpdateColumnSpan(LayoutCell cell, string newColumnSpan)
        {
            RestoreScrollPosition();
            // Check if the string can be parsed to an integer without using the output value
            if (int.TryParse(newColumnSpan, out int newColumnspanInt))
            {
                var targetCell = layout.LayoutCells.FirstOrDefault(c => c.ContentId == cell.ContentId);
                if (targetCell != null)
                {
                    // Store old ColumnSpan to calculate affected area
                    //ToDo: Use targetcell.ColumnSpan instead?
                    int oldColumnSpan = targetCell.ColumnSpan;


                    // Shift cells that will be pushed to the right due to the expanded column span
                    ShiftCellsAfterResize(targetCell, oldColumnSpan, newColumnspanInt);

                    // Optionally, save the new layout order
                    await SaveLayoutChangesAsync(); // Save the layout changes to persistent storage

                    StateHasChanged(); // Refresh the UI
                }
                else
                {
                    Console.WriteLine("Update aborted, cell id was null(empty cell)");
                }
            }
            else
            {
                Console.WriteLine("The parsing of column span to int failed.");
            }
        }

        // Method to shift cells after resizing (either rowShift span or column span)
        private void ShiftCellsAfterResize(LayoutCell targetCell, int oldColumnSpan, int newColumnSpan)
        {
            if (targetCell.ContentId == null)
            {
                newColumnSpan = 1;
            }

            if (newColumnSpan > oldColumnSpan)
            {
                IncreaseColumnSpan(targetCell, oldColumnSpan, newColumnSpan);
            }
            else if (newColumnSpan < oldColumnSpan)
            {
                
                DecreaseColumnSpan(targetCell, oldColumnSpan, newColumnSpan);
            }
        }

        private void DecreaseColumnSpan(LayoutCell targetCell, int oldColumnSpan, int newColumnSpan)
        {

            if (newColumnSpan < oldColumnSpan)
            {
                //ToDo: Evaluate if direct use of layout instead of cellsUpToAndIncludingTargetCell would save costs.
                //Get the cells up to and including hoveredCell.
                var cellsUpToAndIncludingTargetCell = layout.LayoutCells
                  .Where(cell => cell.Row < targetCell.Row || (cell.Row == targetCell.Row && cell.Column <= targetCell.Column))
                  .ToList();

                //Get the rest of the cells after hoveredCell.
                var cellsAfterTargetRow = layout.LayoutCells
                    .Where(cell => cell.Row > targetCell.Row || (cell.Row == targetCell.Row && cell.Column > targetCell.Column))
                    .ToList();

                //ToDo:evaluateassignement:
                //Assign new data for hoveredCell into list.
                foreach (var cell in cellsUpToAndIncludingTargetCell)
                {
                    if (cell.ContentId == targetCell.ContentId)
                    { 
                        cell.ColumnSpan = newColumnSpan;
                    }
                }

                //List of empty cells used to replace the void created by the reduction of span.
                List<LayoutCell> replacementCells = new();

                for (int decrease = 0; decrease <= oldColumnSpan - (newColumnSpan+1); decrease++)
                {
                    replacementCells.Add( new LayoutCell
                    {
                        Row = targetCell.Row,
                        Column = targetCell.Column + newColumnSpan + decrease,
                        ColumnSpan = 1,
                        RowSpan = 1,
                        ContentId = null
                    });
                }


                // Create a list to store the updated LayoutCells
                var updatedLayoutCells = new List<LayoutCell>();

                if (cellsUpToAndIncludingTargetCell.Count > 0)
                {
                    updatedLayoutCells.AddRange(cellsUpToAndIncludingTargetCell);
                }
                if (replacementCells.Count > 0)
                {
                    updatedLayoutCells.AddRange(replacementCells);
                }
                if (cellsAfterTargetRow.Count > 0)
                { 
                    updatedLayoutCells.AddRange(cellsAfterTargetRow);
                }

                // Rebuild the list to trigger setter notification
                layout.LayoutCells = updatedLayoutCells.Select(cell => new LayoutCell
                    {
                        Row = cell.Row,
                        Column = cell.Column,
                        ColumnSpan = cell.ColumnSpan,
                        RowSpan = cell.RowSpan,
                        ContentId = cell.ContentId
                    }).ToList();
            }
        }

        private void IncreaseColumnSpan(LayoutCell targetCell, int oldColumnSpan, int newColumnSpan)
        {
            if (newColumnSpan > oldColumnSpan)
            {
                //Variable for calculating available column space left for cell to expand to
                int availableColumnSpace = 0;

                int NumberOfolumnsLeftOfTargetCell = 0;
                bool OccupiedColumnFound = false;
                List<int?> replaceCellsByIndex = new();

                //Get rowShift data
                var targetRow = layout.LayoutCells
                  .Select(cell => cell.Row == targetCell.Row ? cell : null)
                  .ToList();

                //Count columns
                foreach (var cell in targetRow)
                {
                    if (cell != null)
                    {
                        //Stop count of available space when occupied column is found.
                        if (cell.ContentId != null && cell.ContentId != targetCell.ContentId && cell.Column >= targetCell.Column)
                        {
                            OccupiedColumnFound = true;

                        }

                        //Count available space until occupied column is found.
                        if (!OccupiedColumnFound && cell.Column >= targetCell.Column)
                        {
                            availableColumnSpace = availableColumnSpace + cell.ColumnSpan;

                            //Store cells index for removing that is not hoveredCell.
                            if (cell.ContentId != targetCell.ContentId)
                            {
                                replaceCellsByIndex.Add(targetRow.IndexOf(cell));
                            }

                        }
                        // ToDo: Remove?
                        //Count space in front of hoveredCell.
                        if (cell.Column < targetCell.Column)
                        {
                            NumberOfolumnsLeftOfTargetCell = NumberOfolumnsLeftOfTargetCell + cell.ColumnSpan;
                        }

                    }

                    // If as much avaliable column space as requested are found stop the evaluation.
                    if (availableColumnSpace == newColumnSpan)
                    {
                        break;
                    }

                }
                // Assign available column space if free space is less then requested column space.
                if (availableColumnSpace < newColumnSpan)
                {
                    targetCell.ColumnSpan = availableColumnSpace;
                }
                // Assign new column span when there is enough space for requested column span.
                else
                {
                    targetCell.ColumnSpan = newColumnSpan;
                }

                var targetCellIndex = layout.LayoutCells
                        .Select((cell, index) => new { cell, index })
                        .FirstOrDefault(x => x.cell.Row == targetCell.Row && x.cell.Column == targetCell.Column && x.cell.ContentId == targetCell.ContentId)?.index;
                layout.LayoutCells = layout.LayoutCells
                    .Select((cell, index) =>
                    {
                        // Replace the cell if its index matches the target index
                        if (index == targetCellIndex)  // hoveredCellIndex is the index of the target cell
                            return targetCell;  // Replace with the updated target cell
                        else if (replaceCellsByIndex.Contains(index))//set cells to null 
                            return null;
                        else
                            return cell;  // Otherwise, keep the original cell
                    })
                    .Where(cell => cell != null)  // Remove null cells (cells marked for removal)
                    .ToList();  // Rebuild the list to ensure setter is triggered

            }
        }
        private void SwapCellsPositions(int? draggedCellIndex, int? hoveredCellIndex, LayoutCell draggedCell, LayoutCell hoveredCell)
        {
            // ToDo: move checks in to this method.
            //Swap Content IDs and row spans.
            var storeDraggedCellContenID = draggedCell.ContentId;
            draggedCell.ContentId = hoveredCell.ContentId;
            hoveredCell.ContentId = storeDraggedCellContenID;

            // Store columnspan before changed cells inserted in layout.
            int hoveredCellColumnSpan = hoveredCell.ColumnSpan;
            int draggedCellColumnSpan = draggedCell.ColumnSpan;

            // Insert cells at swapped indexes.

                ReinsertCellInLayout(draggedCellIndex, draggedCell);
                ReinsertCellInLayout(hoveredCellIndex, hoveredCell);


            if (hoveredCell.ColumnSpan != draggedCell.ColumnSpan)
            {
                //ToDo: Evaluate ContentID null use for cells.
                // Control size and format row when needed after new size is set for a cell,
                // if contents ID is null for a cell it is important to rezize first.
                if (hoveredCell.ContentId == null)
                {
                    ShiftCellsAfterResize(hoveredCell, hoveredCellColumnSpan, draggedCellColumnSpan);
                    ShiftCellsAfterResize(draggedCell, draggedCellColumnSpan, hoveredCellColumnSpan);

                }
                else
                {
                    ShiftCellsAfterResize(draggedCell, draggedCellColumnSpan, hoveredCellColumnSpan);
                    ShiftCellsAfterResize(hoveredCell, hoveredCellColumnSpan, draggedCellColumnSpan);
                }
                // Adjust cell position if cell end up within the area of other cells cell span.
                //AdjustCellPosition(hoveredCell);
                //AdjustCellPosition(draggedCell);

            }

            //ToDo: Evaluate, is still needed?
            // Sort the layout cells by Row and Column to ensure they are in the correct grid order
            layout.LayoutCells = layout.LayoutCells
                .OrderBy(cell => cell.Row) // First, order by Row.
                .ThenBy(cell => cell.Column) // Then, order by Column.
                .ToList(); // Rebuild the list to apply sorting.

            Console.WriteLine("Layout updated: Cells swapped.");

        }

        //private void AdjustCellPosition(LayoutCell cellForAdjustments)
        //{
        //    var destinatedRow = layout.LayoutCells.Where(c => c.Row == cellForAdjustments.Row);
        //    int? availableColumn = null;

        //    //Count columns until free column is found.
        //    foreach (var cell in destinatedRow)
        //    {
        //        if (cell != null)
        //        {
        //            // If free column is found stop the evaluation.
        //            if (cell.ContentId==null && cell.Column != cellForAdjustments.Column)
        //            {
        //                cellForAdjustments.Column = (int)availableColumn;
        //                break;
        //            }
        //            else
        //            //Count occupied space until free column is found.
        //            { 
        //                availableColumn = availableColumn + cell.ColumnSpan;
        //            }

        //        }

        //    }
        //    // Assign updated cell to layout
        //    layout.LayoutCells = layout.LayoutCells
        //        .Select((cell, ContentId) =>
        //        {
        //            if (ContentId == cellForAdjustments.ContentId.Value)
        //                return cellForAdjustments;
        //            else
        //                return cell;
        //        })
        //        .ToList(); // Rebuild the list to ensure the setter is triggered.
        //}

        private void ReinsertCellInLayout(int? cellIndex, LayoutCell insertCell)
        {
            // Reassign the LayoutCell property at new index.
            layout.LayoutCells = layout.LayoutCells
                .Select((cell, index) =>
                {
                    if (index == cellIndex.Value)
                        return insertCell;
                    else
                        return cell;
                })
                .ToList(); // Rebuild the list to ensure the setter is triggered.
        }

        // Method for start of moving layout row.
        private void OnDragStartRow(int cellRow)
        {
            RestoreScrollPosition();
            draggedRow = cellRow;
            Console.WriteLine($"Started: drag row:{cellRow}.");
        }

        // Method reading hovered row
        private void OnDragOverRow(int cellRow)
        {
            RestoreScrollPosition();
            hoveredRow = cellRow;
            Console.WriteLine($"Dragged over row:{cellRow}.");
        }

        // Method for handling en of moving layout row
        private async Task OnDragEndRowAsync(DragEventArgs e)
        {
            RestoreScrollPosition();
            if (draggedRow != null)
            {
                if (hoveredRow != null)
                {
                    if (draggedRow != hoveredRow)
                    {
                        Console.WriteLine("Drag ended.");
                        List<LayoutCell> draggedLayoutCells = layout.LayoutCells.Where(c => c.Row == draggedRow).ToList();
                        InsertRowInLayout(draggedLayoutCells, draggedRow, (int)hoveredRow, true);
                        // Save the new layout order
                        await SaveLayoutChangesAsync();
                        StateHasChanged();  // To refresh the UI
                    }
                    else
                    {
                        Console.WriteLine("Dropped same row, no updates are applied");
                    }
                }
                else 
                {
                    Console.WriteLine("No suitable row for moving selected row was found.");
                }
                
            }
            else
            {
                Console.WriteLine("No row selected for moving");
            }
            
        }

        // Method for creating a new rowShift for layout.
        private async Task CreateNewRowAsync(Content? addedContent = null)
        {
            //ToDo: optimize, add select the row should position in layout.
            // Create a new list to hold layout cells
            var newLayoutCells = new List<LayoutCell>();
            int column = 1;
            int row = 1;
            if (addedContent != null)
            {
                //If header navigation bar or footer set content for entire rowShift
                if (addedContent.TemplateId == 6 ||
                    addedContent.TemplateId == 7 ||
                    addedContent.TemplateId == 9)
                {
                    newLayoutCells.Add(new LayoutCell
                    {
                        ContentId = addedContent.ContentId, // Add the content for the first column
                        Row = row,
                        Column = column,
                        ColumnSpan = rowLength
                    });

                    column = 1; // Reset column to 1 for the next rowShift
                }
                else
                {
                    // First column in each rowShift will hold content
                    newLayoutCells.Add(new LayoutCell
                    {
                        ContentId = addedContent.ContentId, // Add the content for the first column
                        Row = row,
                        Column = column
                    });
                    // Fill the remaining 11 columns with null ContentId
                    for (int j = 1; j < rowLength; j++)  // Start from column 2 to 12
                    {
                        newLayoutCells.Add(new LayoutCell
                        {
                            ContentId = null, // Empty cell
                            Row = row,
                            Column = column + j
                        });
                    }

                }
                Console.WriteLine("Layout updated: Content and rowShift added.");
            }
            else
            {
                // Fill the remaining 11 columns with null ContentId
                for (int j = 0; j < rowLength; j++)  // Start from column 2 to 12
                {
                    newLayoutCells.Add(new LayoutCell
                    {
                        ContentId = null, // Empty cell
                        Row = row,
                        Column = column + j
                    });
                }
                Console.WriteLine("Layout updated: Empty rowShift added.");
            }

            InsertRowInLayout(newLayoutCells);
        }

        private void DeleteRowInLayout(int rowNumber)
        {

            List<LayoutCell> rowsBeforeDeletedRow = new();
            List<LayoutCell> rowsAfterDeletedRow = new();
            List<LayoutCell> newLayout = new();


            // Get existing layout excluding the old row.
            rowsBeforeDeletedRow = layout.LayoutCells.Where(c => c.Row < rowNumber).ToList();
            rowsAfterDeletedRow = layout.LayoutCells.Where(c => c.Row > rowNumber).ToList();
            newLayout = new();
          

            // Assign layout list order by index.
            // If there is any rows with lower row number
            if (rowsBeforeDeletedRow.Count > 0)
            {
                // Assign the layouts first row/s.
                newLayout.AddRange(rowsBeforeDeletedRow);
            }

            // If there is any rows with higher row number
            if (rowsAfterDeletedRow.Count > 0)
            {
                // Assign the layouts last row/s with shifted row numbers.
                newLayout.AddRange(rowsAfterDeletedRow);
            }

            // Variable for asigning rows after order of rows are rearanged.
            int newRowNumber = 1;
            // Variable for keeping track of cells in a row and when to update next row. Starts at first row in newLayout.
            int countRowLength = 0;
            // Index order is right, now format row numbers for layout list.
            foreach (var cell in newLayout)
            {
                cell.Row = newRowNumber;
                countRowLength = countRowLength + cell.ColumnSpan;

                // When a row is done.
                if (countRowLength == rowLength)
                {
                    // Reset the counter.
                    countRowLength = 0;
                    // Update rownumber for assigning to cells.
                    newRowNumber++;
                }
            }

            // Rebuild the list to trigger setter notification
            layout.LayoutCells = newLayout.Select(cell => new LayoutCell
            {
                Row = cell.Row,
                Column = cell.Column,
                ColumnSpan = cell.ColumnSpan,
                RowSpan = cell.RowSpan,
                ContentId = cell.ContentId
            }).ToList();

        }

        private void  InsertRowInLayout(List<LayoutCell> layoutRow, int? oldRownumber=null, int rowNumber = 1, bool MoveExistingRow = false)
        {

            List<LayoutCell> rowsBeforeMovedRow = new();
            List<LayoutCell> rowsAfterMovedRow = new();
            List<LayoutCell> newLayout = new();

            // When new row is added get all rows except the old position.
            if (MoveExistingRow) 
            {
                // Get existing layout excluding the old row.
                 rowsBeforeMovedRow = layout.LayoutCells.Where(c => c.Row < rowNumber && c.Row != oldRownumber).ToList();
                 rowsAfterMovedRow = layout.LayoutCells.Where(c => c.Row >= rowNumber && c.Row != oldRownumber).ToList();
                 newLayout = new();
            }
            // When new row is added extract all content.
            else 
            {
                // Get existing layout.
                 rowsBeforeMovedRow = layout.LayoutCells.Where(c => c.Row < rowNumber).ToList();
                 rowsAfterMovedRow = layout.LayoutCells.Where(c => c.Row >= rowNumber).ToList();
                 newLayout = new();
            }

            //Assign layout list order by index.

            // Assign the layouts first row/s.
            if (rowsBeforeMovedRow.Count > 0)
            {
                newLayout.AddRange(rowsBeforeMovedRow);
            }

            // Assign the new row/moved row
            if (layoutRow.Count > 0)
            {
                newLayout.AddRange(layoutRow);
            }

            // Assign the layouts last rows with shifted row numbers.
            if (rowsAfterMovedRow.Count > 0)
            {
                newLayout.AddRange(rowsAfterMovedRow);
            }

            // Variable for asigning rows after order of rows are rearanged.
            int newRowNumber = 1;
            // Variable for keeping track of cells in a row and when to update next row. Starts at first row in newLayout.
            int countRowLength = 0;
            // Index order is right, now format row numbers for layout list.
            foreach (var cell in newLayout)
            {
                cell.Row = newRowNumber;
                countRowLength = countRowLength + cell.ColumnSpan;
                
                // When a row is done.
                if (countRowLength == rowLength)
                {
                    // Reset the counter.
                    countRowLength = 0;
                    // Update rownumber for assigning to cells.
                    newRowNumber++;
                }
            }

            // Rebuild the list to trigger setter notification
            layout.LayoutCells = newLayout.Select(cell => new LayoutCell
            {
                Row = cell.Row,
                Column = cell.Column,
                ColumnSpan = cell.ColumnSpan,
                RowSpan = cell.RowSpan,
                ContentId = cell.ContentId
            }).ToList();

        }

        // Method to save layout changes
        private async Task SaveLayoutChangesAsync()
        {
            // Update LayoutCells in the database
            var layoutSave = await LayoutService.GetLayoutAsync(WebPageId.Value);
            if (layoutSave != null)
            {
                layoutSave.UserId = userId;
                layoutSave.LastUpdated = DateOnly.FromDateTime(DateTime.Now);
                layoutSave.LayoutCellsSerialized = JsonConvert.SerializeObject(layout.LayoutCells);
                await LayoutService.UpdateLayoutAsync(layoutSave);
                UserInformationMessage("Ändringarna Sparades.");
            }
            else
            {
                layoutSave = new WebPageLayout()
                {
                    CreationDate = DateOnly.FromDateTime(DateTime.Now),
                    LastUpdated = DateOnly.FromDateTime(DateTime.Now),
                    UserId = userId,
                    WebPageIdForLayout = WebPageId,
                    LayoutCellsSerialized = JsonConvert.SerializeObject(layout.LayoutCells)
                };
                await LayoutService.SaveLayoutAsync(layoutSave);
                UserInformationMessage("Ny layout skapad.");
            }

        }
    }
}
