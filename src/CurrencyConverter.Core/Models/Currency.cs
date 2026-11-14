namespace CurrencyConverter.Models;

/// <summary>
/// A currency the user can pick, e.g. Code = "MXN", Name = "Mexican Peso".
/// A record gives us value equality for free, which matters for ComboBox.SelectedItem.
/// </summary>
public sealed record Currency(string Code, string Name)
{
    /// <summary>Text shown in the UI: "MXN - Mexican Peso".</summary>
    public string DisplayName => $"{Code} - {Name}";

    public override string ToString() => DisplayName;
}
