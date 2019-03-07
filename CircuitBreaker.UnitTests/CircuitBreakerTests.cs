using CircuitBreaker.Core;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace CircuitBreaker.UnitTests
{
    public class CircuitBreakerTests
    {
        [Fact]
        public void ExecuteAction_StateShouldBeOpenAfterNumberOfFailuresExceedsThreshold()
        {
            //Arrange
            string key = "testKey";
            int numberOfFailuresThreshold = 2;
            TimeSpan windowDuration = TimeSpan.FromSeconds(1000);
            TimeSpan durationOfBreak = TimeSpan.FromMinutes(30);
            IRepository repository = Substitute.For<IRepository>();

            Dictionary<string, byte[]> dic = new Dictionary<string, byte[]>();

            SetRepositoryBehavior(key, repository, dic);

            List<IRule> rules = new List<IRule>
            {
                new FixedNumberOfFailuresRule(numberOfFailuresThreshold)
            };

            int actualNumberOfFailures = 0;

            //Act
            for (int i = 1; i < numberOfFailuresThreshold + 3; i++)
            {
                try
                {
                    var cb = CircuitBreakBuilder.Build(key, windowDuration, durationOfBreak, rules, repository);
                    cb.ExecuteAction(() => { throw new TimeoutException(); });
                }
                catch (BrokenCircuitException ex)
                {
                    actualNumberOfFailures = i - 1;//i stores the actual trial 
                    break;
                }
            }

            var cbf = CircuitBreakBuilder.Build(key, windowDuration, durationOfBreak, rules, repository);

            //Assert
            Assert.True(cbf.IsOpen());
            Assert.Equal(numberOfFailuresThreshold, actualNumberOfFailures);
        }

        [Fact]
        public void ExecuteAction_StateShouldBeClosedAfterDurationOfBreak()
        {
            //Arrange
            string key = "testKey";
            int numberOfFailuresThreshold = 2;
            TimeSpan windowDuration = TimeSpan.FromSeconds(1000);
            TimeSpan durationOfBreak = TimeSpan.FromSeconds(15);
            IRepository repository = Substitute.For<IRepository>();

            Dictionary<string, byte[]> dic = new Dictionary<string, byte[]>();

            SetRepositoryBehavior(key, repository, dic);

            List<IRule> rules = new List<IRule>
            {
                new FixedNumberOfFailuresRule(numberOfFailuresThreshold)
            };

            int actualNumberOfFailures = 0;

            //Act
            for (int i = 1; i <= numberOfFailuresThreshold +1; i++)
            {
                try
                {
                    var cb = CircuitBreakBuilder.Build(key, windowDuration, durationOfBreak, rules, repository);
                    cb.ExecuteAction(() => { throw new TimeoutException(); });
                }
                catch (BrokenCircuitException)
                {
                    actualNumberOfFailures = i - 1;//i stores the actual trial 
                    break;
                }
            }

            var cbOpened = CircuitBreakBuilder.Build(key, windowDuration, durationOfBreak, rules, repository);
            Assert.True(cbOpened.IsOpen());
            Assert.Equal(numberOfFailuresThreshold, actualNumberOfFailures);

            Thread.Sleep(durationOfBreak);
            dic.Clear();

            var cbClosed = CircuitBreakBuilder.Build(key, windowDuration, durationOfBreak, rules, repository);
            cbClosed.ExecuteAction(() => { throw new TimeoutException(); });
            //Assert
            Assert.False(cbClosed.IsOpen());
        }

        private static void SetRepositoryBehavior(string key, IRepository repository, Dictionary<string,byte[]> dic)
        {
            repository.GetString(key).ReturnsForAnyArgs(x => {
                var keyDic = x.Arg<string>();

                if (!dic.ContainsKey(keyDic))
                    return null;

                if (dic[keyDic].GetLength(0) <= 4)
                    return BitConverter.ToInt32(dic[keyDic]).ToString();
                else
                    return BitConverter.ToInt64(dic[keyDic]).ToString();
            });

            repository.WhenForAnyArgs(r => r.Set(key, new byte[1])).Do(p =>
            {
                var arr = p.Arg<byte[]>();
                var s = BitConverter.ToString(arr);
                var keyDic = p.Arg<string>();
                dic[keyDic] = arr;
            });

            repository.WhenForAnyArgs(r => r.Set(key, new byte[1], TimeSpan.FromSeconds(0))).Do(p =>
            {
                var arr = p.Arg<byte[]>();
                var s = BitConverter.ToString(arr);
                var keyDic = p.Arg<string>();
                dic[keyDic] = arr;
            });

            repository.WhenForAnyArgs(r => r.Increment(key)).Do(p =>
            {
                var keyDic = p.Arg<string>();
                int i = BitConverter.ToInt32(dic[keyDic]);
                i++;
                dic[keyDic] = BitConverter.GetBytes(i);
            });
        }
    }
}
