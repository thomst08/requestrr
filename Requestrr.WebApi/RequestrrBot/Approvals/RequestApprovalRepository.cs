using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Requestrr.WebApi.RequestrrBot;

namespace Requestrr.WebApi.RequestrrBot.Approvals
{
    public class RequestApprovalRepository
    {
        private const string ApprovalFileName = "approval_requests.json";
        private static readonly object _lock = new object();
        private Dictionary<int, RequestApprovalRecord> _records = new Dictionary<int, RequestApprovalRecord>();

        public RequestApprovalRepository()
        {
            LoadFromFile();
        }

        public RequestApprovalRecord GetSnapshot(int requestId)
        {
            lock (_lock)
            {
                if (_records.TryGetValue(requestId, out var record))
                {
                    return record.Clone();
                }
            }

            return null;
        }

        public List<RequestApprovalRecord> GetAllSnapshots()
        {
            lock (_lock)
            {
                return _records.Values.Select(x => x.Clone()).ToList();
            }
        }

        public void AddMessage(int requestId, string requesterUsername, ulong requesterUserId, ulong channelId, ulong messageId, bool isAdmin, bool isDirectMessage)
        {
            lock (_lock)
            {
                if (!_records.TryGetValue(requestId, out var record))
                {
                    record = new RequestApprovalRecord
                    {
                        RequestId = requestId,
                        RequesterUsername = requesterUsername ?? string.Empty,
                        RequesterUserId = requesterUserId
                    };
                }
                else if (!string.IsNullOrWhiteSpace(requesterUsername))
                {
                    record.RequesterUsername = requesterUsername;
                }
                else if (record.RequesterUserId == 0 && requesterUserId != 0)
                {
                    record.RequesterUserId = requesterUserId;
                }

                if (!record.Messages.Any(x => x.ChannelId == channelId && x.MessageId == messageId))
                {
                    record.Messages.Add(new RequestApprovalMessageReference
                    {
                        ChannelId = channelId,
                        MessageId = messageId,
                        IsAdmin = isAdmin,
                        IsDirectMessage = isDirectMessage
                    });
                }

                _records[requestId] = record;
                SaveToFile();
            }
        }

        public void Remove(int requestId)
        {
            lock (_lock)
            {
                if (_records.Remove(requestId))
                {
                    SaveToFile();
                }
            }
        }

        private string GetFilePath()
        {
            return Path.Combine(SettingsFile.SettingsFolder, ApprovalFileName);
        }

        private void LoadFromFile()
        {
            lock (_lock)
            {
                try
                {
                    var path = GetFilePath();
                    if (File.Exists(path))
                    {
                        var json = File.ReadAllText(path);
                        var records = JsonConvert.DeserializeObject<List<RequestApprovalRecord>>(json) ?? new List<RequestApprovalRecord>();
                        _records = records.ToDictionary(x => x.RequestId, x => x);
                    }
                    else
                    {
                        _records = new Dictionary<int, RequestApprovalRecord>();
                        SaveToFile();
                    }
                }
                catch
                {
                    _records = new Dictionary<int, RequestApprovalRecord>();
                }
            }
        }

        private void SaveToFile()
        {
            try
            {
                Directory.CreateDirectory(SettingsFile.SettingsFolder);
                var path = GetFilePath();
                File.WriteAllText(path, JsonConvert.SerializeObject(_records.Values.ToList()));
            }
            catch
            {
                // Ignore
            }
        }
    }

    public class RequestApprovalRecord
    {
        public int RequestId { get; set; }
        public string RequesterUsername { get; set; } = string.Empty;
        public ulong RequesterUserId { get; set; }
        public List<RequestApprovalMessageReference> Messages { get; set; } = new List<RequestApprovalMessageReference>();

        public RequestApprovalRecord Clone()
        {
            return new RequestApprovalRecord
                {
                    RequestId = RequestId,
                    RequesterUsername = RequesterUsername,
                    RequesterUserId = RequesterUserId,
                Messages = Messages.Select(x => new RequestApprovalMessageReference
                {
                    ChannelId = x.ChannelId,
                    MessageId = x.MessageId,
                    IsAdmin = x.IsAdmin,
                    IsDirectMessage = x.IsDirectMessage
                }).ToList()
            };
        }
    }

    public class RequestApprovalMessageReference
    {
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsDirectMessage { get; set; }
    }
}
