public class ColorSettingsViewModel
{
    public string PrimaryColor { get; set; } = "#0d6efd";       // Default Bootstrap primary blue
    public string SecondaryColor { get; set; } = "#6c757d";     // Default Bootstrap secondary gray
    public string Tertiary1Color { get; set; } = "#198754";     // Default success green
    public string Tertiary2Color { get; set; } = "#ffc107";     // Default warning orange
    public string Tertiary3Color { get; set; } = "#0dcaf0";    // Default info blue

    // CSS variables for inline styles
    public string PrimaryColorCss => $"--primary-color: {PrimaryColor};";
    public string SecondaryColorCss => $"--secondary-color: {SecondaryColor};";
    public string Tertiary1ColorCss => $"--tertiary1-color: {Tertiary1Color};";
    public string Tertiary2ColorCss => $"--tertiary2-color: {Tertiary2Color};";
    public string Tertiary3ColorCss => $"--tertiary3-color: {Tertiary3Color};";
}