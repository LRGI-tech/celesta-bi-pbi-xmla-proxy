using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Microsoft.AnalysisServices.AdomdClient;
using System.Text.Json;

namespace celesta_bi_pbi_xmla_proxy;

public class Function : IHttpFunction
{


    private static readonly string[] AuthScopes = ["https://analysis.windows.net/powerbi/api/.default"];
    private static readonly string PowerBIServerBaseURL = "powerbi://api.powerbi.com/v1.0/myorg/Celesta%20Analytics%20%5BDev%5D";

    private static readonly string PowerBIDatasetName = "[CEL-007] - Payment Report (1)";

    /// <summary>
    /// Logic for your function goes here.
    /// </summary>
    /// <param name="context">The HTTP context, containing the request and the response.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HandleAsync(HttpContext context)
    {

        // The response will be in JSON format, always.
        context.Response.ContentType = "application/json";

        // Configure JSON serialization options for the response.
        var responseJSONOptions = new JsonSerializerOptions
        {
            WriteIndented = true // Pretty print
        };

        // Get the necessary parameters from request headers.
        // If any of these are missing, return a 400 Bad Request response
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

        if (!context.Request.Headers.TryGetValue("x-pbi-impersonated-upn", out var impersonatedUser))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errorResponse = new
            {
                error = "Invalid header",
                detail = "x-pbi-impersonated-upn header is required"
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
            return;
        }

        try
        {

            // Create connection string (without token in connection string)
            string connectionString = $"Data Source={PowerBIServerBaseURL};User ID=app:{clientId}@{tenantId};Password={clientSecret};Catalog={PowerBIDatasetName};EffectiveUserName={impersonatedUser}";
            // string connectionString = $"Data Source={PowerBIServerBaseURL};Catalog={PowerBIDatasetName};EffectiveUserName={impersonatedUser}";

            // Step 3: Open ADOMD connection
            using (AdomdConnection connection = new AdomdConnection(connectionString))
            {
                // Apply the access token manually using SessionID
                //connection.SessionID = accessToken;

                Console.WriteLine("Opening connection to XMLA endpoint...");
                connection.Open();

                // var json = JsonSerializer.Serialize(connection.Properties, new JsonSerializerOptions { WriteIndented = true , ReferenceHandler = ReferenceHandler.Preserve});

                // Console.WriteLine(json);

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
