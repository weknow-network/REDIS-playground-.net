using StackExchange.Redis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public class Redis_PubSub_Tests : RedisBaseTests
    {
        #region Ctor

        public Redis_PubSub_Tests(ITestOutputHelper output) : base(output)
        {
        }

        #endregion // Ctor

        #region PubSub_Test

        [Fact]
        public async Task PubSub_Test()
        {
            ISubscriber sub = _redis.GetSubscriber();
            double value1 = 0;
            double value2 = 0;
            using (var gate1 = new ManualResetEventSlim())
            using (var gate2 = new ManualResetEventSlim())
            {
                string message = string.Empty;
                sub.Subscribe("messages")
                   .OnMessage(async channelMessage =>
                   {
                       await Task.Delay(1).ConfigureAwait(false);
                       Assert.True(channelMessage.Message.TryParse(out value1));
                       gate1.Set();
                   });
                sub.Subscribe("messages")
                   .OnMessage(async channelMessage =>
                   {
                       await Task.Delay(1).ConfigureAwait(false);
                       Assert.True(channelMessage.Message.TryParse(out value2));
                       gate2.Set();
                   });
                await sub.PublishAsync("messages", 1).ConfigureAwait(false);
                Assert.True(gate1.Wait(TimeSpan.FromSeconds(10)));
                Assert.True(gate2.Wait(TimeSpan.FromSeconds(10)));
            }

            Assert.Equal(1, value1);
            Assert.Equal(1, value2);
        }

        #endregion // PubSub_Test

        #region PubSub_PatternMatching_Test

        [Fact]
        public async Task PubSub_PatternMatching_Test()
        {
            ISubscriber sub = _redis.GetSubscriber();
            ConcurrentQueue<double> queue = new ConcurrentQueue<double>();
            double value2 = 0;
            using (var gate = new CountdownEvent(3))
            {
                string message = string.Empty;
                sub.Subscribe("news.*")
                   .OnMessage(async channelMessage =>
                   {
                       await Task.Delay(1).ConfigureAwait(false);
                       Assert.True(channelMessage.Message.TryParse(out double value));
                       queue.Enqueue(value);
                       gate.Signal();
                   });
                sub.Subscribe("sport.*")
                   .OnMessage(async channelMessage =>
                   {
                       Assert.True(channelMessage.Message.TryParse(out value2));
                       queue.Enqueue(value2);
                       await Task.Delay(1).ConfigureAwait(false);
                       gate.Signal();
                   });
                await sub.PublishAsync("news.art.figurative", 1).ConfigureAwait(false);
                await sub.PublishAsync("news.music.jazz", 2).ConfigureAwait(false);
                await sub.PublishAsync("sport.Bike", 3).ConfigureAwait(false);
                Assert.True(gate.Wait(TimeSpan.FromSeconds(10)));
            }

            var set = new HashSet<double>(queue);
            Assert.Contains(1, set);
            Assert.Contains(2, set);
            Assert.Contains(3, set);
        } 

        #endregion // PubSub_PatternMatching_Test
    }
}
