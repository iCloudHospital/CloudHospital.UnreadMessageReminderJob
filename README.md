## Azure Functions

* HTTP Trigger
* Timer Trigger
* Queue Trigger

### Http triggers

Insert wrapped event data from HTTP request body into table storage.

HTTP 요청 본문을 테이블 스토리지에 입력합니다.

POST: /api/GroupChannelMessageSendWebHook

payload: [group_channel:message_send event](https://sendbird.com/docs/chat/v3/platform-api/webhook/events/group-channel#2-group_channel-message_send)

Removes queued entries in table storage with the channel.channel_url value in the HTTP request body.

HTTP 요청 본문의 channel.channel_url 값으로 테이블 스토리지에 대기중인 항목을 제거합니다.

POST: /api/GroupChannelMessageReadWebHook

payload: [group_channel:message_read event](https://sendbird.com/docs/chat/v3/platform-api/webhook/events/group-channel#2-group_channel-message_read)


### Timer trigger

Enqueue event data that inserted into table storage 5 minutes before.

5분전에 테이블 스토리지에 입력된  이벤트 데이터를 대기열(Queue storage)에 넣습니다.

### Queue trigger

Process dequeued event data.

대기열에서 빠져나온 이벤트 데이터를 처리합니다.

## Settings

| Environment Variable |  Required | Note |
| :------------------: |  :------: | :--- |
| Stage                | ✅        | Application staging |
| AzureWebJobsStorage  | ✅        | Azure Storage Account connection string |
| Database             | ✅        | Azure SQL Database connection string |
| QueueName            | ✅        | Queue name base; Actual queue name is `$(queuename)$(stage)`; e.g.) QueueName: myqueue, Stage: prod ➡️ myqueueprod |
| TableName            | ✅        | Table name base; Actual table name is `$(tablename)$(stage)`; e.g.) TableName: mytable, Stage: prod ➡️ mytableprod |
| TimerSchedule        | ✅        | Timer schedule; Cron style schedule; e.g.) Trigger every 5 minutes. ➡️ 0 */5 * * * * |
| UnreadDelayMinutes   | ✅        | Unread message criteria minutes. |
| SendGridApiKey       | ✅        | Sendgrid api key |
| SendGridSenderEmail  | ✅        | Sender email address |
| SendGridSenderName   | ✅        | Sender display name |
| Debug                |           | Show more information |


```json
{
    "Stage": "<stage>",
    "AzureWebJobsStorage": "<Azure Storage Account connection string; with queue-endpoint, table-endpoint>",
    "Database": "<Azure SQL Database connection string>",
    "QueueName": "<queue name base>",
    "TableName": "<table name base>",
    "TimerSchedule": "<timer schedume; 0 */1 * * * *>",
    "UnreadDelayMinutes": <Unread message criteria minutes; 5>,
    "SendGridApiKey": "<sendgrid api key>",
    "SendGridSenderEmail": "<sender email address>",
    "SendGridSenderName": "<sender name>",
    "Debug": false,
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
}
```

```bash
$ echo '{
  "IsEncrypted": false,
  "Values": {
    "Stage": "<stage>",
    "AzureWebJobsStorage": "<Azure Storage Account connection string; with queue-endpoint, table-endpoint>",
    "Database": "<Azure SQL Database connection string>",
    "QueueName": "sendbirdmessages",
    "TableName": "sendbirdmessages",
    "TimerSchedule": "0 */5 * * * *",
    "UnreadDelayMinutes": 5,
    "SendGridApiKey": "<sendgrid api key>",
    "SendGridSenderEmail": "<sender email address>",
    "SendGridSenderName": "<sender name>",
    "Debug": false,
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  }
}' >> local.settings.json
```

## Debug

### Install Azure Functions Core Tools

[Work with Azure Functions Core Tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=v4%2Cmacos%2Ccsharp%2Cportal%2Cbash)

on macOS

```bash
$ brew tap azure/functions
$ brew install azure-functions-core-tools@4
# if upgrading on a machine that has 2.x or 3.x installed:
$ brew link --overwrite azure-functions-core-tools@4
```

