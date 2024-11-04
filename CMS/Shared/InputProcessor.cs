using CMS.Shared;

public class InputProcessor
{
    public static PartDetail[] SplitInput(string input,string delimiter)
    {
        var parts = SplitInputIntoParts(input, delimiter);
        var partDetails = new List<PartDetail>();

        foreach (var part in parts)
        {
            var detail = ProcessPart(part);
            if (detail != null)
            {
                partDetails.Add(detail);
            }
        }

        return partDetails.ToArray();
    }

    private static string[] SplitInputIntoParts(string input, string delimiter)
    {
        return input.Split(new string[] { delimiter }, StringSplitOptions.None);
    }

    private static PartDetail ProcessPart(string part)
    {
        string linkDelimiter = "=";
        string[] partAndUrl = part.Split(new string[] { linkDelimiter }, StringSplitOptions.None);

        if (partAndUrl.Length >= 2 && !string.IsNullOrEmpty(partAndUrl[0]) && !string.IsNullOrEmpty(partAndUrl[1]))
        {
            // It's a link
            return new PartDetail
            {
                IsLink = true,
                DisplayText = partAndUrl[0],
                Url = partAndUrl[1]
            };
        }
        else if (partAndUrl.Length >= 1 && !string.IsNullOrEmpty(partAndUrl[0]))
        {
            // It's plain text
            return new PartDetail
            {
                IsLink = false,
                DisplayText = partAndUrl[0],
                Url = null
            };
        }

        // Return null if no valid part was found
        return null;
    }
}
