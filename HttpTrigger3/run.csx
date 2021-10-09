#r "Newtonsoft.Json"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Microsoft.Azure.Devices;

//this class is used to invoke a direct method on the associate device


private static ServiceClient s_serviceClient;
//service connection string
private static string s_connectionString = "HostName=FinalYearHub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=1HpoEt1u1AX95BvW9qB2O3YwIXEESMcnOCVz60d7V1k=";



public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
    log.LogInformation("C# HTTP trigger function processed a request.");
    string responseMessage = string.Empty;

    s_serviceClient = ServiceClient.CreateFromConnectionString(s_connectionString);

    //http request send methodname, devicename and associated commandarguemnt
    string methodname = req.Query["MethodName"];
    string payload = req.Query["Payload"];  //this should have been serialised into json
    string devicename = req.Query["deviceName"];

    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    dynamic data = JsonConvert.DeserializeObject(requestBody);
    methodname = methodname ?? data?.name;
    payload = payload ?? data?.name;


    if (string.IsNullOrEmpty(methodname) || string.IsNullOrEmpty(payload)){
        responseMessage = "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.";
    }
    else {
    log.LogInformation(($"Sent device command; MethodName:{methodname}; payload:{payload}; deviceName:{devicename}"));
    responseMessage = await InvokeMethodAsync(methodname, devicename, JsonConvert.SerializeObject(payload), log);  //invoke method on the device and wait 
    s_serviceClient.Dispose();  //dispose client conection
    // responseMessage = responseMessageWait;
    }
 
     return new OkObjectResult(responseMessage);
}

private static async Task<string> InvokeMethodAsync(string _methodName, string _devicename, string _payload, ILogger log)
{
        
            var methodInvocation = new CloudToDeviceMethod(_methodName)
            {
                ResponseTimeout = TimeSpan.FromSeconds(30),
            };
            methodInvocation.SetPayloadJson(_payload);

            // Invoke the direct method asynchronously and get the response from the simulated device.
            var response = await s_serviceClient.InvokeDeviceMethodAsync(_devicename, methodInvocation);
            string responseMessage = $"\nResponse status: {response.Status}, payload:\n\t{response.GetPayloadAsJson()}";

            log.LogInformation(responseMessage);
            return responseMessage;
}