### Install Azurite

[Azurite](https://github.com/Azure/Azurite)

```bash
$ npm i -g azurite
```

### Install Microsoft Storage Account Explorer

[Azure Storage Explorer](https://azure.microsoft.com/en-us/features/storage-explorer/)

### Start local development environment

```bash
$ mkdir ~/.azurite # if ~/.azurite directory does not exists 
$ azurite --silent --location ~/.azurite --debug ~/.azurite/debug.log
```

### Open Microsoft Storage Account Explorer

1. Select Emulator on left hand side pane.
1. You can find connection string of emulator

### Edit your local.settings.json file

AzureWebJobsStorage is Azure Storage Account connection string.

> Azure Storage Account connection string must have queue endpoint and table endpoint.

### Start functions app on local

```bash
$ func start
```

### Trigger with HTTP request

POST: http://localhost:7071/api/GroupChannelMessageSendWebHook

```json
{
    "category": "group_channel:message_send",
    "sender": {
        "user_id": "Jeff",
        "nickname": "Oldies but goodies",
        "profile_url": "https://sendbird.com/main/img/profiles/profile_38_512px.png",
        "metadata": {}
    },
    "silent": false,
    "sender_ip_addr": "xxx.xxx.xxx.xx",
    "custom_type": "",
    "mention_type": "users",
    "mentioned_users": [],
    "members": [
        {
            "user_id": "Jeff",
            "nickname": "Oldies but goodies",
            "profile_url": "https://sendbird.com/main/img/profiles/profile_38_512px.png",
            "is_active": true,
            "is_online": false,
            "is_hidden": 0,
            "state": "joined",  
            "is_blocking_sender": false,
            "is_blocked_by_sender": false,
            "unread_message_count": 16,
            "total_unread_message_count": 16,
            "channel_unread_message_count": 5,
            "channel_mention_count": 0,
            "push_enabled": false,
            "push_trigger_option": "default",
            "do_not_disturb": false,
            "metadata": {}
        }
    ],
    "type": "MESG", 
    "payload": {
        "message_id": 238303376,
        "custom_type": "",
        "message": "Webhook simulation",
        "translations": {
            "en": "",
            "de": ""
        },
        "created_at": 1540798555343,
        "data": ""
    },
    "channel": {
        "name": "Sendbird engineers talking room",
        "channel_url": "sendbird_group_channel_47226288_21c0d617e45a7db4e12a7f5efdb4df4743b11c16",
        "custom_type": "business",
        "is_distinct": false,
        "is_public": false,
        "is_super": false,
        "is_ephemeral": false,
        "is_discoverable": false,
        "data": ""
    },
    "sdk": "API",
    "app_id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```


POST: http://localhost:7071/api/GroupChannelMessageReadWebHook

```json
{
    "category": "group_channel:message_read",
    "members": [   
        {
            "user_id": "John",
            "nickname": "Sendbirdian",
            "profile_url": "https://sendbird.com/main/img/profiles/profile_24_512px.png",
            "is_active": true,
            "is_online": false,
            "is_hidden": 0,
            "state": "joined", 
            "unread_message_count": 0,
            "total_unread_message_count": 3,
            "channel_unread_message_count": 0,
            "channel_mention_count": 0,
            "push_enabled": false,
            "push_trigger_option": "default",
            "do_not_disturb": false,
            "metadata": {}
        }
    ],
    "channel": {
        "name": "Let's make a good company",
        "channel_url": "sendbird_group_channel_47226288_21c0d617e45a7db4e12a7f5efdb4df4743b11c16",
        "custom_type": "business",
        "is_distinct": false,
        "is_public": false,
        "is_super": false,
        "is_ephemeral": false,
        "is_discoverable": false,
        "data": ""
    },
    "read_updates": [
        {
            "user_id": "John",
            "read_ts": 1540864257418,
            "channel_unread_message_count": 0,
            "total_unread_message_count": 3
        }
    ],
    "app_id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
```