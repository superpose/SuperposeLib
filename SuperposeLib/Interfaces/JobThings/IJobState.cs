using System;
using System.Collections.Generic;
using SuperposeLib.Models;

namespace SuperposeLib.Interfaces.JobThings
{
    public interface IJobState
    {
        string JobStateTypeName { set; get; }
        DateTime? Started { set; get; }
        DateTime? Ended { set; get; }

        /// <summary>
        /// updated in process
        /// </summary>
        List<JobExecutionStatus> PreviousJobExecutionStatusList { set; get; }
    }
}