using System;
using System.Collections.Generic;

namespace VaultDataAPISampleApp.Models
{
    public class CursorPaginationResponse<T>
    {
        public CursorPagination Pagination { get; set; }
        public List<T> Results { get; set; }
    }

    public class CursorPagination
    {
        public int Limit { get; set; }
        public string NextCursorState { get; set; }
        public string NextUrl { get; set; }
    }

    public class ExtSyncTaskResponse
    {
        public string Id { get; set; }
        public string EntityId { get; set; }
        public string EntityClassId { get; set; }
        public string ConfigId { get; set; }
        public string WorkflowType { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public Dictionary<string, string> Params { get; set; }
        public bool ExecuteImmediately { get; set; }
        public string PredecessorTaskId { get; set; }
        public long VaultId { get; set; }
        public long UserId { get; set; }
        public DateTime? CreateDate { get; set; }
    }

    public class CreateExtSyncTaskRequest
    {
        public string EntityId { get; set; }
        public string EntityClassId { get; set; }
        public string ConfigId { get; set; }
        public string WorkflowType { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> Params { get; set; }
        public bool ExecuteImmediately { get; set; }
        public string PredecessorTaskId { get; set; }
    }

    public class FindExtSyncTasksByEntityIdsRequest
    {
        public List<string> EntityIds { get; set; }
        public string WorkflowType { get; set; }
    }

    public class UpdateExtSyncTaskResultRequest
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
