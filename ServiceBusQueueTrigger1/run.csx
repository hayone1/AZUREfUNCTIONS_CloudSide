//task to send SMS to client if message is serviceBusQueue is triggered by warning or critical message
#r "Microsoft.Azure.Cosmos.Table"
#r "Newtonsoft.Json"
#r "Twilio"
#r "Microsoft.Azure.WebJobs.Extensions.Twilio"
//#r "Microsoft.Graph"
//#r "Microsoft.Graph.Auth"
//#r "Microsoft.Identity.Client"
#r "Microsoft.IdentityModel.Clients.ActiveDirectory"


using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Graph.Core;

//This function sends alerts via SMS and gmail to client when conditions are met

// public static async Task Run(string myQueueItem, ILogger log)
// public static readonly string connString = "DefaultEndpointsProtocol=https;AccountName=csb50998223a98f;AccountKey=EDq/hstdbgTSRGREDG/sdhrydydu66sdg+XI2H2AWnzjRdAFmr+E3kb3M9lbVRAVaASGBA==;EndpointSuffix=core.windows.net";
public static readonly string connString = "use your own connection string it looks like the above comment";public static string _userPhoneNo = String.Empty;
//message levels
public const string normalMessage = "normal";
public const string warningMessage = "warning";
public const string criticalMessage = "critical";

public static readonly string myuserdetails = "myuserdetails";
//Note that the account SID and AuthToken is securely stored in my Functions Configurations/AppSettings
//using names "TwilioAccountSid" and "TwilioAuthToken" without quotes
public static CreateMessageOptions smsText = null;

//region for sending mail
private const string tenantId = "72ca12ad-1c5b-400e-a56e-de2f46920121";
private const string clientId = "0a92f7d1-8687-4623-aab3-e35ac9a6575f";
private const string clientSecret = "-JubR9p6W-.8V56v3c4_fLp3_mIkVv1Q.d";
private const string userId ="d9a76cdd-ee1f-4991-a255-54a206fcd4c3";  //also objectID
//The following scope is required to acquire the token
private static string[] scopes = new string[] { "https://graph.microsoft.com/.default" };
//end region

// public static async Task Run(string myQueueItem, IAsyncCollector<CreateMessageOptions> messageSender, ILogger log)

public static async Task Run(string myQueueItem, [TwilioSms(AccountSidSetting = "TwilioAccountSid",
                                                       AuthTokenSetting = "TwilioAuthToken",
                                                       From = "use twilio assigned number")]IAsyncCollector<CreateMessageOptions> messageSender, ILogger log)
{
    
    log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
    var e = JsonConvert.DeserializeObject<TelemetryDataPoint<object>>(myQueueItem as string);


// CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=csb50998223a98f;AccountKey=EDq/hstdbgTSRGREDG/sdhrydydu66sdg+XI2H2AWnzjRdAFmr+E3kb3M9lbVRAVaASGBA==;EndpointSuffix=core.windows.net");
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse("use your own connection string it looks like the above comment");        
        // Create the table client.
        CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

        // Retrieve a reference to the table.
        CloudTable table = tableClient.GetTableReference("FinalYearTable");

        PlayerEntity<object> entity = new PlayerEntity<object>(e.PartitionKey, e.RowKey, e.deviceId, e.propertyLabel1, e.propertyLabel2, e.property1, s_property2: e.property2, e.Misc);
        var _sendSmsTask = SendSmsAsync(entity, messageSender, log);  //send sms immediately if needed
        var _sendEmailTask = SendEmailAsync(entity, log);  //send sms immediately if needed

        //send message as necessary{asynchronously}
        
        string sentMessage = await _sendSmsTask;
        string sentEmail = await _sendEmailTask;
        log.LogInformation(($"Text Message processed successfully:{sentMessage}"));
        log.LogInformation(($"Email Message processed successfully.:{sentEmail}"));

        

        // Create the table if it doesn't exist.
        // table.CreateIfNotExists();
}
public static async Task<string> SendEmailAsync(PlayerEntity<object> _entity, ILogger log){
    string _clientMailAddress = ReturnEmail(_entity);
    string _mailContent = _entity.Misc;
    var message = new Message
            {
                Subject = "Bolu Morawo CloudHome ALert",
                Body = new ItemBody{
                    ContentType = BodyType.Html, Content = _mailContent
                },
                ToRecipients = new List<Recipient>(){
                    new Recipient{
                        EmailAddress = new EmailAddress{
                            Address = _clientMailAddress
                        }
                    },
                    new Recipient{
                        EmailAddress = new EmailAddress{
                            Address = "use user email"
                        }
                    }
                }
            };
            if (_entity.deviceId.Contains(myuserdetails)  && !String.IsNullOrEmpty(_entity.Misc) &&
            (_entity.Misc.Contains(warningMessage) || _entity.Misc.Contains(criticalMessage))){
                await AddEmailAsync(message);
                return $"Email sent Successfully: {_entity.Misc}";
        }
        else {
            return "Email doesn't need sending";
        }
}
public static async Task AddEmailAsync(Message _message)
{
    IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithTenantId(tenantId)
            .WithClientSecret(clientSecret)
            .Build();

            var authResultDirect = await confidentialClientApplication.AcquireTokenForClient(scopes).ExecuteAsync().ConfigureAwait(false);

//Microsoft.Graph.Auth is required for the following to work
            ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);
            GraphServiceClient graphClient = new GraphServiceClient(authProvider);

            await graphClient.Users[userId]
                        .SendMail(_message, false)
                        .Request()
                        .PostAsync();
                
}

