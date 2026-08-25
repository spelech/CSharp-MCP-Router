namespace CatalogGenerator.Models
{
    public class CatalogIndex
    {
        public Dictionary<string, RequirementItem> Requirements { get; } = new();

        public void AddOrMergeProof(string id, string category, RequirementType type, string description, TestCaseProof proof)
        {
            if (!Requirements.TryGetValue(id, out var item))
            {
                item = new RequirementItem(id, category, type, description);
                Requirements[id] = item;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(item.Description) && !string.IsNullOrWhiteSpace(description))
                {
                    item.Description = description;
                }
                if (item.Type != type && type == RequirementType.Negative)
                {
                    item.Type = type;
                }
            }

            item.AddProof(proof);
        }

        public IEnumerable<RequirementItem> GetAll() => Requirements.Values.OrderBy(r => r.Category).ThenBy(r => r.Id);
    }
}
