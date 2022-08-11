## UnreadMessageReminder

### Http triggers

POST: /api/GroupChannelMessageWebHook

Provide two functions depending on the event type of the payload.

payload: [group_channel:message_send event](https://sendbird.com/docs/chat/v3/platform-api/webhook/events/group-channel#2-group_channel-message_send)

Insert wrapped event data from HTTP request body into table storage.

HTTP 요청 본문을 테이블 스토리지에 입력합니다.

payload: [group_channel:message_read event](https://sendbird.com/docs/chat/v3/platform-api/webhook/events/group-channel#2-group_channel-message_read)

Removes queued entries in table storage with the channel.channel_url value in the HTTP request body.

HTTP 요청 본문의 channel.channel_url 값으로 테이블 스토리지에 대기중인 항목을 제거합니다.


### Timer trigger

Enqueue event data that inserted into table storage 5 minutes before.

5분전에 테이블 스토리지에 입력된  이벤트 데이터를 대기열(Queue storage)에 넣습니다.

### Queue trigger

Process dequeued event data.

대기열에서 빠져나온 이벤트 데이터를 처리합니다.