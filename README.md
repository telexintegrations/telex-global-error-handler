# Telex Global Error Handling Integration

## Overview
This integration provides a **global error-handling mechanism** for applications using the **Telex platform**. It ensures that errors are captured, formatted, and logged in your telex channel in a structured way, improving observability and debugging. To use this integration, you need to **activate it in your Telex organization** and also **install the corresponding NuGet package in your application**.

---

## 1. Setting Up Telex Integration

To begin using the global error-handling integration, follow these steps to configure it within your **Telex organization**.

### **Step 1: Activate the Integration**
1. Navigate to your **Telex Dashboard**.
2. Go to the **Integrations** section.
3. Locate **Global Error Handler** and click **Activate**.
4. Provide the necessary configuration values:
   - **Include InnerException:** `true`
   - **Include Stack Trace:** `true`
   - **Max-Length for Stack Trace** `200` or `0` for the full length.
   - **To get the full Stack Trace length, input `0` or leave it empty.
5. Save your settings.

### **Step 2: Define the Integration JSON**
Here is the deployed url for the integration json `https://global-error-handler-latest.onrender.com/api/v1/telex-global-error-handler/simulate-error`

Make a Get request to the deployed json integration to get the integrated json object.

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
    "TelexWebhook": "https://your-telex-webhook-url.com"
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
            await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred" });
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
4. Use the exposed endpoint to simulate an error and test the logging.

Here is a screenshot of the integration working in a channel

### Screenshot 1 (The raw error message which has not been formatted)
![Integration Test Screenshot 1](https://res.cloudinary.com/dlu45noef/image/upload/v1740324613/Screenshot_2025-02-22_164649_pyeekg.png)

### Screenshot 2 (The formatted error message)
![Integration Test Screenshot 2](https://res.cloudinary.com/dlu45noef/image/upload/v1740324612/Screenshot_2025-02-22_164750_aq0kte.png)

Ensure that the output matches the expected results as seen in the screenshots.

---

## **5. Deployment**  
The integration is hosted on **Render** using a **Docker image**. To ensure it works as expected, an endpoint is provided to simulate errors and verify the logging process.

# Base Url  
👉 **`https://global-error-handler-latest.onrender.com/api/v1`**  

### **Testing the Deployment**  
To test if the integration successfully catches and logs errors, you can make a get request to the error simulation endpoint:  
```sh
GET https://global-error-handler-latest.onrender.com/api/v1/telex-global-error-handler/simulate-error

```sh
curl -X GET  https://global-error-handler-latest.onrender.com/api/v1/telex-global-error-handler/simulate-error
```
This should trigger an error and log it in your Telex channel.

### **Testing the error handling process in your own code**  
To use the integration in your own code, the **NuGet package** should be included in your project dependencies and necessary configurations above should be made.

---

By following these steps, you can seamlessly integrate Telex’s global error-handling capabilities into your application. 🚀

