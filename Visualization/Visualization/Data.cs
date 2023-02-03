using Amazon.DynamoDBv2.DataModel;

namespace Visualization
{
    [DynamoDBTable("ChartDB")]
    public class Data
    {
        [DynamoDBHashKey("topic")]
        public string? topic { get; set; }

        [DynamoDBProperty("published")]
        public string published { get; set; }

        [DynamoDBProperty("added")]
        public string? added { get; set; }

        [DynamoDBProperty("sector")]
        public string? sector { get; set; }

        [DynamoDBProperty("source")]
        public string? source { get; set; }

        [DynamoDBProperty("url")]
        public string? url { get; set; }

        [DynamoDBProperty("country")]
        public string? country { get; set; }

        [DynamoDBProperty("relevance")]
        public int? relevance { get; set; }

        [DynamoDBProperty("likelihood")]
        public int? likelihood { get; set; }

        [DynamoDBProperty("pestle")]
        public string? pestle { get; set; }

        [DynamoDBProperty("region")]
        public string? region { get; set; }

        [DynamoDBProperty("insight")]
        public string? insight { get; set; }

        [DynamoDBProperty("intensity")]
        public int? intensity { get; set; }

        [DynamoDBProperty("title")]
        public string? title { get; set; }

        [DynamoDBProperty("end_year")]
        public int? end_year { get; set; }

        [DynamoDBProperty("start_year")]
        public int? start_year { get; set; }

        [DynamoDBProperty("impact")]
        public int? impact { get; set; }

    }
}
