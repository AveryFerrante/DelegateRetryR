using DelegateRetry.Exceptions;
using DelegateRetry.WorkRunner;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DelegateRetry.Tests
{
    public class DelegateRetryTests
    {
        [Fact]
        public async void ensure_action_is_executed_after_failures_under_retry_count()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new ArgumentNullException())
                .WithFailureCount(2)
                .PerformsWork(null)
                .Build();
            IDelegateRetryR RetryHelper = new DelegateRetryR();

            var exception = await Record.ExceptionAsync(() => RetryHelper.RetryWorkAsync<ArgumentNullException>(functionToTest, null, (int retryCount) => retryCount < 3));
            Assert.Null(exception);
        }

        [Fact]
        public async void ensure_action_returns_error_after_failures_over_retry_count()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new InvalidCastException())
                .WithFailureCount(10)
                .PerformsWork(null)
                .Build();
            IDelegateRetryR RetryHelper = new DelegateRetryR();

            await Assert.ThrowsAsync<InvalidCastException>(() => RetryHelper.RetryWorkAsync<InvalidCastException>(functionToTest, null, (int retryCount) => retryCount < 3));
        }

        [Fact]
        public async void ensure_action_returns_error_if_different_than_expected_error()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new InvalidCastException())
                .WithFailureCount(1)
                .PerformsWork(null)
                .Build();
            IDelegateRetryR RetryHelper = new DelegateRetryR();

            await Assert.ThrowsAsync<InvalidCastException>(() => RetryHelper.RetryWorkAsync<ArgumentNullException>(functionToTest, null, (int retryCount) => retryCount < 3));
        }

        [Fact]
        public async void ensure_invalid_return_type_error_is_thrown_if_actual_return_type_differs_from_expected()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new InvalidCastException())
                .WithFailureCount(2)
                .PerformsWork(() => 15)
                .Build();
            IDelegateRetryR RetryHelper = new DelegateRetryR();

            await Assert.ThrowsAsync<InvalidReturnTypeException>(() => RetryHelper.RetryWorkAsync<InvalidCastException, string>(functionToTest, null));
        }

        [Fact]
        public async void ensure_invalid_return_type_error_is_thrown_if_work_doesnt_return_task_when_using_async_work_function()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new InvalidCastException())
                .WithFailureCount(2)
                .PerformsWork(() => 10)
                .Build();
            IDelegateRetryR RetryHelper = new DelegateRetryR();

            await Assert.ThrowsAsync<InvalidReturnTypeException>(() => RetryHelper.RetryAsyncWorkAsync<InvalidCastException, string>(functionToTest, null));
        }

        [Fact]
        public async void ensure_proper_return_value_is_given_after_failures_under_retry_count()
        {
            var returnValue = 10;
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new ArgumentNullException())
                .WithFailureCount(2)
                .PerformsWork(() => returnValue)
                .BuildWithReturnType<int>();
            IDelegateRetryR RetryHelper = new DelegateRetryR();

            var result = await RetryHelper.RetryWorkAsync<ArgumentNullException, int>(functionToTest, null, (int retryCount) => retryCount < 3);
            Assert.Equal(returnValue, result);
        }

        [Fact]
        public async void ensure_proper_return_value_is_given_when_parameter_is_passed_after_failures_under_retry_count()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new ArgumentNullException())
                .WithFailureCount(2)
                .PerformsWork((string a) => a + "123")
                .BuildWithExpectedParamsAndReturnType<string, string>();
            IDelegateRetryR RetryHelper = new DelegateRetryR();

            var result = await RetryHelper.RetryWorkAsync<ArgumentNullException, string>(functionToTest, new object[] { "abc" }, (int retryCount) => retryCount < 3);
            var expected = "abc123";
            Assert.Equal(expected, result);
        }

        [Fact]
        public async void ensure_proper_return_value_is_given_when_multiple_parameters_are_passed_after_failures_under_retry_count()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new ArgumentNullException())
                .WithFailureCount(2)
                .PerformsWork((string a, int b) => $"{a}{b}123")
                .BuildWithExpectedParamsAndReturnType<string, int, string>();
            IDelegateRetryR RetryHelper = new DelegateRetryR();

            var result = await RetryHelper.RetryWorkAsync<ArgumentNullException, string>(functionToTest, new object[] { "abc", 15 }, (int retryCount) => retryCount < 3);
            Assert.Equal("abc15123", result);
        }

        [Fact]
        public async void ensure_configured_retry_count_is_used_if_none_is_provided_by_the_caller()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new ArgumentNullException())
                .WithFailureCount(6)
                .PerformsWork((string a) => $"{a}123")
                .BuildWithExpectedParamsAndReturnType<string, string>();
            IDelegateRetryR RetryHelper = new DelegateRetryR(new DelegateRetryRConfiguration() { RetryConditional = (int retryCount) => retryCount < 7 });

            var result = await RetryHelper.RetryWorkAsync<ArgumentNullException, string>(functionToTest, new object[] { "abc" });
            Assert.Equal("abc123", result);
        }

        [Fact]
        public async void ensure_configured_retry_delay_is_used_if_none_is_provided_by_the_caller()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new ArgumentNullException())
                .WithFailureCount(4)
                .PerformsWork((string a) => a + "123")
                .BuildWithExpectedParamsAndReturnType<string, string>();
            IDelegateRetryR RetryHelper = new DelegateRetryR(new DelegateRetryRConfiguration() { RetryDelay = (int retryCount) => retryCount * 1000 });

            var watch = new Stopwatch();
            watch.Start();
            var result = await RetryHelper.RetryWorkAsync<ArgumentNullException, string>(functionToTest, new object[] { "abc" }, (int retryCount) => retryCount < 5);
            watch.Stop();

            var expectedMinimumInMs = 0 + 2000 + 3000 + 4000 + 5000;
            Assert.True(watch.ElapsedMilliseconds > expectedMinimumInMs,
                $"elapsed execution time (in ms) of {watch.ElapsedMilliseconds} is less than expected minimum of {expectedMinimumInMs}");
        }

        [Fact]
        public async void ensure_async_work_returns_proper_inner_value_after_failures_under_retry_count()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new ArgumentNullException())
                .WithFailureCount(2)
                .PerformsWork((string a) => Task.Run(() => { Thread.Sleep(1000); return a; }))
                .BuildWithExpectedParamsAndReturnType<string, Task<string>>();
            IDelegateRetryR RetryHelper = new DelegateRetryR(new DelegateRetryRConfiguration() { RetryConditional = (int retryCount) => retryCount < 5 });

            var result = await RetryHelper.RetryAsyncWorkAsync<ArgumentNullException, string>(functionToTest, new object[] { "abc" });
            Assert.Equal("abc", result);
        }

        [Fact]
        public async void ensure_async_work_fails_properly_if_inner_task_fails()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new OutOfMemoryException())
                .WithFailureCount(0)
                .PerformsWork((string a) => Task.Run(() => { Thread.Sleep(1000); throw new ArgumentNullException(); }))
                .BuildWithExpectedParamsAndReturnType<string, Task>();
            Predicate<int> doNotRetryConditional = (int retryCount) => retryCount < 0;
            IDelegateRetryR RetryHelper = new DelegateRetryR(new DelegateRetryRConfiguration() { RetryConditional = doNotRetryConditional });

            await Assert.ThrowsAsync<ArgumentNullException>(() => RetryHelper.RetryAsyncWorkAsync<InvalidCastException>(functionToTest, new object[] { "abc" }));
        }

        [Fact]
        public async void ensure_generalized_exception_catches_more_specific_exceptions_and_keeps_processing()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new AggregateException())
                .WithFailureCount(3)
                .PerformsWork(() => 10)
                .BuildWithReturnType<int>();
            IDelegateRetryR RetryHelper = new DelegateRetryR(new DelegateRetryRConfiguration() { RetryConditional = (int retryCount) => retryCount < 5 });

            var result = await RetryHelper.RetryWorkAsync<Exception, int>(functionToTest, parameters: null);
            Assert.Equal(10, result);
        }

        [Fact]
        public async void ensure_parameter_count_mismatch_exception_is_thrown_if_counts_do_not_match()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new InvalidCastException())
                .WithFailureCount(0)
                .PerformsWork((string a) => a)
                .BuildWithExpectedParamsAndReturnType<string, string>();
            IDelegateRetryR RetryHelper = new DelegateRetryR();

            await Assert.ThrowsAsync<ProvidedParameterCountMismatchException>(() =>
                RetryHelper.RetryWorkAsync<InvalidCastException>(functionToTest, new object[] { "abc", 1234 }));
        }

        [Fact]
        public async void ensure_invalid_parameter_type_exception_is_thrown_if_types_do_not_match()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new InvalidCastException())
                .WithFailureCount(0)
                .PerformsWork((string a, string b) => a + b)
                .BuildWithExpectedParams<string, string>();
            IDelegateRetryR RetryHelper = new DelegateRetryR();

            await Assert.ThrowsAsync<InvalidProvidedParameterTypeException>(() =>
                RetryHelper.RetryWorkAsync<InvalidCastException>(functionToTest, new object[] { "abc", 1234 }));
        }

        [Fact]
        public async void ensure_subclasses_are_allowed_parameter_types()
        {
            var functionToTest = FailingDelegateBuilder
                .WillThrow(new ArgumentNullException())
                .WithFailureCount(2)
                .PerformsWork((WorkRunnerBase generalizedParam) => 50)
                .BuildWithExpectedParamsAndReturnType<WorkRunnerBase, int>();
            IDelegateRetryR RetryHelper = new DelegateRetryR();

            var specificParam = new WorkRunnerWithoutReturn(() => { }, null, false);
            var result = await RetryHelper.RetryWorkAsync<ArgumentNullException, int>(functionToTest, new object[] { specificParam });
            Assert.Equal(50, result);
        }
    }
}