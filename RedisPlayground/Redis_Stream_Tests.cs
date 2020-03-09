using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

// https://stackexchange.github.io/StackExchange.Redis/Basics
// Deploy REDIS on K8s: helm install redis stable/redis --set cluster.enabled=false --set usePassword=false --namespace redis
// Port Forward: kubectl port-forward --namespace redis svc/redis-master 6379:6379
// StackExchange.Redis Samples: https://stackexchange.github.io/StackExchange.Redis/Streams.html

namespace RedisPlayground
{
    public class Redis_Stream_Tests : RedisBaseTests
    {
        private readonly IDatabase _db;

        #region Ctor

        public Redis_Stream_Tests(ITestOutputHelper output) : base(output)
        {
            _db = _redis.GetDatabase();
        }

        #endregion // Ctor

        #region Stream_Add_Test

        [Fact]
        public async Task Stream_Add_Test()
        {
            try
            {
                await _db.KeyDeleteAsync("X").ConfigureAwait(false);
                RedisValue id = await _db.StreamAddAsync("X", new[]
                {
                new NameValueEntry("A", 1),
                new NameValueEntry("B", 2)
            }).ConfigureAwait(false);
                StreamInfo info = await _db.StreamInfoAsync("X").ConfigureAwait(false);

                _output.WriteLine($"Info: Length = {info.Length}, ConsumerGroupCount = {info.ConsumerGroupCount}, LastGeneratedId = {info.LastGeneratedId}");

                StreamEntry[] values = await _db.StreamReadAsync("X", "0", 2).ConfigureAwait(false);
                //StreamEntry[] values1 = await _db.StreamReadAsync("X", id, 2).ConfigureAwait(false);

                Assert.Single(values);
                Assert.Contains(values[0].Values, nv => nv.Name == "A" && nv.Value == 1);
                Assert.Contains(values[0].Values, nv => nv.Name == "B" && nv.Value == 2);
            }
            finally
            {
                await _db.KeyDeleteAsync("X").ConfigureAwait(false);
            }
        }

        #endregion // Stream_Add_Test}


        [Fact] //(Skip = "Waiting for pull request StackExchange.Redis")]
        public async Task ConsumerGroup_Test()
        {
            try
            {
                await _db.KeyDeleteAsync("events_stream").ConfigureAwait(false);


                await _db.StreamAddAsync("events_stream", "Begin", -1).ConfigureAwait(false); // walk around, StreamCreateConsumerGroupAsync don't create the stream as it should 

                await _db.StreamDeleteConsumerGroupAsync("events_stream", "events_cg").ConfigureAwait(false);
                await _db.StreamCreateConsumerGroupAsync("events_stream", "events_cg", StreamPosition.Beginning).ConfigureAwait(false);

                for (int i = 0; i < 5; i++)
                {
                    RedisValue id = await _db.StreamAddAsync("events_stream", "A", i).ConfigureAwait(false);
                }
                StreamEntry[] values = await _db.StreamReadGroupAsync("events_stream", "events_cg", "Consumer A",
                                                        position: StreamPosition.Beginning,
                                                        count: 10).ConfigureAwait(false);

                foreach (StreamEntry value in values)
                {
                    await _db.StreamAcknowledgeAsync("events_stream", "events_cg", value.Id).ConfigureAwait(false);
                }

                StreamInfo info = await _db.StreamInfoAsync("events_stream").ConfigureAwait(false);
                _output.WriteLine($"Info: Length = {info.Length}, ConsumerGroupCount = {info.ConsumerGroupCount}, LastGeneratedId = {info.LastGeneratedId}");
                StreamGroupInfo[] groupInfos = await _db.StreamGroupInfoAsync("events_stream").ConfigureAwait(false);

                foreach (StreamGroupInfo groupInfo in groupInfos)
                {
                    _output.WriteLine($"Info: Name = {groupInfo.Name}, ConsumerCount = {groupInfo.ConsumerCount}, PendingMessageCount = {groupInfo.PendingMessageCount}");
                }

                //values = values.Skip(1).ToArray(); // walk around, side effect
                //Assert.Equal(5, values.Length);
                //for (int i = 0; i < 5; i++)
                //{
                //    Assert.Contains(values[i].Values, nv => nv.Name == "A" && nv.Value == i);
                //}
            }
            finally
            {
                await _db.KeyDeleteAsync("events_stream").ConfigureAwait(false);
            }
        }

        //[Fact]
        //public async Task Stream_Raw_Test()
        //{
        //    await _db.KeyDeleteAsync("events_stream").ConfigureAwait(false);

        //    RedisResult consumer = await _db.ExecuteAsync("XGROUP", "CREATE", "mystream", "events_cg", "$", "MKSTREAM").ConfigureAwait(false);

        //    for (int i = 0; i < 5; i++)
        //    {
        //        RedisResult added = await _db.ExecuteAsync("XADD", "mystream", "*", "value", i).ConfigureAwait(false);
        //    }

        //    RedisResult read = await _db.ExecuteAsync("XREADGROUP", "GROUP", "events_cg", "consumera", "COUNT ", 10, "STREAMS", "mystream").ConfigureAwait(false);

        //}
    }
}
