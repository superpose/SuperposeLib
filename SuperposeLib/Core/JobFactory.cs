﻿using System;
using System.Collections.Generic;
using System.Threading;
using Superpose.StorageInterface;
using Superpose.StorageInterface.Converters;
using SuperposeLib.Extensions;
using SuperposeLib.Interfaces;
using SuperposeLib.Interfaces.JobThings;
using SuperposeLib.Models;

namespace SuperposeLib.Core
{
    public class JobFactory : IJobFactory
    {
        public JobFactory(IJobStorage jobStorage, IJobConverter jobConverter, ITime time = null)
        {
            time = time ?? new RealTime();
            Time = time;
            if (jobStorage == null) throw new ArgumentNullException(nameof(jobStorage));
            if (jobConverter == null) throw new ArgumentNullException(nameof(jobConverter));
            JobConverter = jobConverter;
            JobStorage = jobStorage;

            MaxWaitSecondsBeforeOverridingCurrentProcessingJob = 2*60;
        }

        public int MaxWaitSecondsBeforeOverridingCurrentProcessingJob { set; get; }
        public IJobConverter JobConverter { set; get; }
        public ITime Time { set; get; }
        public IJobStorage JobStorage { get; set; }

        public string QueueJob(Type jobType)
        {
            return ScheduleJob(jobType, Time.UtcNow);
        }

        public string ScheduleJob(Type jobType, DateTime? scheduleTime)
        {
            var jobId = Guid.NewGuid().ToString();
            var jobLoad = new JobLoad
            {
                TimeToRun = scheduleTime,
                JobTypeFullName = jobType.AssemblyQualifiedName,
                Id = jobId,
                JobStateTypeName = Enum.GetName(typeof (JobStateType), JobStateType.Unknown)
            };
            jobLoad = (JobLoad) new JobStateTransitionFactory().GetNextState(jobLoad, SuperVisionDecision.Unknown);
            JobStorage.JobSaver.SaveNew(JobConverter.Serialize(jobLoad), jobId);
            return jobId;
        }

        private static readonly Dictionary<string,AJob> CachedInstances=new Dictionary<string, AJob>(); 
        public JobLoad InstantiateJobComponent(IJobLoad jobLoad)
        {
            var load = (JobLoad)jobLoad;
            try
            {
                if (CachedInstances.ContainsKey(jobLoad.JobTypeFullName) && CachedInstances[jobLoad.JobTypeFullName]!=null)
                {
                    load.Job = CachedInstances[jobLoad.JobTypeFullName];
                }
                else
                {
                   var job = (AJob)Activator.CreateInstance(Type.GetType(jobLoad.JobTypeFullName));
                    CachedInstances.Add(jobLoad.JobTypeFullName,job);
                    load.Job = job;
                }
                
            }
            catch (Exception e)
            {
                load.Job = new CoreJobThatFails(e, jobLoad.JobTypeFullName);
            }
            return load;
        }

        public JobLoad GetJobLoad(string jobId)
        {
            JobLoad jobLoad;
            try
            {
                var data = JobStorage.JobLoader.LoadJobById(jobId);
                if (data == null)
                {
                    throw new Exception("Unable to load jobLoad :  jobId - " + jobId);
                }
                jobLoad = (JobLoad) JobConverter.Parse(data);

                if (jobLoad == null)
                {
                    throw new Exception("Unable to create jobLoad instance from raw job data : jobId - " + jobId + " : " +
                                        data);
                }

                if (jobLoad.JobTypeFullName == null)
                {
                    throw new Exception("Unable to determine job type :  jobId -" + jobId + " : " + data);
                }
            }
            catch (Exception e)
            {
                throw;
            }

            return jobLoad;
        }

        public IJobLoad ProcessJob(string jobId)
        {
            var jobLoad = GetJobLoad(jobId);

            if (jobLoad == null) return null;
            var now = Time.UtcNow;
            var aboutTime = jobLoad.TimeToRun != null && now >= jobLoad.TimeToRun.Value;
            var itsTimeToProcess = jobLoad.TimeToRun == null || aboutTime;

            var jobIsInQueue = jobLoad.JobStateTypeName == Enum.GetName(typeof (JobStateType), JobStateType.Queued);

            var canOverideCurrentlyProcessing = jobLoad.JobStateTypeName ==
                                                Enum.GetName(typeof (JobStateType), JobStateType.Processing) &&
                                                (jobLoad.Started == null ||
                                                 ((Time.UtcNow - jobLoad.Started.Value).TotalMinutes >
                                                  MaxWaitSecondsBeforeOverridingCurrentProcessingJob));

            var canProcess = itsTimeToProcess && (jobIsInQueue || canOverideCurrentlyProcessing);

            if (!canProcess) return null;

            jobLoad.JobStateTypeName = Enum.GetName(typeof (JobStateType), JobStateType.Queued);
            jobLoad.Started = Time.UtcNow;

            jobLoad = (JobLoad) new JobStateTransitionFactory().GetNextState(jobLoad, SuperVisionDecision.Unknown);

            //persist
            var updateStorageToProcessing = false;
            try
            {
                JobStorage.JobSaver.Update(JobConverter.Serialize(jobLoad), jobLoad.Id);
                updateStorageToProcessing = true;
            }
            catch (Exception e)
            {
                //failed to update after load
                //throw e;
            }
            if (!updateStorageToProcessing) return null;

            jobLoad = InstantiateJobComponent(jobLoad);
            var result = jobLoad.Job.RunJob();
            jobLoad.PreviousJobExecutionStatusList.Add(result.IsSuccessfull
                ? JobExecutionStatus.Passed
                : JobExecutionStatus.Failed);

            if (!result.IsSuccessfull)
            {
                try
                {
                    result.SuperVisionDecision = jobLoad.Job.Supervision(result.Exception,
                        jobLoad.HistoricFailureCount());
                }
                catch (Exception se)
                {
                    result.SuperVisionDecision = SuperVisionDecision.Fail;
                    result.SuperVisionException = se;
                }
            }


            jobLoad = (JobLoad) new JobStateTransitionFactory().GetNextState(jobLoad, result.SuperVisionDecision);

            try
            {
                jobLoad.Job = null;
                JobStorage.JobSaver.Update(JobConverter.Serialize(jobLoad), jobLoad.Id);

                return jobLoad;
            }
            catch (Exception e)
            {
                //failed to update after processing
                // throw e;
            }
            return null;
        }
    }

    public class CoreJobThatFails : AJob
    {
        private Exception Exception { set; get; }
        private string JobTypeFullName { set; get; }

        public override SuperVisionDecision Supervision(Exception reaon, int totalNumberOfHistoricFailures)
        {
            return SuperVisionDecision.Fail;
        }

        public CoreJobThatFails(Exception exception, string jobTypeFullName)
        {
            Exception = exception;
            JobTypeFullName = jobTypeFullName;
        }
        protected override void Execute()
        {
            throw new Exception("Unable to run job "+ JobTypeFullName,Exception);
        }
    }
}