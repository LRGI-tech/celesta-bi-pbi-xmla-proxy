using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Celesta.Bi.Pbi.XmlaProxy.Models;

public sealed class ExecuteQueryRequestPayload : IValidatableObject
{
    [Required, MinLength(1)]
    public List<QueryItem> Queries { get; init; }

    [Required, EmailAddress]
    public string ImpersonatedUserName { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext _)
    {
        if (Queries is null || Queries.Count == 0)
        {
            yield return new ValidationResult(
                "queries must contain at least one item",
                [nameof(Queries)]);
            yield break;
        }

        for (int i = 0; i < Queries.Count; i++)
        {
            if (Queries[i] is null || string.IsNullOrWhiteSpace(Queries[i].Query))
            {
                yield return new ValidationResult(
                    $"queries[{i}].query is required",
                    [$"{nameof(Queries)}[{i}].{nameof(QueryItem.Query)}"]);
            }
        }
    }

    IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
    {
        throw new System.NotImplementedException();
    }
}

public sealed class QueryItem
{
    [Required, MinLength(1)]
    public string Query { get; init; }
}
