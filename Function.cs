using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Microsoft.AnalysisServices.AdomdClient;
using System.Text.Json;
using System.IO;

namespace Celesta.Bi.Pbi.XmlaProxy;

public class Function : IHttpFunction
{
    private static readonly string[] AuthScopes = ["https://analysis.windows.net/powerbi/api/.default"];

    /// <summary>
    /// Logic for your function goes here.
    /// </summary>
    /// <param name="context">The HTTP context, containing the request and the response.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(HttpContext context)
    {

        // The response will be in JSON format, always.
        context.Response.ContentType = "application/json";

        // Get the necessary parameters from request headers.
        // If any of these are missing, return a 400 Bad Request response
        // Expected headers:
        // - x-pbi-tenant-id : The Azure tenant ID, can be found in the Azure portal under Microsoft Entra Id > Overview > Tenant ID
        // - x-pbi-client-id : The client ID of the Azure AD app, can be found/created in the Azure portal under App registrations > bi-ci-powerbi-xmla-client > Overview > Application (client) ID
        // - x-pbi-client-secret : The client secret of the Azure AD app, can be found/created in the Azure portal under App registrations > bi-ci-powerbi-xmla-client > Certificates & secrets
        // - x-pbi-xmla-endpoint : The XMLA endpoint URL, can be found in the PowerBI portal under Workspace settings > License info > Connection link
        // - x-pbi-dataset-name : The name of the semaintic model to query
        if (!context.Request.Headers.TryGetValue("x-pbi-tenant-id", out var tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errorResponse = new
            {
                error = "Invalid header",
                detail = "x-pbi-tenant-id header is required"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            return;
        }

        if (!context.Request.Headers.TryGetValue("x-pbi-client-id", out var clientId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errorResponse = new
            {
                error = "Invalid header",
                detail = "x-pbi-client-id header is required"
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            return;
        }

        if (!context.Request.Headers.TryGetValue("x-pbi-client-secret", out var clientSecret))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errorResponse = new
            {
                error = "Invalid header",
                detail = "x-pbi-client-secret header is required"
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            return;
        }

        if (!context.Request.Headers.TryGetValue("x-pbi-xmla-endpoint", out var xmlaEndpoint))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errorResponse = new
            {
                error = "Invalid header",
                detail = "x-pbi-xmla-endpoint header is required"
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            return;
        }

        if (!context.Request.Headers.TryGetValue("x-pbi-dataset-name", out var datasetName))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errorResponse = new
            {
                error = "Invalid header",
                detail = "x-pbi-dataset-name header is required"
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            return;
        }
        
        string bodyRaw;
        using (var reader = new StreamReader(context.Request.Body))
        {
            bodyRaw = await reader.ReadToEndAsync();
        }

        // Get the necessary parameters from request body.
        // The body must have a JSON object with the following properties:
        // - queries: an array of query objects with at least one "query" property containing the DAX query to execute
        // - impersonatedUserName: the user to impersonate
        // If any of these are missing, return a 400 Bad Request response
        if (string.IsNullOrWhiteSpace(bodyRaw))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errorResponse = new
            {
                error = "Invalid body",
                detail = "Request body is required"
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            return;
        }

        var body = JsonSerializer.Deserialize<Models.ExecuteQueryRequestPayload>(bodyRaw);

        if (body?.Queries == null || body.Queries.Count == 0 || string.IsNullOrWhiteSpace(body.Queries[0]?.Query))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errorResponse = new
            {
                error = "Invalid body",
                detail = "Request body must contain at least one Query"
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            return;
        }

        if (string.IsNullOrWhiteSpace(body?.ImpersonatedUserName))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errorResponse = new
            {
                error = "Invalid body",
                detail = "Request body must contain ImpersonatedUserName"
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            return;
        }

        try
        {

            // Create connection string (without token in connection string)
            string connectionString = $"Data Source={xmlaEndpoint};User ID=app:{clientId}@{tenantId};Password={clientSecret};Catalog={datasetName};EffectiveUserName={body.ImpersonatedUserName};";

            // Open ADOMD connection
            using AdomdConnection connection = new(connectionString);
            connection.Open();




            // Step 4: Execute DAX Query
            string query = "EVALUATE DISTINCT(Domains[domain_id])";

            using (AdomdCommand command = new AdomdCommand(query, connection))
            {
                Console.WriteLine("Executing query...");
                using (AdomdDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine(reader[0]);  // Print first column (adjust as needed)
                    }
                }
            }

            connection.Close();
            Console.WriteLine("Connection closed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        // var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        // var token = (await credential.GetTokenAsync(new TokenRequestContext(AuthScopes))).Token;
        // var conn = new AdomdConnection($"Data Source={PowerBIServerBaseURL};Catalog={PowerBIDatasetName};EffectiveUserName={impersonatedUser}")
        // {
        //     SessionID = token
        // };
        // Console.WriteLine("------------------- Connection created -------------------");

        // try
        // {
        //     conn.Open();
        //     Console.WriteLine("------------------- Connection opened -------------------");
        //     var cmd = new AdomdCommand("EVALUATE DISTINCT('Domains Table'[Domain id])", conn);
        //     var reader = cmd.ExecuteReader();
        //     Console.WriteLine("------------------- Query executed -------------------");
        // }
        // catch (Exception ex)
        // {
        //     await context.Response.WriteAsync($"Error: {ex.Message}", context.RequestAborted);
        //     conn.Close();
        // }

        // while (reader.Read())
        // {
        //     Console.WriteLine("Read!!!!!!!");
        //     await context.Response.WriteAsync($"Hello, {reader[0]}!", context.RequestAborted);
        // }
    }
}