public static async Task<string> SendSmsAsync(PlayerEntity<object> _entity, IAsyncCollector<CreateMessageOptions> _messageSender, ILogger log)
{   //send an SMS to user's phone no if iot telemetry signals a critical or warning message

    //initialize the CreateMessageOptions variable with the "To" phone number if it hasnt been assigned
    if (smsText == null){
        string _userPhoneNo = ReturnPhoneNo(_entity);
        if (!String.IsNullOrEmpty(_userPhoneNo)){
            smsText = new CreateMessageOptions(new PhoneNumber(_userPhoneNo));
        }
    }

    log.LogInformation("device ID is:" + _entity.deviceId);
    // log.LogInformation(($"Sms text state is:{(} deviceIdcheck: {} contains check:{} "));
    log.LogInformation("Sms text state is: " + (smsText != null).ToString());
    log.LogInformation("deviceIdcheck: " + (_entity.deviceId == myuserdetails).ToString());
    if (!String.IsNullOrEmpty(_entity.Misc)){
    log.LogInformation("contains check: " + (_entity.Misc.Contains(warningMessage) || _entity.Misc.Contains(criticalMessage)).ToString());
    log.LogInformation("Device Property2: " + Convert.ToString(_entity.property2));

    }
    
    //when "myuserdetails" brings the warning or critial message
    if (_entity.deviceId.Contains(myuserdetails)  && smsText != null && !String.IsNullOrEmpty(_entity.Misc) &&
        (_entity.Misc.Contains(warningMessage) || _entity.Misc.Contains(criticalMessage))){
         //get the message text to be sent
        smsText.Body = _entity.Misc;
        log.LogInformation("misc is:" + _entity.Misc );
        await _messageSender.AddAsync(smsText);
        return $"Sms sent Successfully: {_entity.Misc}";
    }
    else {
        return "Sms doesn't need sending";
    }
}

public static string ReturnPhoneNo(PlayerEntity<object> _entity)
{
    if (_entity.deviceId == myuserdetails){
        //the number sarts from + and ends before ;
        //  int startIndex = s.IndexOf('+');
        //  int endIndex = s.LastIndexOf(';');
        //  int length = endIndex - startIndex + 1;
        //  var phoneNo = Convert.ToString(_entity.property2).Substring(startIndex, length).Trim();
        var phoneNo = Convert.ToString(_entity.property2).Trim();
        return phoneNo;
        //  return Convert.ToString(_entity.property2).Trim();
    }
    else {
        return String.Empty;
    }
}
public static string ReturnEmail(PlayerEntity<object> _entity)
{
    if (_entity.deviceId == myuserdetails){
        //the number sarts from + and ends before ;
        var Email = Convert.ToString(_entity.RowKey)
            .Substring(_entity.deviceId.LastIndexOf(":") + 1).Trim();
        return Email;
        //  return Convert.ToString(_entity.property2).Trim();
    }
    else {
        return String.Empty;
    }
}

public class PlayerEntity<T> : TableEntity
{
        public string deviceId { get; set; }    //name of device
        //the label of the properties eg temperature, humidity
        public string propertyLabel1 { get; set; }  //usually affirms connection
        public string propertyLabel2 { get; set; }  //most important parameter here
        public bool property1 { get; set; }   //corresponds to PartitionKey
        public T property2 { get; set; }   //corresponds to RowKey
        public string Etag { get; set; }
        public string Misc { get; set; }    //for any redundant data needed

    public PlayerEntity() {}

    public PlayerEntity(string skey, string srow)
    {
        this.PartitionKey = skey;
        this.RowKey = srow;
        Etag = this.PartitionKey + this.RowKey; //let thus be the E_tag
    }
    public PlayerEntity(string s_partitionKey, string s_rowKey, string s_myDeviceId, string label1, string label2,bool s_property1, T s_property2, string s_misc)
        {
            this.PartitionKey = s_partitionKey;
            this.RowKey = s_rowKey;
            deviceId = s_myDeviceId;
            propertyLabel1 = label1;
            propertyLabel2 = label2;
            property1 = s_property1;
            property2 = s_property2;
            Etag = this.RowKey + this.PartitionKey; //let thus be the E_tag
            Misc = s_misc;
        }

    
}

public class TelemetryDataPoint<T>
    {
        public string PartitionKey { get; set; }    //type of device
        public string RowKey { get; set; }  //serial number given to device from I2C etc.
        public string deviceId { get; set; }    //name of device
        //the label of the properties eg temperature, humidity
        public string propertyLabel1 { get; set; }  //usually affirms connection
        public string propertyLabel2 { get; set; }  //most important parameter here
        public bool property1 { get; set; }   //corresponds to PartitionKey
        public T property2 { get; set; }   //the most important data of the device
        public string Etag { get; set; }
        public string Misc { get; set; }    //for any redundant data needed
        
        public TelemetryDataPoint(string s_partitionKey, string s_rowKey, string s_myDeviceId, string label1, string label2,bool s_property1, T s_property2, string s_misc)
        {
            PartitionKey = s_partitionKey;
            RowKey = s_rowKey;
            deviceId = s_myDeviceId;
            propertyLabel1 = label1;
            propertyLabel2 = label2;
            property1 = s_property1;
            property2 = s_property2;
            Etag = this.RowKey + this.PartitionKey; //let thus be the E_tag
            Misc = s_misc;
        }
    }