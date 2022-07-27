using System.Text.Json.Serialization;

namespace CloudHospital.UnreadMessageReminderJob.Models;

/// <summary>
/// Web hook payload of group_channel:message_read
/// <para>
/// https://sendbird.com/docs/chat/v3/platform-api/webhook/events/group-channel#2-group_channel-message_read
/// </para>
/// </summary>
public class SendBirdGroupChannelMessageReadEventModel : SendBirdGroupChannelEventModelBase
{
    public IEnumerable<SendBirdChannelMemberModel> Members { get; set; } = Enumerable.Empty<SendBirdChannelMemberModel>();

    public SendBirdGroupChannelModel Channel { get; set; }

    [JsonPropertyName("read_updates")]
    public IEnumerable<SendBirdGroupChannelReadUpdateModel> ReadUpdates { get; set; } = Enumerable.Empty<SendBirdGroupChannelReadUpdateModel>();
}
