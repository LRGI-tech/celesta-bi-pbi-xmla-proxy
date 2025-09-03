using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Celesta.Bi.Pbi.XmlaProxy.Models;

public sealed class ExecuteQueryRequestPayload
{
    [Required, MinLength(1)]
    public List<QueryItem> Queries { get; init; }

    [Required, EmailAddress]
    public string ImpersonatedUserName { get; init; }

}

public sealed class QueryItem
{
    [Required, MinLength(1)]
    public string Query { get; init; }
}
