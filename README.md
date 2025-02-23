# Telex Global Error Handling Integration

## Overview
This integration provides a **global error-handling mechanism** for ASPNET Core applications using the **Telex platform**. It ensures that errors are captured, formatted, and logged in your Telex channel in a structured way, improving observability and debugging.

---

## 1. Setting Up Telex Integration

To begin using the global error-handling integration, follow these steps to configure it within your **Telex organization**.

### **Step 1: Activate the Integration**
1. Navigate to your **Telex Organisation**.
2. Go to the **Add Integrations** section.
3. Use the deployed url of the integration json file below to add the integration to your organization.
`https://global-error-handler-latest.onrender.com/api/v1/telex-global-error-handler/integration.json`

4. Locate **Global Error Handler** and **Activate** for your channel.
5. Provide the necessary configuration settings:
   - **Include InnerException:** `true`
   - **Include Stack Trace:** `true`
   - **Max-Length for Stack Trace:** `200` or **Leave it empty** for full length.
6. Save your settings.

### **Step 2: Test the Integration**
The deployed API provides an endpoint to simulate errors:

**GET Request URL:**
```sh
https://global-error-handler-latest.onrender.com/api/v1/telex-global-error-handler/simulate-error
```
Make a GET request to trigger an error and verify that it is logged correctly.

---

## 2. Project Structure

```
/telex-global-error-handler
│── GlobalErrorHandlerIntegration/              # Main project folder 
|   ├── Helpers/ 
|   |   ├── TelexSettings.cs              # Contains an auto implemented property for the webhook url configuration.
|   |   |
│   ├── Middleware/                   # Contains the global error-handling middleware  
│   │   ├── TelexGlobalExceptionMiddleware.cs  
│   │   |
│   ├── Models/                       # Defines error models and response structures  
│   │   ├── ErrorDetails.cs  
│   │   ├── ErrorFormatPayload.cs 
|   |   ├── Setting
|   |   |
|   ├── IServices/
|   ├── ├── ITelexErrorLogger.cs       # Defined the methods responsible for sending and formatting error logs to Telex
|   |   |
│   ├── Services/                       
│   │   ├── TelexErrorLogger.cs        # Implements the methods responsible for sending and formatting error logs to Telex
│   │   |
│   ├── appsettings.json               # Application settings (Webhook, logging config, etc.)  
│   ├── Program.cs                     # Entry point for the application  
│   ├── GlobalErrorHandlerIntegration.csproj  # Project file  
│                              
│── README.md                          # Documentation  
│── .gitignore                         # Git ignore file  
│── Dockerfile                         # Deployment configuration  
│── telex-global-error-handler.sln     # Solution file  

```

## 3. Installing the Project
Since this application is still in develpment stage, it is not yet a NuGet package, you will need to manually clone the repository and integrate it into your project.

### **Step 1: Clone the Repository**
Run the following command in your terminal:

```sh
git clone https://github.com/telexintegrations/telex-global-error-handler.git
```

### **Step 2: Add the Project to Your Solution**
1. Navigate to your ASP.NET Core solution.
2. Open the `Solution Explorer` in Visual Studio.
3. Right-click on your solution and select **Add > Existing Project**.
4. Once successful, locate the `TelexGlobalErrorHandler.csproj` file inside the cloned repository and add it to your solution.

### **Step 4: Reference the Project in Your Application**
Modify your application's `.csproj` file to include a reference to the Telex Global Error Handling project:

```xml
<ItemGroup>
  <ProjectReference Include="path\to\telex-global-error-handler\GlobalErrorHandlerIntegration.csproj" />
</ItemGroup>
```

---

## 5 Configuring the Middleware

To ensure your application captures errors globally and reports them to Telex, you must configure the middleware in your **request pipeline**.

### **Step 1: Configure `appsettings.json`**
Add the following settings to your `appsettings.json` file:

```json
{
  "TelexSettings": {
    "WebhookUrl": "https://your-telex-webhook-url.com"
  }
}
```

### **Step 2: Register the Middleware**
Modify `Program.cs` (for .NET 6+ projects) to include the middleware:

```csharp
using GlobalErrorHandlerIntegration.Middlewares // import the Middleware
using GlobalErrorHandlerIntegration.IServices  // import the IServices
using GlobalErrorHandlerIntegration.Services  // import the Services
using GlobalErrorHandlerIntegration.Helpers  // import the Helper

var builder = WebApplication.CreateBuilder(args);

// Configure Telex webhook url
builder.Services.Configure<TelexSettings>(builder.Configuration.GetSection("TelexSettings"));

// Register Telex error logging services
builder.Services.AddSingleton<ITelexErrorLogger, TelexErrorLogger>();
// Register Telex Logger Service http client
builder.Services.AddHttpClient<ITelexErrorLogger, TelexErrorLogger>(); 

var app = builder.Build();

// Add Telex global error handling middleware
app.UseMiddleware<TelexGlobalExceptionMiddleware>();

app.Run();
```
---

It is important to note that you are not to interact directly with any of these classes and methods as they are already implemented and integrated into the middleware.
Your job is to ensure that you have added a reference to `GlobalErrorHandlerIntegration` in your application project to use its services.
Then configure the middleware in the request pipeline (program.cs) of your application as shown in the previous steps, and watch the Error handler do its work.

---

## 6. Testing the Integration

Once your integration is active in the telex channel and the project is correctly referenced, follow these steps to test the setup:

### **Test 1: Trigger an Error in Your Application**
Introduce an error in your application and verify that it is logged correctly.

### **Test 2: Check Your Logs**
Ensure structured error messages are recorded in your logs.

### **Test 3: Verify in Telex Dashboard**
Check your Telex dashboard to confirm errors are reported properly.

### **Test 4: Simulate an Error Using the API**
To test error handling manually without embedding it into your project, send a GET request to the error simulation endpoint:

```sh
curl -X GET https://global-error-handler-latest.onrender.com/api/v1/telex-global-error-handler/simulate-error
```
Here is a screenshot of the integration working in a channel

### Screenshot 1 (The raw error message which has not been formatted)
![Integration Test Screenshot 1](https://res.cloudinary.com/dlu45noef/image/upload/v1740324613/Screenshot_2025-02-22_164649_pyeekg.png)

### Screenshot 2 (The formatted error message)
![Integration Test Screenshot 2](https://res.cloudinary.com/dlu45noef/image/upload/v1740324612/Screenshot_2025-02-22_164750_aq0kte.png)

Ensure that your output matches the expected results as seen in the screenshots.

---

## **7. Deployment**  
The integration is hosted on **Render** using a **Docker image**. To ensure it works as expected, an endpoint is provided to simulate errors and verify the logging process.

# Base Url  
👉 **`https://global-error-handler-latest.onrender.com/api/v1`**  

### **Testing the Deployment**  
Remember that to test if the integration successfully catches and logs errors in isolation, you can make an http request to the error simulation endpoint:  
```sh
GET https://global-error-handler-latest.onrender.com/api/v1/telex-global-error-handler/simulate-error
```     

This should trigger an error and log it in your Telex channel.

---

## Conclusion
By following these steps, you can seamlessly integrate Telex’s global error-handling capabilities into your application, improving debugging and observability. 🚀


