﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using System.Collections;
using System.Linq;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Commands.Batch.Models;
using Microsoft.Azure.Commands.Batch.Properties;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Commands.Batch.Models
{
    public partial class BatchClient
    {
        /// <summary>
        /// Lists the jobs matching the specified filter options.
        /// </summary>
        /// <param name="options">The options to use when querying for jobs.</param>
        /// <returns>The jobs matching the specified filter options.</returns>
        public IEnumerable<PSCloudJob> ListJobs(ListJobOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            // Get the single job matching the specified id
            if (!string.IsNullOrEmpty(options.JobId))
            {
                WriteVerbose(string.Format(Resources.GBJ_GetById, options.JobId));
                JobOperations jobOperations = options.Context.BatchOMClient.JobOperations;
                CloudJob job = jobOperations.GetJob(options.JobId, additionalBehaviors: options.AdditionalBehaviors);
                PSCloudJob psJob = new PSCloudJob(job);
                return new PSCloudJob[] { psJob };
            }
            // List jobs using the specified filter
            else
            {
                string jobScheduleId = options.JobSchedule == null ? options.JobScheduleId : options.JobSchedule.Id;
                bool filterByJobSchedule = !string.IsNullOrEmpty(jobScheduleId);

                ODATADetailLevel odata = null;
                string verboseLogString = null;
                if (!string.IsNullOrEmpty(options.Filter))
                {
                    verboseLogString = filterByJobSchedule ? Resources.GBJ_GetByOData : string.Format(Resources.GBJ_GetByODataAndJobSChedule, jobScheduleId);
                    odata = new ODATADetailLevel(filterClause: options.Filter);
                }
                else
                {
                    verboseLogString = filterByJobSchedule ? Resources.GBJ_GetNoFilter : string.Format(Resources.GBJ_GetByJobScheduleNoFilter, jobScheduleId);
                }
                WriteVerbose(verboseLogString);

                IPagedEnumerable<CloudJob> jobs = null;
                if (filterByJobSchedule)
                {
                    JobScheduleOperations jobScheduleOperations = options.Context.BatchOMClient.JobScheduleOperations;
                    jobs = jobScheduleOperations.ListJobs(jobScheduleId, odata, options.AdditionalBehaviors);
                }
                else
                {
                    JobOperations jobOperations = options.Context.BatchOMClient.JobOperations;
                    jobs = jobOperations.ListJobs(odata, options.AdditionalBehaviors);      
                }
                Func<CloudJob, PSCloudJob> mappingFunction = j => { return new PSCloudJob(j); };
                return PSPagedEnumerable<PSCloudJob, CloudJob>.CreateWithMaxCount(
                    jobs, mappingFunction, options.MaxCount, () => WriteVerbose(string.Format(Resources.MaxCount, options.MaxCount)));
            }
        }

        /// <summary>
        /// Creates a new job.
        /// </summary>
        /// <param name="parameters">The parameters to use when creating the job.</param>
        public void CreateJob(NewJobParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException("parameters");
            }

            JobOperations jobOperations = parameters.Context.BatchOMClient.JobOperations;
            CloudJob job = jobOperations.CreateJob();

            job.Id = parameters.JobId;
            job.DisplayName = parameters.DisplayName;
            job.Priority = parameters.Priority;

            if (parameters.CommonEnvironmentSettings != null)
            {
                List<EnvironmentSetting> envSettings = new List<EnvironmentSetting>();
                foreach (DictionaryEntry d in parameters.CommonEnvironmentSettings)
                {
                    EnvironmentSetting envSetting = new EnvironmentSetting(d.Key.ToString(), d.Value.ToString());
                    envSettings.Add(envSetting);
                }
                job.CommonEnvironmentSettings = envSettings;
            }

            if (parameters.Constraints != null)
            {
                job.Constraints = parameters.Constraints.omObject;
            }

            if (parameters.JobManagerTask != null)
            {
                Utils.Utils.JobManagerTaskSyncCollections(parameters.JobManagerTask);
                job.JobManagerTask = parameters.JobManagerTask.omObject;
            }

            if (parameters.JobPreparationTask != null)
            {
                Utils.Utils.JobPreparationTaskSyncCollections(parameters.JobPreparationTask);
                job.JobPreparationTask = parameters.JobPreparationTask.omObject;
            }

            if (parameters.JobReleaseTask != null)
            {
                Utils.Utils.JobReleaseTaskSyncCollections(parameters.JobReleaseTask);
                job.JobReleaseTask = parameters.JobReleaseTask.omObject;
            }

            if (parameters.Metadata != null)
            {
                job.Metadata = new List<MetadataItem>();
                foreach (DictionaryEntry d in parameters.Metadata)
                {
                    MetadataItem metadata = new MetadataItem(d.Key.ToString(), d.Value.ToString());
                    job.Metadata.Add(metadata);
                }
            }

            if (parameters.PoolInformation != null)
            {
                Utils.Utils.PoolInformationSyncCollections(parameters.PoolInformation);
                job.PoolInformation = parameters.PoolInformation.omObject;
            }

            WriteVerbose(string.Format(Resources.NBJ_CreatingJob, parameters.JobId));
            job.Commit(parameters.AdditionalBehaviors);
        }

        /// <summary>
        /// Deletes the specified job.
        /// </summary>
        /// <param name="context">The account to use.</param>
        /// <param name="jobId">The id of the job to delete.</param>
        /// <param name="additionBehaviors">Additional client behaviors to perform.</param>
        public void DeleteJob(BatchAccountContext context, string jobId, IEnumerable<BatchClientBehavior> additionBehaviors = null)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentNullException("jobId");
            }

            JobOperations jobOperations = context.BatchOMClient.JobOperations;
            jobOperations.DeleteJob(jobId, additionBehaviors);
        }
    }
}
