namespace CatalogGenerator.Models
{
    public class RequirementItem
    {
        public string Id { get; }
        public string Category { get; set; }
        public RequirementType Type { get; set; }
        public string Description { get; set; }
        public List<TestCaseProof> Proofs { get; } = new();

        public RequirementItem(string id, string category, RequirementType type, string description)
        {
            Id = id;
            Category = category;
            Type = type;
            Description = description;
        }

        public void AddProof(TestCaseProof proof)
        {
            Proofs.Add(proof);
        }
    }
}
