namespace CMS.Models
{
    public class LayoutCell
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public int? ContentId { get; set; }
        public int ColumnSpan { get; set; } = 1; // Default to 1 column
        public int RowSpan { get; set; } = 1;    // Default to 1 row
    }
}
