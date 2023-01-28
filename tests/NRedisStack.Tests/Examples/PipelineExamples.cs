using Moq;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search;
using NRedisStack.Search.Aggregation;
using NRedisStack.Search.FT.CREATE;
using StackExchange.Redis;
using System;
using System.Net;
using System.Text.Json;
using Xunit;

namespace NRedisStack.Tests.Examples
{
    public class PipelineExamples : AbstractNRedisStackTest, IDisposable
    {
        private readonly string index = "pipeline-idx";

        public PipelineExamples(RedisFixture redisFixture) : base(redisFixture) { }

        public void Dispose()
        {
            redisFixture.Redis.GetDatabase().FT().DropIndex(index, true);
        }

        [Fact]
        public async Task JsonwithSearch()
        {
            //Setup pipeline connection
            var pipeline = new Pipeline(ConnectionMultiplexer.Connect("localhost"));

            //add JsonSet to pipeline
             pipeline.Json.SetAsync("person:01", "$", new { name = "John", age = 30, city = "New York" });
             pipeline.Json.SetAsync("person:02", "$", new { name = "Joy", age = 25, city = "Los Angeles" });
             pipeline.Json.SetAsync("person:03", "$", new { name = "Mark", age = 21, city = "Chicago" });
             pipeline.Json.SetAsync("person:04", "$", new { name = "Steve", age = 24, city = "Phoenix" });
             pipeline.Json.SetAsync("person:05", "$", new { name = "Michael", age = 55, city = "San Antonio" });

            // Create the schema to index first and age as a numeric field
            var schema = new Schema().AddTextField("name").AddNumericField("age", true).AddTagField("city");

            // Filter the index to only include Jsons with an age greater than 20, and prefix of 
            var parameters = FTCreateParams.CreateParams().On(Literals.Enums.IndexDataType.JSON).Prefix("person:");

            // Create the index
            pipeline.Search.CreateAsync(index, parameters, schema);

            //execute the pipeline
            pipeline.Execute();

            //search for all the 
            var getAllPersons = redisFixture.Redis.GetDatabase().FT().Search(index, new Query());

        }
    }
}
