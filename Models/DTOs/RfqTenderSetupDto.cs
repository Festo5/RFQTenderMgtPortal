public class RfqTenderSetupDto
{
    public Guid SystemId { get; set; }
    public string PrimaryColor { get; set; } = "#0d6efd"; // Bootstrap primary blue as default
    public string SecondaryColor { get; set; } = "#6c757d"; // Bootstrap secondary gray as default
    public string Tertiary1Color { get; set; } = "#198754"; // Success green
    public string Tertiary2Color { get; set; } = "#ffc107"; // Warning orange
    public string Tertiary3Color { get; set; } = "#0d6efd"; // Primary blue
}
