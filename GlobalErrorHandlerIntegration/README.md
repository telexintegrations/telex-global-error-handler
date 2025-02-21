# Telex Global Error Handling Integration

## Overview
This integration provides a **global error-handling mechanism** for applications using the **Telex platform**. It ensures that errors are captured, formatted, and logged in a structured way, improving observability and debugging. To use this integration, you need to **activate it in your Telex organization** and also **install the corresponding NuGet package in your application**.

---

## 1. Setting Up Telex Integration

To begin using the global error-handling integration, follow these steps to configure it within your **Telex organization**.

### **Step 1: Activate the Integration**
1. Navigate to your **Telex Dashboard**.
2. Go to the **Integrations** section.
3. Locate **Global Error Handler** and click **Activate**.
4. Provide the necessary configuration values:
   - **Log Level:** `Error`
   - **Enable Stack Trace:** `true`
5. Save your settings.

### **Step 2: Define the Integration JSON**
Ensure your Telex integration JSON is correctly structured as follows:

```json
{
  "name": "Global Error Handler",
  "type": "Modifier",
  "description": "Handles and formats errors in Telex messages",
  "settings": {
    "logLevel": "Error",
    "enableStackTrace": true
  }
}
```

---

## 2. Installing the NuGet Package

To integrate Telex's global error handling within your **ASP.NET Core application**, you need to install the corresponding **NuGet package**.

### **Step 1: Install the Package**
Run the following command in your terminal or **Package Manager Console**:

```sh
Install-Package Telex.ErrorHandling
```

### **Step 2: Configure `appsettings.json`**
Add the following settings to your `appsettings.json` file:

```json
{
  "TelexSettings": {
    "TelexWebhook": "https://your-telex-webhook-endpoint.com"
  }
}
```

---

## 3. Configuring the Middleware
To ensure that your application captures errors globally and reports them to Telex, you must configure the middleware in your **request pipeline**.

### **Step 1: Register the Middleware**
Modify `Program.cs` (for .NET 6+ projects) to include the middleware:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register Telex error logging services
builder.Services.AddSingleton<ITelexErrorLogger, TelexErrorLogger>();
builder.Services.AddHttpClient<ITelexErrorLogger, TelexErrorLogger>();

var app = builder.Build();

// Add global error handling middleware
app.UseMiddleware<TelexGlobalErrorHandlingMiddleware>();

app.Run();
```

### **Step 2: Implement the Middleware**
Create a middleware class to handle errors globally:

```csharp
public class GlobalErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalErrorHandlingMiddleware> _logger;

    public GlobalErrorHandlingMiddleware(RequestDelegate next, ILogger<GlobalErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred.");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { error = "Internal Server Error" });
        }
    }
}
```

---

## 4. Testing the Integration
Once your integration is active and your NuGet package is installed, test the setup:

1. **Trigger an error** in your application and verify that it is logged correctly.
2. **Check your logs** to ensure structured error messages are being recorded.
3. **View errors in Telex Dashboard** to confirm they are reported properly.

---

## 5. Deployment
- **Telex hosts the integration**, so you don’t need to manage external hosting.
- Ensure that your **Telex webhook endpoint** is correctly configured.
- The **NuGet package** should be included in your project dependencies.

---

By following these steps, you can seamlessly integrate Telex’s global error-handling capabilities into your application. ??

