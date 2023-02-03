using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System.Net;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Visualization;

public class Function
{
    //Get All Data from Database
	public async Task<APIGatewayProxyResponse> GetAllDataAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
	{
		AmazonDynamoDBClient client = new AmazonDynamoDBClient();
		DynamoDBContext dbContext;
		dbContext = new DynamoDBContext(client);
		var data = await dbContext.ScanAsync<Data>(default).GetRemainingAsync();
		var response = new APIGatewayProxyResponse
		{
			StatusCode = 200,
			Body = JsonConvert.SerializeObject(data),
			Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
		};
		return response;
	}

    //Get All Topics
	public async Task<APIGatewayHttpApiV2ProxyResponse> GetDataByTopicAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext;
        dbContext = new DynamoDBContext(client);

        var items = await dbContext.ScanAsync<Data>(new List<ScanCondition>()).GetRemainingAsync();
        var data = new List<Data>();

        foreach (var item in items)
        {
            data.Add(new Data
            {
                topic = item.topic,
                published = item.published,
                added = item.added,
                sector = item.sector,
                source = item.source,
                url = item.url,
                country = item.country,
                relevance = item.relevance,
                likelihood = item.likelihood,
                pestle = item.pestle,
                region = item.region,
                insight = item.insight,
                intensity = item.intensity,
                title = item.title,
                end_year = item.end_year,
                start_year = item.start_year,
                impact = item.impact
            });
        }


        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 200,
            Body = Newtonsoft.Json.JsonConvert.SerializeObject(data)
        };
    }

    //Get All Sectors
    public async Task<APIGatewayProxyResponse> GetSectorData(APIGatewayProxyRequest request, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext;
        dbContext = new DynamoDBContext(client);
        var sector = request.QueryStringParameters["sector"];
        var data = await dbContext.QueryAsync<Data>(sector, new DynamoDBOperationConfig { IndexName = "sector-index" }).GetRemainingAsync();
        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonConvert.SerializeObject(data)
        };
        return response;
    }

    //Data by sector
    public async Task<APIGatewayHttpApiV2ProxyResponse> GetDataBySectorAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext;
        dbContext = new DynamoDBContext(client);

        var items = await dbContext.ScanAsync<Data>(new List<ScanCondition>()).GetRemainingAsync();
        var data = new List<Data>();

        foreach (var item in items)
        {
            data.Add(new Data
            {
                topic = item.topic,
                sector = item.sector
            });
        }

        return new APIGatewayHttpApiV2ProxyResponse
        {
            StatusCode = 200,
            Body = Newtonsoft.Json.JsonConvert.SerializeObject(data)
        };
    }

    /*format the data as a dictionary where key is the sector name and value is the number of items
      in that sector. This will be used to create a pie chart to represent the distribution of data 
      across different sectors.
    */
    public async Task<APIGatewayProxyResponse> GetSectorPieData(APIGatewayProxyRequest request, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext = new DynamoDBContext(client);
        var data = await dbContext.ScanAsync<Data>(new List<ScanCondition>()).GetRemainingAsync();

        var result = data.Where(x => x.sector != null)
            .GroupBy(x => x.sector!)
            .ToDictionary(x => x.Key, x => x.Count());

        var totalCount = result.Sum(x => x.Value);
        var pieData = result.ToDictionary(x => x.Key, x => (double)x.Value / totalCount);

        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonConvert.SerializeObject(pieData)
        };
        return response;
    }

    /*formats the data into a dictionary where the keys are the relevance values and the values 
     * are the count of the records with that relevance value: This will be used to create a 
     * bar chart to represent the distribution of the data values.
    */
    public async Task<APIGatewayProxyResponse> GetRelevanceBySector(APIGatewayProxyRequest request, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext = new DynamoDBContext(client);
        var data = await dbContext.ScanAsync<Data>(new List<ScanCondition>()).GetRemainingAsync();

        var relevanceBySector = data
            .Where(x => x.relevance != null && x.sector != null)
            .GroupBy(x => x.sector)
            .ToDictionary(x => x.Key ?? "Unknown", x => x.Average(y => y.relevance.GetValueOrDefault()));



        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonConvert.SerializeObject(relevanceBySector)
        };
        return response;
    }

    /*This function calculates the average likelihood for each sector by querying the Data items 
     * in the DynamoDB table and grouping them by the sector attribute. The result is a dictionary 
     * where the keys are the sector names and the values are the average likelihood values. 
     * The response is returned as a JSON string in the API Gateway response object.
    */

    public static async Task<APIGatewayProxyResponse> LikelihoodBySector(APIGatewayProxyRequest request, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext = new DynamoDBContext(client);
        var data = await dbContext.ScanAsync<Data>(new List<ScanCondition>()).GetRemainingAsync();

        var likelihoodBySector = data
            .Where(x => x.likelihood != null && x.sector != null)
            .GroupBy(x => x.sector)
            .ToDictionary(x => x.Key ?? "Unknown", x => x.Average(y => y.likelihood.GetValueOrDefault()));

        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonConvert.SerializeObject(likelihoodBySector)
        };
        return response;
    }

    /* This function returns the intensity values for each sector in a JSON format. 
     * We will use the data in the frontend with D3.js to represent the intensity values as a histogram.
     */
    public static async Task<APIGatewayProxyResponse> IntensityBySector(APIGatewayProxyRequest request, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext = new DynamoDBContext(client);
        var data = await dbContext.ScanAsync<Data>(new List<ScanCondition>()).GetRemainingAsync();

        var intensityBySector = data
            .Where(x => x.intensity != null && x.sector != null)
            .GroupBy(x => x.sector)
            .ToDictionary(x => x.Key ?? "Unknown", x => x.Select(y => y.intensity.GetValueOrDefault()));

        var json = JsonConvert.SerializeObject(intensityBySector);

        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = json,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            }
        };

        return response;
    }

    /* Impact By Sector
     */
    public static async Task<APIGatewayProxyResponse> ImpactBySector(APIGatewayProxyRequest request, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext = new DynamoDBContext(client);
        var data = await dbContext.ScanAsync<Data>(new List<ScanCondition>()).GetRemainingAsync();

        var impactBySector = data
            .Where(x => x.impact != null && x.sector != null)
            .GroupBy(x => x.sector)
            .ToDictionary(x => x.Key ?? "Unknown", x => x.Select(y => y.impact.GetValueOrDefault()));

        var json = JsonConvert.SerializeObject(impactBySector);

        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = json,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            }
        };

        return response;
    }

    //Topic By Sector, we will represent this on a Histogram
    public static async Task<APIGatewayProxyResponse> TopicBySector(APIGatewayProxyRequest request, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext = new DynamoDBContext(client);
        var data = await dbContext.ScanAsync<Data>(new List<ScanCondition>()).GetRemainingAsync();

        var topicBySector = data
            .Where(x => x.topic != null && x.sector != null)
            .GroupBy(x => x.sector)
            .ToDictionary(x => x.Key ?? "Unknown", x => x.Select(y => y.topic).Distinct());

        var json = JsonConvert.SerializeObject(topicBySector);

        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = json,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            }
        };

        return response;
    }

    // Returns the data, matching the topic in query string
    public async Task<APIGatewayProxyResponse> GetFiletredDataByTopicAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext;
        dbContext = new DynamoDBContext(client);

        var topic = request.QueryStringParameters?["topic"];
        var scanConditions = new List<ScanCondition>();
        if (!string.IsNullOrEmpty(topic))
        {
            scanConditions.Add(new ScanCondition("topic", ScanOperator.Equal, topic));
        }

        var data = await dbContext.ScanAsync<Data>(scanConditions).GetRemainingAsync();
        var filteredData = data.Where(d => !string.IsNullOrEmpty(topic) && d.topic == topic).ToList();


        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonConvert.SerializeObject(filteredData),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
        return response;
    }

    // Returns the data, matching the Sector in query string
    public async Task<APIGatewayProxyResponse> GetFiletredDataBySectorAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext;
        dbContext = new DynamoDBContext(client);

        var sector = request.QueryStringParameters?["sector"];
        var scanConditions = new List<ScanCondition>();
        if (!string.IsNullOrEmpty(sector))
        {
            scanConditions.Add(new ScanCondition("sector", ScanOperator.Equal, sector));
        }

        var data = await dbContext.ScanAsync<Data>(scanConditions).GetRemainingAsync();
        var filteredData = data.Where(d => !string.IsNullOrEmpty(sector) && d.sector == sector).ToList();


        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonConvert.SerializeObject(filteredData),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
        return response;
    }

    // Returns the data, matching the pestle in query string
    public async Task<APIGatewayProxyResponse> GetFiletredDataByPestleAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext;
        dbContext = new DynamoDBContext(client);

        var pestle = request.QueryStringParameters?["pestle"];
        var scanConditions = new List<ScanCondition>();
        if (!string.IsNullOrEmpty(pestle))
        {
            scanConditions.Add(new ScanCondition("pestle", ScanOperator.Equal, pestle));
        }

        var data = await dbContext.ScanAsync<Data>(scanConditions).GetRemainingAsync();
        var filteredData = data.Where(d => !string.IsNullOrEmpty(pestle) && d.pestle == pestle).ToList();


        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonConvert.SerializeObject(filteredData),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
        return response;
    }

    // Returns the data, matching the source in query string
    public async Task<APIGatewayProxyResponse> GetFiletredDataBySourceAsync(APIGatewayHttpApiV2ProxyRequest request, ILambdaContext context)
    {
        AmazonDynamoDBClient client = new AmazonDynamoDBClient();
        DynamoDBContext dbContext;
        dbContext = new DynamoDBContext(client);

        var source = request.QueryStringParameters?["source"];
        var scanConditions = new List<ScanCondition>();
        if (!string.IsNullOrEmpty(source))
        {
            scanConditions.Add(new ScanCondition("source", ScanOperator.Equal, source));
        }

        var data = await dbContext.ScanAsync<Data>(scanConditions).GetRemainingAsync();
        var filteredData = data.Where(d => !string.IsNullOrEmpty(source) && d.source == source).ToList();


        var response = new APIGatewayProxyResponse
        {
            StatusCode = 200,
            Body = JsonConvert.SerializeObject(filteredData),
            Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
        };
        return response;
    }


}













