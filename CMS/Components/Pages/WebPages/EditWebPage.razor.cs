
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
namespace CMS.Components.Pages.WebPages
{
    //ToDO: Chanfe variables name from ec. WebSiteId to webSiteId
    public partial class EditWebPage
    {
        [Inject] private ILayoutService LayoutService { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private NavigationManager NavigationManager { get; set; } = default!;

        List<Content> Contents { get; set; } = new List<Content>();
        [SupplyParameterFromQuery]
        public int? WebPageId { get; set; }
        private int? WebSiteId { get; set; }

        public string webPageBackgroundColor { get; set; } = "white";
        private int? ContentForEditing { get; set; } = null;

        public int ContentId { get; set; }


        public Content? Content { get; set; }

        ApplicationDbContext context = default!;

        private bool HideToolbar = false;

        private LayoutCell? draggedCell { get; set; } = null;

        private LayoutCell? hoveredCell { get; set; } = null;
        public WebPageLayout? layout { get; set; } = new WebPageLayout();

        public LayoutCell? layoutCell { get; set; } = null;

        private string userId { get; set; } = string.Empty;



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

                var CurrentWebPageLayout = await LayoutService.GetLayoutAsync(WebPageId.Value);
                // Initial population of layout cells and content
                GetLayout(CurrentWebPageLayout);
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

        private void GetLayout(WebPageLayout webPageLayout)
        {
            // If layout or LayoutCells is null or empty, generate new layout using Contents
            if (webPageLayout == null || webPageLayout.LayoutCells == null || !webPageLayout.LayoutCells.Any())
            {
                // Create a new list to hold layout cells
                var newLayoutCells = new List<LayoutCell>();

                int cellsPerRow = 12; // Number of cells per row
                int totalContents = Contents.Count;

                int contentIndex = 0;
                int row = 1;
                int column = 1;

                // Loop through each content and create a row for each one
                for (int i = 0; i < totalContents; i++)
                {
                    //If header navigation bar or footer set content for entire row
                    if(Contents[contentIndex].TemplateId == 6 ||
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
                            // Move to the next row
                            row++;
                            column = 1; // Reset column to 1 for the next row
                    }
                    else
                    { 
                    // First column in each row will hold content
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

                        // Move to the next row
                        row++;
                        column = 1; // Reset column to 1 for the next row
                    }

                }

                // Reassign the new list to layout.LayoutCells to trigger a state change
                layout.LayoutCells = newLayoutCells;
            }
            else
            {
                // If layout already has cells, set the layout's LayoutCells to the provided layout
                layout.LayoutCells = webPageLayout.LayoutCells.ToList(); // Make sure to use a new list to trigger the state change
            }

            // Trigger UI refresh to reflect changes
            StateHasChanged();
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

        //DRAG and drop content order

        // Event handler for drag start
        private void OnDragStart(DragEventArgs e, LayoutCell layoutCell)
        {
            // Store the dragged cell
            draggedCell = layoutCell;
            // Optional: you can use DataTransfer here, but Blazor doesn't expose it directly.
            // If needed, we can store some information in a custom attribute or pass via JS.
            Console.WriteLine($"Started dragging: {layoutCell.ContentId}");
        }

        // Event handler for drag end VERSION 1: works along vid version 1 in EditWebpage.razor.
        //private async Task OnDragEndAsync(DragEventArgs e)
        //{
        //    // If the dragged cell is set, update layout
        //    if (draggedCell != null)
        //    {
        //        // Logic to update layout after drag ends
        //        Console.WriteLine("Drag ended, updating layout.");

        //        // Find the index of the dragged cell in LayoutCells
        //        var draggedCellIndex = layout.LayoutCells
        //            .Select((cell, index) => new { cell, index })
        //            .FirstOrDefault(x => x.cell.ContentId == draggedCell.ContentId)?.index;

        //        // Find the index of the target cell in LayoutCells
        //        var targetCellIndex = layout.LayoutCells
        //            .Select((cell, index) => new { cell, index })
        //            .FirstOrDefault(x => x.cell.ContentId == hoveredCell.ContentId)?.index;

        //        if (draggedCellIndex.HasValue && targetCellIndex.HasValue)
        //        {
        //            // Check if both indices are valid and different
        //            if (draggedCellIndex.Value != targetCellIndex.Value)
        //            {
        //                // Swap the cells by index
        //                var tempCell = layout.LayoutCells[draggedCellIndex.Value];
        //                var tempCellhover = layout.LayoutCells[targetCellIndex.Value];

        //                // Now, reassign the LayoutCells property to trigger the setter
        //                layout.LayoutCells = layout.LayoutCells
        //                    .Select((cell, index) =>
        //                    {
        //                        if (index == draggedCellIndex.Value)
        //                            return tempCellhover;
        //                        else if (index == targetCellIndex.Value)
        //                            return tempCell;
        //                        else
        //                            return cell;
        //                    })
        //                    .ToList(); // Rebuild the list to ensure the setter is triggered

        //                    //// Sort the layout cells by Row and Column to ensure they are in the correct grid order
        //                    //layout.LayoutCells = layout.LayoutCells
        //                    //    .OrderBy(cell => cell.Row) // First, order by Row
        //                    //    .ThenBy(cell => cell.Column) // Then, order by Column
        //                    //    .ToList(); // Rebuild the list to apply sorting

        //                // Optionally save the new layout order
        //                await SaveLayoutChanges();
        //                //await SaveScrollAndReloadPage();
        //               StateHasChanged();  // To refresh the UI

        //                Console.WriteLine("Layout updated: Cells swapped.");
        //            }
        //            else
        //            {
        //                Console.WriteLine("Dragged cell is already in the target position. No swap needed.");
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine("No valid target cell to swap with.");
        //        }

        //        // Reset dragged cell after layout update
        //        draggedCell = null;
        //    }

        //    Console.WriteLine("Drag ended.");
        //}

        // Event handler for drag end VERSION 2: works along vid version 2 in EditWebpage.razor.

        //Version 2
        //private async Task OnDragEndAsync(DragEventArgs e)
        //{
        //    // If the dragged cell is set, update layout
        //    if (draggedCell != null)
        //    {
        //        // Logic to update layout after drag ends
        //        Console.WriteLine("Drag ended, updating layout.");

        //        // Find the index of the dragged cell in LayoutCells
        //        var draggedCellIndex = layout.LayoutCells
        //            .Select((cell, index) => new { cell, index })
        //            .FirstOrDefault(x => x.cell.Row == draggedCell.Row && x.cell.Column == draggedCell.Column)?.index;

        //        // Find the index of the target cell in LayoutCells
        //        var targetCellIndex = layout.LayoutCells
        //            .Select((cell, index) => new { cell, index })
        //            .FirstOrDefault(x => x.cell.Row == hoveredCell.Row && x.cell.Column == hoveredCell.Column)?.index;

        //        if (draggedCellIndex.HasValue && targetCellIndex.HasValue)
        //        {
        //            // Check if both indices are valid and different
        //            if (draggedCellIndex.Value != targetCellIndex.Value)
        //            {
        //                // Swap the cells by index
        //                var tempCell = layout.LayoutCells[draggedCellIndex.Value];
        //                var tempCellhover = layout.LayoutCells[targetCellIndex.Value];
        //                //ToDo: Resolve in a better way,
        //                //Restore ID, temp. fix
        //                tempCell.ContentId = hoveredCell.ContentId;
        //                tempCellhover.ContentId = draggedCell.ContentId;
        //                // End Restore ID , temp. fix
        //                // Now, reassign the LayoutCells property to trigger the setter
        //                layout.LayoutCells = layout.LayoutCells
        //                    .Select((cell, index) =>
        //                    {
        //                        if (index == draggedCellIndex.Value)
        //                            return tempCell;
        //                        else if (index == targetCellIndex.Value)
        //                            return tempCellhover;
        //                        else
        //                            return cell;
        //                    })
        //                    .ToList(); // Rebuild the list to ensure the setter is triggered

        //                //// Sort the layout cells by Row and Column to ensure they are in the correct grid order
        //                //layout.LayoutCells = layout.LayoutCells
        //                //    .OrderBy(cell => cell.Row) // First, order by Row
        //                //    .ThenBy(cell => cell.Column) // Then, order by Column
        //                //    .ToList(); // Rebuild the list to apply sorting

        //                // Optionally save the new layout order
        //                await SaveLayoutChanges();
        //                //await SaveScrollAndReloadPage();
        //                StateHasChanged();  // To refresh the UI

        //                Console.WriteLine("Layout updated: Cells swapped.");
        //            }
        //            else
        //            {
        //                Console.WriteLine("Dragged cell is already in the target position. No swap needed.");
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine("No valid target cell to swap with.");
        //        }

        //        // Reset dragged cell after layout update
        //        draggedCell = null;
        //        hoveredCell = null;
        //    }

        //    Console.WriteLine("Drag ended.");
        //}

        private async Task OnDragEndAsync(DragEventArgs e)
        {
            //ToDo: Add column span:
            // If the dragged cell is set, update layout
            if (draggedCell != null )
            {
                if (hoveredCell != null )
                {
                    // Logic to update layout after drag ends
                    Console.WriteLine("Drag ended, updating layout.");

                    // Find the index of the dragged cell in LayoutCells
                    var draggedCellIndex = layout.LayoutCells
                        .Select((cell, index) => new { cell, index })
                        .FirstOrDefault(x => x.cell.Row == draggedCell.Row && x.cell.Column == draggedCell.Column)?.index;


                    // Find the index of the target cell in LayoutCells
                    var targetCellIndex = layout.LayoutCells
                        .Select((cell, index) => new { cell, index })
                        .FirstOrDefault(x => x.cell.Row == hoveredCell.Row && x.cell.Column == hoveredCell.Column)?.index;

                    if (draggedCellIndex.HasValue && targetCellIndex.HasValue)
                    {
                        // Check if both indices are valid and different
                        if (draggedCellIndex.Value != targetCellIndex.Value)
                        {
                            // Swap the cells by index
                            var tempCell = layout.LayoutCells[draggedCellIndex.Value];
                            var tempCellhover = layout.LayoutCells[targetCellIndex.Value];
                        
                            //ToDo: Resolve in a better way
                            //Restore ID and rowspan, temp. fix
                            tempCell.ContentId = hoveredCell.ContentId;
                            tempCellhover.ContentId = draggedCell.ContentId;
                            tempCell.ColumnSpan = hoveredCell.ColumnSpan;
                            tempCellhover.ColumnSpan = draggedCell.ColumnSpan;
                            // End Restore ID and rowspan, temp. fix

                            // Now, reassign the LayoutCells property to trigger the setter
                            layout.LayoutCells = layout.LayoutCells
                                .Select((cell, index) =>
                                {
                                    if (index == draggedCellIndex.Value)
                                        return tempCell;
                                    else if (index == targetCellIndex.Value)
                                        return tempCellhover;
                                    else
                                        return cell;
                                })
                                .ToList(); // Rebuild the list to ensure the setter is triggered

                            //// Sort the layout cells by Row and Column to ensure they are in the correct grid order
                            //layout.LayoutCells = layout.LayoutCells
                            //    .OrderBy(cell => cell.Row) // First, order by Row
                            //    .ThenBy(cell => cell.Column) // Then, order by Column
                            //    .ToList(); // Rebuild the list to apply sorting

                            // Optionally save the new layout order
                            await SaveLayoutChanges();
                            //await SaveScrollAndReloadPage();
                            StateHasChanged();  // To refresh the UI

                            Console.WriteLine("Layout updated: Cells swapped.");
                        }
                        else
                        {
                            Console.WriteLine("Dragged cell is already in the target position. No swap needed.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No valid target cell to swap with.");
                    }

                    // Reset dragged cell after layout update
                    draggedCell = null;
                    hoveredCell = null;
                }
            }

            Console.WriteLine("Drag ended.");
        }

        // Method to handle drag over events
        private void OnDragOver(DragEventArgs e, int row, int column, int? contentId)
        {
            //ToDo: Add column span:
            // Update the hovered cell using the row, column, and ContentId passed
            hoveredCell = layout.LayoutCells.FirstOrDefault(cell =>
                cell.Row == row && cell.Column == column && cell.ContentId == contentId);

            // Optionally, log or handle the hovered cell
            if (hoveredCell != null)
            {
                Console.WriteLine($"Hovered over: Row {row}, Column {column}, ContentId: {contentId}");
            }


        }

        // Method to update RowSpan for a specific cell
        private async Task UpdateRowSpan(LayoutCell cell, int newRowSpan)
        {
            var targetCell = layout.LayoutCells.FirstOrDefault(c => c.ContentId == cell.ContentId);
            if (targetCell != null)
            {
                // Store old RowSpan to calculate affected area
                int oldRowSpan = targetCell.RowSpan;
                int oldColumnSpan = targetCell.ColumnSpan; // Store old ColumnSpan as well

                // Update the RowSpan
                //targetCell.RowSpan = newRowSpan;

                // Shift cells that will be pushed down by the expanded row span
                ShiftCellsAfterResize(targetCell, oldRowSpan, newRowSpan, oldColumnSpan, targetCell.ColumnSpan);

                // Re-sort layout to maintain grid order (based on row and column)
                layout.LayoutCells = layout.LayoutCells
                    .OrderBy(c => c.Row)
                    .ThenBy(c => c.Column)
                    .ToList();

                // Optionally, save the new layout order
                await SaveLayoutChanges(); // Save the layout changes to persistent storage

                StateHasChanged(); // Refresh the UI
            }
        }

        // Method to update ColumnSpan for a specific cell
        private async Task UpdateColumnSpan(LayoutCell cell, int newColumnSpan)
        {
            var targetCell = layout.LayoutCells.FirstOrDefault(c => c.ContentId == cell.ContentId);
            if (targetCell != null)
            {
                // Store old ColumnSpan to calculate affected area
                int oldColumnSpan = targetCell.ColumnSpan;
                int oldRowSpan = targetCell.RowSpan; // Store old RowSpan as well

                // Update the ColumnSpan
                //targetCell.ColumnSpan = newColumnSpan;

                // Shift cells that will be pushed to the right due to the expanded column span
                ShiftCellsAfterResize(targetCell, oldRowSpan, targetCell.RowSpan, oldColumnSpan, newColumnSpan);

                // Re-sort layout to maintain grid order (based on row and column)
                //layout.LayoutCells = layout.LayoutCells
                //    .OrderBy(c => c.Row)
                //    .ThenBy(c => c.Column)
                //    .ToList();

                // Optionally, save the new layout order
                await SaveLayoutChanges(); // Save the layout changes to persistent storage

                StateHasChanged(); // Refresh the UI
            }
        }

        // Method to shift cells after resizing (either row span or column span)
        private void ShiftCellsAfterResize(LayoutCell targetCell, int oldRowSpan, int newRowSpan, int oldColumnSpan, int newColumnSpan)
        {
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
                //Get the cells up to and including targetCell.
                var cellsUpToAndIncludingTargetCell = layout.LayoutCells
                  .Where(cell => cell.Row < targetCell.Row || (cell.Row == targetCell.Row && cell.Column <= targetCell.Column))
                  .ToList();

                //Get the rest of the cells after targetCell.
                var cellsAfterTargetRow = layout.LayoutCells
                    .Where(cell => cell.Row > targetCell.Row || (cell.Row == targetCell.Row && cell.Column > targetCell.Column))
                    .ToList();

                //Assign new columnSpan to targetCell.
                foreach (var cell in cellsUpToAndIncludingTargetCell)
                {
                    if (cell.ContentId == targetCell.ContentId)
                        cell.ColumnSpan = newColumnSpan;
                }

                //Assign new columnSpan to targetCell.
                targetCell.ColumnSpan = newColumnSpan;


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

                //Get row data
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

                            //Store cells index for removing that is not targetCell.
                            if (cell.ContentId != targetCell.ContentId)
                            {
                                replaceCellsByIndex.Add(targetRow.IndexOf(cell));
                            }

                        }

                        //Count space in front of targetCell.
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
                        if (index == targetCellIndex)  // targetCellIndex is the index of the target cell
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


        // Method to save layout changes
        private async Task SaveLayoutChanges()
        {
            // Update LayoutCells in the database
            var layoutSave = await LayoutService.GetLayoutAsync(WebPageId.Value);
            if (layoutSave != null)
            {
                layoutSave.UserId = userId;
                layoutSave.LastUpdated = DateOnly.FromDateTime(DateTime.Now);
                layoutSave.LayoutCellsSerialized = JsonConvert.SerializeObject(layout.LayoutCells);
                await LayoutService.UpdateLayoutAsync(layoutSave);
                Console.WriteLine("Layout updates saved!");
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
                Console.WriteLine("New layout saved!");
            }

        }
    }

}
