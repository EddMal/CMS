using CMS.Entities;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using CMS.Models;

public class WebPageLayout
{
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int? WebPageIdForLayout { get; set; } = null;
        public DateOnly CreationDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public DateOnly? LastUpdated { get; set; } = null;

        // Serialized column (storing layout cells as JSON or serialized string)
        public string LayoutCellsSerialized { get; set; } = string.Empty;

        // Navigation property
        public WebPage? WebPage { get; set; } = null;

        // NotMapped property for the LayoutCells (combines row, column, and contentId)
        [NotMapped]
        public List<LayoutCell> LayoutCells
        {
            get => JsonConvert.DeserializeObject<List<LayoutCell>>(LayoutCellsSerialized) ?? new List<LayoutCell>();
            set => LayoutCellsSerialized = JsonConvert.SerializeObject(value);
        }

        // Method to serialize the LayoutCells directly, which we can use in the database
        public void AddLayoutCell(int row, int column, int columnSpan, int rowSpan, int? contentId)
        {
            LayoutCells ??= new List<LayoutCell>();
            LayoutCells.Add(new LayoutCell { Row = row, Column = column, ColumnSpan = columnSpan, RowSpan = rowSpan, ContentId = contentId });
        }
    }

