using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

// https://stackexchange.github.io/StackExchange.Redis/Basics
// Deploy REDIS on K8s: helm install redis stable/redis --set cluster.enabled=false --set usePassword=false --namespace redis
// Port Forward: kubectl port-forward --namespace redis svc/redis-master 6379:6379


namespace RedisPlayground
{
    public class Redis_Hash_Tests : RedisBaseTests
    {
        private readonly IDatabase _db;

        #region Ctor

        public Redis_Hash_Tests(ITestOutputHelper output) : base(output)
        {
            _db = _redis.GetDatabase();
        }

        #endregion // Ctor

        [Fact]
        public async Task Hash_Test()
        {
            await _db.HashDeleteAsync("Classes", "Class A").ConfigureAwait(false);
            await _db.HashSetAsync("Classes", "Class A", 10).ConfigureAwait(false);
            RedisValue value = await _db.HashGetAsync("Classes", "Class A").ConfigureAwait(false);

            Assert.True(value.TryParse(out double val));
            Assert.Equal(10, val);
        }
    }
}
