#r "Newtonsoft.Json"
#r "Microsoft.Azure.Cosmos.Table"

using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos.Table;

//class to query table for devices info; specific device is selected based on row and partition
//then returned to client app for display or action 

// public static readonly string connString = "DefaultEndpointsProtocol=https;AccountName=csb50998223a98f;AccountKey=EDq/hstdbgTSRGREDG/sdhrydydu66sdg+XI2H2AWnzjRdAFmr+E3kb3M9lbVRAVaASGBA==;EndpointSuffix=core.windows.net";
public static readonly string connString = "use your own connection string it looks like the above comment";//device row and partition should be able to uniquely identify each device
public static string device_partition;
public static string device_row;


public static async Task<IActionResult> Run(HttpRequest req, CloudTable inputTable, ILogger log)
{
    log.LogInformation("C# HTTP trigger function processed a request.");
    string responseMessage = string.Empty;

    device_partition = req.Query["partition"];
    device_row = req.Query["row"];
    log.LogInformation("received partition is: " + device_partition + "\nreceived row is: " + device_row);

    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    dynamic data = JsonConvert.DeserializeObject(requestBody);
    //check if query is null, else assign the contents f the Json request o output if it is also
    //not null
    try{
    device_partition = device_partition ?? data?.name;  
    device_row = device_row ?? data?.name;
    }
    catch (NullReferenceException e){
        responseMessage = "There is no device that matches the query properties partition and row\n check for spelling errors in identifying device, otherwise contact your installer";
        device_partition = string.Empty;
        device_row = string.Empty;
    }

    
    
    if (string.IsNullOrEmpty(device_partition) || string.IsNullOrEmpty(device_row)){
        responseMessage = "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.";
    }
    else {
        TableQueryClass querytheTable = new TableQueryClass(device_partition, device_row);
        // querytheTable.RetrieveFirstEntity(inputTable);
        //etrieve the device data from table
        //Read the entity back from AzureTableStorage as DynamicTableEntity using the same PK and RK
        DynamicTableEntity dynamicEntity = await querytheTable.RetrieveFirstDynamicEntity(inputTable);

        //Convert the DynamicTableEntity back to original complex object.
        PlayerEntity<object> outputTableEntity = EntityPropertyConverter.ConvertBack<PlayerEntity<object>>(dynamicEntity.Properties, null);
        log.LogInformation("The partition of the Dynamictable is " + (outputTableEntity.PartitionKey));

        // TableEntity outputTableEntity = querytheTable.RetrieveFirstEntity(inputTable);  //retrieve the associated table entity
        TelemetryDataPoint<object> outputTelemetry = querytheTable.CastToDataPoint<object>(outputTableEntity, dynamicEntity);
        
        responseMessage = JsonConvert.SerializeObject(outputTelemetry);

        log.LogInformation("The Etag of the table is " + (outputTelemetry.Etag));
        log.LogInformation($"Hello, device teletry sent in Json is {responseMessage}");

        // var property1 = (await querytheTable.RetrieveFirstEntity(inputTable)).property1;
        // var property2 = (await querytheTable.RetrieveFirstEntity(inputTable)).property2;

        // responseMessage = $"Hello, device teletry sent in Json is {TelemetryDataString}";

    }

    // string responseMessage = string.IsNullOrEmpty(device_partition)
    //     ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
    //             : $"Hello, device parameters are {device_partition}: {property1} and {device_row}: {property2}";

        return new OkObjectResult(responseMessage);
}

public class PlayerEntity<T> : TableEntity //used in httptrigger 1 to store the table elements
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

    public PlayerEntity(string s_partitionKey, string s_rowKey, string s_myDeviceId, string label1, string label2,bool s_property1, T s_property2, string s_misc = null)
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
        public T property2 { get; set; }   //corresponds to RowKey
        public string Etag { get; set; }
        public string Misc { get; set; }    //for any redundant data needed
        

        public TelemetryDataPoint(string s_partitionKey, string s_rowKey, string s_myDeviceId, string label1, string label2,bool s_property1, T s_property2, string s_misc = null)
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

public class TableQueryClass{

    public TableQuery<DynamicTableEntity> rangeQuery = new TableQuery<DynamicTableEntity> (); //use this to store the table query
    // public CloudTable currentTable;

    public TableQueryClass(string s_partitionKey, string s_rowKey){
        rangeQuery = new TableQuery<DynamicTableEntity>().Where(
    TableQuery.CombineFilters(
        TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, 
            s_partitionKey),
        TableOperators.And,
        TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, 
            s_rowKey)));
        
        // currentTable = queryTable;
    }
    public TableQueryClass(string deviceName){  //generate query based on device name
        rangeQuery = new TableQuery<DynamicTableEntity>()
            .Where(TableQuery.GenerateFilterCondition("myDeviceId", QueryComparisons.Equal, deviceName));
        
        // currentTable = queryTable;
    }

    public async Task<DynamicTableEntity> RetrieveFirstDynamicEntity(CloudTable currentTable){
        foreach (var dynamicEntity in 
            await currentTable.ExecuteQuerySegmentedAsync<DynamicTableEntity>(rangeQuery, null))
                {
                    return dynamicEntity;    //return the first entity
                }
        
        // return e.FirstOrDefault(default);
        return null;
    }

    public TelemetryDataPoint<T> CastToDataPoint<T>(PlayerEntity<T> _entity, DynamicTableEntity _dynamicEntity){    //casts data from tableentity to telemetry data point

        return new TelemetryDataPoint<T>(_dynamicEntity.PartitionKey, _dynamicEntity.RowKey, _entity.deviceId, _entity.propertyLabel1, _entity.propertyLabel2, _entity.property1, _entity.property2, _entity.Misc);
    }
}
