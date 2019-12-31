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


namespace RedisPlayground
{
    public class Redis_Batch_Tests : RedisBaseTests
    {
        private readonly IDatabase _db;

        #region Ctor

        public Redis_Batch_Tests(ITestOutputHelper output) : base(output)
        {
            _db = _redis.GetDatabase();
        }

        #endregion // Ctor

        [Fact]
        public async Task Batch_Test()
        {
            var key = "BATCH_TEST";
            await _db.KeyDeleteAsync(key).ConfigureAwait(false);
            //await _db.StringSetAsync(key, "batch-sent").ConfigureAwait(false);

            var batch = _db.CreateBatch();
            Task<bool>[] tasks = (from i in Enumerable.Range(0, 5)
                        let v = (char)('A' + i)
                        select batch.SetAddAsync(key, v.ToString()))
                        .ToArray(); 
            batch.Execute();
            bool[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
            RedisValue[] arr = await _db.SetMembersAsync(key).ConfigureAwait(false);

            Array.Sort(arr, (x, y) => string.Compare(x, y));
            Assert.All(results, b => Assert.True(b));
            Assert.Equal(5, arr.Length);
            Assert.True(arr.SequenceEqual(new RedisValue[] { "A", "B", "C", "D", "E" }));
        }


    }
}
