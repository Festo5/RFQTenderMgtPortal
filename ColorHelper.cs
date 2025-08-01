public static class ColorHelper
{
    public static string Darken(string hexColor, int percent)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(hexColor))
        {
            return "#000000"; // Return black as fallback
        }

        // Remove # if present
        hexColor = hexColor.Trim().Replace("#", "");

        // Validate hex length
        if (hexColor.Length != 3 && hexColor.Length != 6)
        {
            return "#" + hexColor; // Return original (though invalid)
        }

        try
        {
            // Parse the color
            var color = System.Drawing.ColorTranslator.FromHtml("#" + hexColor);

            // Ensure percent is within valid range
            percent = Math.Clamp(percent, 0, 100);

            // Darken each component
            var r = (int)(color.R * (100 - percent) / 100f);
            var g = (int)(color.G * (100 - percent) / 100f);
            var b = (int)(color.B * (100 - percent) / 100f);

            // Ensure values are within byte range
            r = Math.Clamp(r, 0, 255);
            g = Math.Clamp(g, 0, 255);
            b = Math.Clamp(b, 0, 255);

            return $"#{r:X2}{g:X2}{b:X2}";
        }
        catch
        {
            return "#000000"; // Return black if parsing fails
        }
    }
}