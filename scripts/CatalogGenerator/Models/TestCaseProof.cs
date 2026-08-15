namespace CatalogGenerator.Models
{
    public record TestCaseProof(
        string Suite,
        string FilePath,
        int LineNumber,
        string TestName,
        string? Details = null
    );
}
