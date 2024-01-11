# Request/Response Logging

IIS module that enables HTTP request/response logging for ASP.NET framework web applications and web services. 
No code required - all configured via the web.config file.

## Features
- HTTP request/response logging without adding any code.
- Configurable via the web.config file:
    - Enable/disable logging
    - Filter for specific requests by specifying paths to include, eg: /api/customer
    - Exclude specific requests by specifying paths to exclude eg: api/payment, .aspx
- Adds a custom response header to indicate if the request qualified for logging (assist with troubleshooting/configuring your filters)

## Getting Started

Add the RequestResponseModule.dll file to the bin directory of your ASP.NET web application or web service. 
Add config (see below). 
Test. 
Smile. 

## Add Config

Register the module with IIS via your web.config file:

    <!-- Required -->
    <system.webServer>
        <modules>
            <add name="RequestResponseModule" type="RequestResponseModule.Logger" />
        </modules>        
    </system.webServer>

Override the hosts regional settings with custom values defined in your web.config file, eg:         

    <!-- Enable/disable logging (disable when not required, you'll save some overhead) -->
    <add key="requestResponseLogger.enabled" value="true" />

    <!-- Required (wildcards are not supported) -->
    <!-- Comma separated path values to include in the logs. -->
    <!-- If the URL path contains this value, the request will be logged. -->
    <add key="requestResponseLogger.path.include" value="api/customer, api/order" />

    <!-- Comma separated path values to exclude from the logs -->
    <!-- If the URL path contains this value, the request will not be logged. -->
    <add key="requestResponseLogger.path.exclude" value="api/payment,.aspx" />


## Logging:

Log files will be created in your web application's root directory (~/Logs/RequestResponse/).
A new file is created daily and any exceptions will be logged to the Errors.log file.

**Note:** be sure to allow the web application's service account (the user context under which the web application runs - usually ASPNET or NETWORK SERVICE) sufficient privileges (Modify) to be able to create the logging directory structure and files.

The logs contain the following data in JSON format:
- Url 
- Method 
- TimeStamp (yyyy-MM-dd HH:mm:ss.fff) 
- RequestBody 
- ResponseBody 
- ProcessingTime (hh:mm:ss.fffffff) 
- HttpStatusCode 

Here is an example of a single entry in the log file: 

    {"Url":"/api/customer/card/14","Method":"GET","TimeStamp":"2024-01-09 14:32:36.815","RequestBody":"","ResponseBody":"{\"CardHolderName\":\"LT FORD 14\",\"Token\":\"FF3D60F8-6418-4729-AD06-BBB946462A8C\",\"ExpiryDate\":\"07/2025\",\"IssuedDate\":\"2023-11-10T14:32:36.8151265+02:00\"}"," ProcessingTime ":"00:00:0009941","HttpStatusCode":200}

## Additional documentation

- [Microsoft IIS Module walkthrough](https://learn.microsoft.com/en-us/iis/develop/runtime-extensibility/developing-iis-modules-and-handlers-with-the-net-framework) 
- [Capturing the HTTP request body in ASP.NET](https://stackoverflow.com/questions/1038466/logging-raw-http-request-response-in-asp-net-mvc-iis7/1792864#1792864)
- [Microsoft ANCM for enlisting ASP.NET Core web apps into the IIS processing pipeline](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/aspnet-core-module?view=aspnetcore-8.0)

## Feedback

I welcome comments, suggestions, feature requests and honest criticism :)  

 
- [Github Repo](https://github.com/thurstonford?tab=repositories)  
- Email: lance@cogware.co.za