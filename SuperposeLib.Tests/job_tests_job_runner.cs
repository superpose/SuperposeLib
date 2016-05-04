using System;
using System.Linq;
using Microsoft.Owin.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Owin;
using SuperposeLib.Core;
using SuperposeLib.Extensions;
using SuperposeLib.Interfaces.Converters;
using SuperposeLib.Interfaces.JobThings;
using SuperposeLib.Interfaces.Storage;
using SuperposeLib.Models;
using SuperposeLib.Services.DefaultConverter;
using SuperposeLib.Services.InMemoryStorage;
using SuperposeLib.Tests.Jobs;

namespace SuperposeLib.Tests
{

    [TestClass]
    public class job_tests_job_runner
    {
        public class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                app.UseSuperposeLibInMemoryStorageFactory();
                app.UseSuperposeLibServerMiddleware();
            }
        }

        [TestMethod]
        public void test_owin()
        {
            using (WebApp.Start<Startup>("http://localhost:12345"))
            {
                using (var storage = SuperposeLibServerMiddleware.StorageFactory.CreateJobStorage())
                {
                    var converter = new DefaultJobConverterFactory().CretateConverter();
                    IJobFactory factory = new JobFactory(storage, converter);
                    var jobId = factory.QueueJob(typeof(TestJobThatPassesAfter2Tryals));
                    var passed = false;

                    var iteri = 10;
                    while (iteri > 0 && !passed)
                    {
                        iteri--;

                        System.Threading.Thread.Sleep(1000);
                        try
                        {
                            var existingResult = factory.GetJobLoad(jobId);
                            Assert.AreEqual(existingResult.JobStateType, JobStateType.Successfull);
                            var statistics = factory.JobStorage.JobLoader.GetJobStatistics();
                            Assert.AreEqual(statistics.TotalSuccessfullJobs, 1);
                            passed = true;
                        }
                        catch (Exception e)
                        {


                        }
                    }
                    Assert.IsTrue(passed);
                }
            }
        }

        [TestMethod]
        public void test_bare_bone()
        {
            IJobStoragefactory storageFactory = new InMemoryJobStoragefactory();
            IJobConverterFactory converterFactory = new DefaultJobConverterFactory();
            var converter = converterFactory.CretateConverter();
            using (var storage = storageFactory.CreateJobStorage())
            {
                IJobFactory factory = new JobFactory(storage, converter);
                var jobId = factory.QueueJob(typeof(TestJobThatPassesAfter2Tryals));
                IJobRunner runner = new JobRunner(storage, converter);
                runner.Run();
                var existingResult = factory.GetJobLoad(jobId);
                Assert.AreEqual(existingResult.JobStateType, JobStateType.Successfull);
                var statistics = factory.JobStorage.JobLoader.GetJobStatistics();
                Assert.AreEqual(statistics.TotalSuccessfullJobs, 1);
            }
        }


        [TestMethod]
        public void process_a_queued_job()
        {
            IJobStoragefactory storageFactory = new InMemoryJobStoragefactory();
            IJobConverterFactory converterFactory = new DefaultJobConverterFactory();
            var converter = converterFactory.CretateConverter();
            using (var storage = storageFactory.CreateJobStorage())
            {
                IJobFactory factory = new JobFactory(storage, converter);
                var jobId = factory.QueueJob(typeof(TestJobThatPassesAfter2Tryals));

                var runner = new JobRunner(storage, converter);
                var result = runner.Run();

                Assert.IsTrue(result);
                var existingResult = factory.GetJobLoad(jobId);

                Assert.AreEqual(existingResult.JobType, typeof(TestJobThatPassesAfter2Tryals));
                Assert.AreEqual(existingResult.JobId, jobId);
                Assert.IsNotNull(existingResult);
                Assert.AreEqual(existingResult.PreviousJobExecutionStatusList.Count(x => x == JobExecutionStatus.Passed), 0);
                Assert.AreEqual(existingResult.PreviousJobExecutionStatusList.Last(), JobExecutionStatus.Failed);
                Assert.AreEqual(existingResult.JobStateType, JobStateType.Successfull);

                var statistics = factory.JobStorage.JobLoader.GetJobStatistics();
                Assert.AreEqual(statistics.TotalNumberOfJobs, 1);
                Assert.AreEqual(statistics.TotalSuccessfullJobs, 1);
                Assert.AreEqual(statistics.TotalFailedJobs, 0);
                Assert.AreEqual(statistics.TotalProcessingJobs, 0);

            }
        }

        [TestMethod]
        public void process_a_queued_job2()
        {
            IJobStoragefactory storageFactory = new InMemoryJobStoragefactory();
            IJobConverterFactory converterFactory = new DefaultJobConverterFactory();
            var converter = converterFactory.CretateConverter();
            using (var storage = storageFactory.CreateJobStorage())
            {
                IJobFactory factory = new JobFactory(storage, converter);
                const int noOfJobs = 10;
                for (var j = 1; j <= noOfJobs; j++)
                {
                    var jobId = factory.QueueJob(typeof(TestJobThatPassesAfter2Tryals));

                    var runner = new JobRunner(storage, converter);
                    var result = runner.Run();
                    Assert.IsTrue(result);
                    var existingResult = factory.GetJobLoad(jobId);

                    Assert.AreEqual(existingResult.JobType, typeof(TestJobThatPassesAfter2Tryals));
                    Assert.AreEqual(existingResult.JobId, jobId);
                    Assert.IsNotNull(existingResult);
                    Assert.AreEqual(existingResult.PreviousJobExecutionStatusList.Count(x => x == JobExecutionStatus.Passed), 0);
                    Assert.AreEqual(existingResult.PreviousJobExecutionStatusList.Last(), JobExecutionStatus.Failed);
                    Assert.AreEqual(existingResult.JobStateType, JobStateType.Successfull);


                    var statistics = factory.JobStorage.JobLoader.GetJobStatistics();
                    Assert.AreEqual(statistics.TotalNumberOfJobs, j);
                    Assert.AreEqual(statistics.TotalSuccessfullJobs, j);
                    Assert.AreEqual(statistics.TotalFailedJobs, 0);
                    Assert.AreEqual(statistics.TotalProcessingJobs, 0);
                }
            }
        }
        [TestMethod]
        public void process_a_queued_job_generic_time()
        {
            IJobStoragefactory storageFactory = new InMemoryJobStoragefactory();
            IJobConverterFactory converterFactory = new DefaultJobConverterFactory();
            var converter = converterFactory.CretateConverter();
            using (var storage = storageFactory.CreateJobStorage())
            {
                IJobFactory factory = new JobFactory(storage, converter);
                const int noOfJobs = 10;
                for (var j = 1; j <= noOfJobs; j++)
                {
                    var jobId = factory.QueueJob(typeof(TestJobThatPassesAfter2Tryals));
                    const int noOfCircles = 10;
                    for (var i = 1; i <= noOfCircles; i++)
                    {
                        var runner = new JobRunner(storage, converter);
                        var result = runner.Run();
                        Assert.IsTrue(result);
                        var existingResult = factory.GetJobLoad(jobId);

                        Assert.AreEqual(existingResult.JobType, typeof(TestJobThatPassesAfter2Tryals));
                        Assert.AreEqual(existingResult.JobId, jobId);
                        Assert.IsNotNull(existingResult);
                        Assert.AreEqual(existingResult.PreviousJobExecutionStatusList.Count(x => x == JobExecutionStatus.Passed), 0);
                        Assert.AreEqual(existingResult.PreviousJobExecutionStatusList.Last(), JobExecutionStatus.Failed);
                        Assert.AreEqual(existingResult.JobStateType, JobStateType.Successfull);
                    }

                    var statistics = factory.JobStorage.JobLoader.GetJobStatistics();
                    Assert.AreEqual(statistics.TotalNumberOfJobs, j);
                    Assert.AreEqual(statistics.TotalSuccessfullJobs, j);
                    Assert.AreEqual(statistics.TotalFailedJobs, 0);
                    Assert.AreEqual(statistics.TotalProcessingJobs, 0);
                }
            }
        }


    }
}