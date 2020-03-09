using StackExchange.Redis;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

// Deploy REDIS on K8s: helm install redis stable/redis --set cluster.enabled=false --set usePassword=false --namespace redis
// Port Forward: kubectl port-forward --namespace redis svc/redis-master 6379:6379

namespace RedisPlayground
{
    public class RedisBaseTests: IDisposable
    {
        protected readonly ConnectionMultiplexer _redis;
        protected readonly ITestOutputHelper _output;
        private readonly Process _portForwarding;
        private static int _portForwardingCounter = 0;
        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly StringWriter _writer;

        #region Ctor

        public RedisBaseTests(ITestOutputHelper output)
        {
            _output = output;
            int count = Interlocked.Increment(ref _portForwardingCounter);
            if(count == 1)
                _portForwarding = Process.Start("kubectl", "port-forward --namespace redis svc/redis-master 6379:6379");
            _writer = new StringWriter(_buffer);

            string endpoint = Environment.GetEnvironmentVariable("REDIS_ENDPOINT");
            if (string.IsNullOrEmpty(endpoint))
                _redis = ConnectionMultiplexer.Connect("localhost", _writer);
            else
            {
                string pass = Environment.GetEnvironmentVariable("REDIS_PASS");
                _redis = ConnectionMultiplexer.Connect($"{endpoint},password={pass}");
            }
        }

        #endregion // Ctor

        #region Dispose

        protected virtual void Dispose(bool disposing)
        { 
            _output.WriteLine(_buffer.ToString());
            _writer.Dispose();
            _redis.Dispose();
            int count = Interlocked.Decrement(ref _portForwardingCounter);
            if(count == 0)
                _portForwarding?.Dispose();
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~RedisBaseTests() => Dispose(false);
        #endregion // Dispose
    }
}
