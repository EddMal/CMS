using Microsoft.AspNetCore.Components;

namespace CMS.Components.BlazorComponents.HtmlTemplates
{
    public class BaseTemplateComponent : ComponentBase
    {
        [Parameter] public int TemplateId { get; set; }
        [Parameter] public int WebPageId { get; set; }
        [Parameter] public int? ContentId { get; set; }
        [Parameter] public EventCallback<Dictionary<string, object>> OnSubmit { get; set; }
    }

    public class BaseImageCardTemplate : BaseTemplateComponent
    {
        [Parameter] public string ContentName { get; set; } = string.Empty;
        [Parameter] public string ImageInput { get; set; }
        [Parameter] public string CardTitle { get; set; } = "Titel";
        [Parameter] public string CardText1 { get; set; } = "Underrubrik 1";
        [Parameter] public string CardText2 { get; set; } = "Underrubrik 2";
        [Parameter] public string BackgroundColor { get; set; } = "white";
        [Parameter] public string TextColor { get; set; } = "black";
    }
    public class BaseImageTemplate : BaseTemplateComponent
    {
        [Parameter] public string ContentName { get; set; } = string.Empty;
        [Parameter] public string ImageInput { get; set; }
        [Parameter] public string ImageWidth { get; set; }
        [Parameter] public string ImageHeight { get; set; }
        [Parameter] public decimal BorderWidth { get; set; }
        [Parameter] public string BorderColor { get; set; }
        [Parameter] public decimal BorderRadius { get; set; }
        [Parameter] public string BoxShadow { get; set; }
        [Parameter] public string BackgroundColor { get; set; }
        [Parameter] public string ImageAlignment { get; set; }
    }
    public class BaseTextTemplateMarkdown : BaseTemplateComponent
    {
        [Parameter] public string ContentName { get; set; } = string.Empty;
        [Parameter] public string MarkdownContent { get; set; } = string.Empty;
    }
    public class BaseTextTemplateQuill : BaseTemplateComponent
    {
        [Parameter] public string ContentName { get; set; } = string.Empty;
        [Parameter] public string QuillContent { get; set; } = string.Empty;
    }

    public class BaseVideoTemplate : BaseTemplateComponent
    {
        [Parameter] public string ContentName { get; set; } = string.Empty;
        [Parameter] public string VideoUrl { get; set; } = string.Empty;
        [Parameter] public string VideoWidth { get; set; } = "100%";  // Use meaningful default values
        [Parameter] public string VideoHeight { get; set; } = "315px"; // You can adjust height accordingly
        [Parameter] public string VideoAlignment { get; set; } = "center";
    }

    public class BaseLinkTemplate : BaseTemplateComponent
    {
        [Parameter] public string ContentName { get; set; } = string.Empty;
        [Parameter] public string TextInput1 { get; set; } = "Text";
        [Parameter] public string TextInput2 { get; set; } = "URL";
        [Parameter] public string Backgroundcolor { get; set; } = "white";
        [Parameter] public string TextColor { get; set; } = "blue";
    }

    public class BaseFooterTemplate : BaseTemplateComponent
    {
        [Parameter] public string ContentName { get; set; } = string.Empty;
        [Parameter] public string TextInput1 { get; set; } = "Rubrik";
        [Parameter] public string TextInput2 { get; set; } = "Underrubrik";
        [Parameter] public string BackgroundColor { get; set; } = "white";
        [Parameter] public string TextColor { get; set; } = "black";
    }
    public class BaseHeaderTemplate : BaseTemplateComponent
    {
        [Parameter] public string ContentName { get; set; } = string.Empty;
        [Parameter] public string TextInput1 { get; set; } = "Rubrik";
        [Parameter] public string TextInput2 { get; set; } = "Underrubrik";
        [Parameter] public string BackgroundColor { get; set; } = "white";
        [Parameter] public string TextColor { get; set; } = "black";
    }
    public class BaseNavBarTemplate : BaseTemplateComponent
    {
        [Parameter] public string ContentName { get; set; } = string.Empty;
        [Parameter] public Dictionary<string, string> MenuItems { get; set; } = new Dictionary<string, string>() { { "Innehåll saknas", "Länk Saknas" } };
        [Parameter] public string BackgroundColor { get; set; } = "white";
        [Parameter] public string TextColor { get; set; } = "black";
    }

}
