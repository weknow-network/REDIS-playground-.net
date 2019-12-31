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
    public class Redis_String_Tests : RedisBaseTests
    {
        private readonly IDatabase _db;

        #region Ctor

        public Redis_String_Tests(ITestOutputHelper output) : base(output)
        {
            _db = _redis.GetDatabase();
        }

        #endregion // Ctor

        [Fact]
        public async Task StringSet_Test()
        {
            await _db.KeyDeleteAsync("A").ConfigureAwait(false);
            await _db.StringSetAsync("A", 1).ConfigureAwait(false);
            RedisValue value = await _db.StringGetAsync("A").ConfigureAwait(false);

            Assert.True(value.TryParse(out double val));
            Assert.Equal(1, val);
        }

        [Fact]
        public async Task StringSetAppend_Test()
        {
            await _db.KeyDeleteAsync("A").ConfigureAwait(false);
            await _db.StringSetAsync("A", "ABC").ConfigureAwait(false);
            await _db.StringAppendAsync("A", "DE").ConfigureAwait(false);
            RedisValue value = await _db.StringGetAsync("A").ConfigureAwait(false);

            Assert.Equal("ABCDE", value);
        }
    }
}
