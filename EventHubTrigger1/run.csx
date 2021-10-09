#r "Microsoft.Azure.EventHubs"
#r "Microsoft.Azure.Cosmos.Table"
#r "Newtonsoft.Json"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Microsoft.Azure.EventHubs;

// public static async Task Run(string events, ILogger log)
// public static readonly string connString = "DefaultEndpointsProtocol=https;AccountName=csb50998223a98f;AccountKey=EDq/hstdbgTSRGREDG/sdhrydydu66sdg+XI2H2AWnzjRdAFmr+E3kb3M9lbVRAVaASGBA==;EndpointSuffix=core.windows.net";
public static readonly string connString = "use your own connection string it looks like the above comment";
//message levels
public const string normalMessage = "normal";
public const string warningMessage = "warning";
public const string criticalMessage = "critical";

public static readonly string myuserdetails = "myuserdetails";
// public static async Task Run(string events, IAsyncCollector<CreateMessageOptions> messageSender, ILogger log)
public static async Task Run(string events, ILogger log)
{
    
    log.LogInformation($"C# IoTHub queue trigger function processed event: {events}");
    var e = JsonConvert.DeserializeObject<TelemetryDataPoint<object>>(events as string);

        // CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=csb50998223a98f;AccountKey=EDq/hstdbgTSRGREDG/sdhrydydu66sdg+XI2H2AWnzjRdAFmr+E3kb3M9lbVRAVaASGBA==;EndpointSuffix=core.windows.net");
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse("use your own connection string it looks like the above comment");
        
        // Create the table client.
        CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

        // Retrieve a reference to the table.
        CloudTable table = tableClient.GetTableReference("FinalYearTable");

        PlayerEntity<object> entity = new PlayerEntity<object>(e.PartitionKey, e.RowKey, e.deviceId, e.propertyLabel1, e.propertyLabel2, e.property1, s_property2: e.property2, e.Misc);

        //send message if necessary{asynchronously}
        
        //Flatten object and convert it to EntityProperty Dictionary
        Dictionary<string, EntityProperty> flattenedProperties = EntityPropertyConverter.Flatten(entity, null);

        // Create a DynamicTableEntity and set its PK and RKe.
        DynamicTableEntity dynamicTableEntity = new DynamicTableEntity(e.PartitionKey, e.RowKey);
        dynamicTableEntity.Properties = flattenedProperties;
        // entity.Etag = "*";

        TableOperation insertOperation = TableOperation.InsertOrReplace (dynamicTableEntity);
        // TableOperation insertOperation = TableOperation.InsertOrReplace (entity as PlayerEntity<object>);
        var _insertTask = table.ExecuteAsync(insertOperation);  //start the insert operation

        
        //await Task.WhenAll(_intertTask, _sendSmsTask);  
        await _insertTask;
        log.LogInformation(($"Table updated successfully"));

        

        // Create the table if it doesn't exist.
        // table.CreateIfNotExists();
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