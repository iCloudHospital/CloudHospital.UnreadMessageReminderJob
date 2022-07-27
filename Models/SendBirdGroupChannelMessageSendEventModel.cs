using System.Text.Json.Serialization;

namespace CloudHospital.UnreadMessageReminderJob.Models;

public class SendBirdGroupChannelMessageSendEventModel
{
    public string Category { get; set; }

    public SendBirdUserModel Sender { get; set; }

    public bool Silent { get; set; }

    [JsonPropertyName("sender_ip_addr")]
    public string SenderIpAddress { get; set; }
    [JsonPropertyName("custom_type")]
    public string CustomType { get; set; }

    [JsonPropertyName("mention_type")]
    public string MentionType { get; set; }
    [JsonPropertyName("mentioned_users")]
    public IEnumerable<SendBirdUserModel> MentionedUsers { get; set; } = Enumerable.Empty<SendBirdUserModel>();

    public IEnumerable<SendBirdChannelMemberModel> Members { get; set; } = Enumerable.Empty<SendBirdChannelMemberModel>();

    public string Type { get; set; }

    public SendBirdGroupChannelMessageSendPayloadModel Payload { get; set; }

    public SendBirdGroupChannelModel Channel { get; set; }

    /// <summary>
    /// iOS, Android, JavaScript, .NET, API
    /// </summary>
    public string Sdk { get; set; }
    [JsonPropertyName("app_id")]
    public string AppId { get; set; }
}

public class SendBirdUserModel
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; }
    public string Nickname { get; set; }

    [JsonPropertyName("profile_url")]
    public string ProfileUrl { get; set; }

    public MetadataModel? Metadata { get; set; }
}

public class SendBirdChannelMemberModel : SendBirdUserModel
{
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }
    [JsonPropertyName("is_online")]
    public bool IsOnline { get; set; }
    [JsonPropertyName("is_hidden")]
    public int IsHidden { get; set; }

    /// <summary>
    /// joined, invited
    /// </summary>
    public string State { get; set; }
    [JsonPropertyName("is_blocking_sender")]
    public bool IsBlockingSender { get; set; }
    [JsonPropertyName("is_blocked_by_sender")]
    public bool IsBlockedBySender { get; set; }
    [JsonPropertyName("unread_message_count")]
    public int UnreadMessageCount { get; set; } = 0;
    [JsonPropertyName("total_unread_message_count")]
    public int TotalUnreadMessageCount { get; set; } = 0;
    [JsonPropertyName("channel_unread_message_count")]
    public int ChannelUnreadMessageCount { get; set; } = 0;
    [JsonPropertyName("channel_mention_count")]
    public int ChannelMentionCount { get; set; } = 0;
    [JsonPropertyName("push_enabled")]
    public bool PushEnabled { get; set; }
    [JsonPropertyName("push_trigger_option")]
    public string PushTriggerOption { get; set; }
    [JsonPropertyName("do_not_disturb")]
    public bool DoNotDisturb { get; set; }

}

public class SendBirdGroupChannelMessageSendPayloadModel
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }
    [JsonPropertyName("custom_type")]
    public string CustomType { get; set; }
    public string Message { get; set; }

    public IDictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    public string Data { get; set; }
}

public class SendBirdGroupChannelModel
{
    public string Name { get; set; }
    [JsonPropertyName("channel_url")]
    public string ChannelUrl { get; set; }
    [JsonPropertyName("custom_type")]
    public string CustomType { get; set; }
    [JsonPropertyName("is_distinct")]
    public bool IsDistinct { get; set; }
    [JsonPropertyName("is_public")]
    public bool IsPublic { get; set; }
    [JsonPropertyName("is_super")]
    public bool IsSuper { get; set; }
    [JsonPropertyName("is_ephemeral")]
    public bool IsEphemeral { get; set; }
    [JsonPropertyName("is_discoverable")]
    public bool IsDiscoverable { get; set; }
    public string Data { get; set; }
}

public class MetadataModel
{
    public string Unknown { get; set; }
}