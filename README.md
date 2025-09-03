# celesta-bi-pbi-xmla-proxy

A Google Cloud Function that acts as a proxy for Power BI XMLA endpoints, enabling secure execution of DAX queries against Power BI datasets with user impersonation capabilities.

## Overview

This function mimics the Power BI service endpoint `POST https://api.powerbi.com/v1.0/myorg/datasets/{datasetId}/executeQueries` by connecting to Power BI datasets through their XMLA endpoints using Azure AD authentication.

## Prerequisite
- The PowerBI workspace containing the dataset must be premium-enabled.
- The service principal must have the `Build` privilege on the queries dataset.

## Functionality

The `Function.cs` implements an HTTP-triggered Google Cloud Function that:

- **Accepts DAX Queries**: Processes POST requests containing DAX queries to execute against Power BI semantic models
- **User Impersonation**: Supports executing queries on behalf of specific users using the `EffectiveUserName` parameter
- **Azure AD Authentication**: Uses client credentials (tenant ID, client ID, client secret) for secure authentication
- **XMLA Connectivity**: Connects to Power BI datasets via their XMLA endpoints using ADOMD.NET
- **Power BI API Compatibility**: Returns responses in the same format as the official Power BI executeQueries API

## Required Headers

- `x-pbi-tenant-id`: Azure tenant ID
- `x-pbi-client-id`: Azure AD application client ID  
- `x-pbi-client-secret`: Azure AD application client secret
- `x-pbi-xmla-endpoint`: Power BI workspace XMLA endpoint URL
- `x-pbi-dataset-name`: Name of the semantic model to query

## Request Body

```json
{
  "queries": [
    {
      "query": "EVALUATE 'Table'"
    }
  ],
  "impersonatedUserName": "user@domain.com"
}
```

## Response Format

Returns JSON responses matching the Power BI executeQueries API format, with query results organized in tables and rows structure. Handles both successful query execution and error scenarios with appropriate HTTP status codes.