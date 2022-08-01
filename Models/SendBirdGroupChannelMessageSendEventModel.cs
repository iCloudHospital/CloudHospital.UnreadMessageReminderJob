using System.Text.Json.Serialization;
using CloudHospital.UnreadMessageReminderJob.Converters;

namespace CloudHospital.UnreadMessageReminderJob.Models;

/// <summary>
/// Web hook payload of group_channel:message_send
/// <para>
/// https://sendbird.com/docs/chat/v3/platform-api/webhook/events/group-channel#2-group_channel-message_send
/// </para>
/// </summary>
public class SendBirdGroupChannelMessageSendEventModel : SendBirdGroupChannelEventModelBase
{
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

    /// <summary>
    /// One of [MESG, FILE, ADMM]
    /// <para>
    /// <see cref="SendBirdGroupChannelMessageSendEventTypes" />
    /// </para>
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public SendBirdGroupChannelMessagePayloadModel Payload { get; set; }

    public SendBirdGroupChannelModel Channel { get; set; }

    /// <summary>
    /// One of [iOS, Android, JavaScript, .NET, API]
    /// <para>
    /// <see cref="SendBirdGroupChannelMessageSendEventSdks" />
    /// </para>
    /// </summary>
    public string Sdk { get; set; } = string.Empty;
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
    /// One of [joined, invited]
    /// <para>
    /// <see cref="SendBirdChannelMemberStates" />
    /// </para>
    /// </summary>
    public string State { get; set; } = string.Empty;
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

public class SendBirdGroupChannelMessagePayloadModel
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; set; }
    [JsonPropertyName("custom_type")]
    public string CustomType { get; set; }
    // Text message
    public string? Message { get; set; }
    public IDictionary<string, string>? Translations { get; set; } = new Dictionary<string, string>();
    // Text message

    // File message
    public string? Filename { get; set; }
    [JsonPropertyName("content_type")]
    public string? ContentType { get; set; }
    public string? Url { get; set; }
    [JsonPropertyName("content_size")]
    public long? ContentSize { get; set; }
    // File message

    [JsonPropertyName("created_at")]
    [JsonConverter(typeof(DateTimeJsonCoonverter))]
    public DateTime? CreatedAt { get; set; }

    public string? Data { get; set; }
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

// TODO: Verify fields
public class MetadataModel
{
    public string? Unknown { get; set; }

    public string? UserType { get; set; }
}

/// <summary>
/// Group channel message types
/// </summary>
public class SendBirdGroupChannelMessageSendEventTypes
{
    /// <summary>
    /// Text message
    /// </summary>
    public const string TextMessage = "MESG";
    /// <summary>
    /// File message
    /// </summary>
    public const string FileMessage = "FILE";
    /// <summary>
    /// Admin message
    /// </summary>
    public const string AdminMessage = "ADMM";
}

// iOS, Android, JavaScript, .NET, API
public class SendBirdGroupChannelMessageSendEventSdks
{
    public const string iOS = "iOS";
    public const string Android = "Andoird";
    public const string JavaScript = "JavaScript";
    public const string DotNet = ".NET";
    public const string Api = "API";
}

public class SendBirdChannelMemberStates
{

    public const string Joined = "joined";
    public const string Invited = "invited";
}

public class SendBirdGroupChannelReadUpdateModel
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
    [JsonPropertyName("read_ts")]
    [JsonConverter(typeof(DateTimeJsonCoonverter))]
    public DateTime? ReadAt { get; set; }
    [JsonPropertyName("channel_unread_message_count")]
    public int ChannelUnreadMessageCount { get; set; } = 0;
    [JsonPropertyName("total_unread_message_count")]
    public int TotalUnreadMessageCount { get; set; } = 0;
}

public class SendBirdGroupChannelEventCategories
{
    public const string MessageSend = "group_channel:message_send";
    public const string MessageRead = "group_channel:message_read";
}

public class SendBirdSenderUserTypes
{
    public const string ChManager = "CHManager";
    public const string Manager = "Manager";
}

public abstract class SendBirdGroupChannelEventModelBase
{
    /// <summary>
    /// <see cref="SendBirdGroupChannelEventCategories" />
    /// </summary>
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("app_id")]
    public string AppId { get; set; } = string.Empty;
}

public class SendBirdGroupChannelEventModel : SendBirdGroupChannelEventModelBase
{

}