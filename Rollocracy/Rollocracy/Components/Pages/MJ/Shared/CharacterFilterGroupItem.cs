public class CharacterFilterGroupItem
{
    public string GroupName { get; set; } = string.Empty;
    public List<CharacterFilterReferenceItem> Options { get; set; } = new();
}