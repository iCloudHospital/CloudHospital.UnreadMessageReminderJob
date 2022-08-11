## Azure Functions

* HTTP Trigger
* Timer Trigger
* Queue Trigger

## Features

* [Unread message reminder job](./README-UnreadMessageReminder.md)
* [Calling reminder job](./README-CallingReminder.md)

## Settings

| Environment Variable |  Required | Default | Note |
| :------------------: |  :------: | :---- | :--- |
| Stage                | ✅        | - | Application staging |
| LogoImageUrl         | ✅        | - | Default logo image url |
| CloudHospitalBaseUrl | ✅        | - | Default web application  url |
| AzureWebJobsStorage  | ✅        | - | Azure Storage Account connection string |
| Database             | ✅        | - | Azure SQL Database connection string |
| UnreadMessageReminderQueueName            | ✅        | - | Queue name base for unread message reminder job; Actual queue name is `$(UnreadMessageReminderQueueName)$(Stage)`; e.g.) QueueName: myqueue, Stage: prod ➡️ myqueueprod |
| UnreadMessageReminderTableName            | ✅        | - | Table name base for unread message reminder job; Actual table name is `$(UnreadMessageReminderTableName)$(Stage)`; e.g.) TableName: mytable, Stage: prod ➡️ mytableprod |
| UnreadMessageReminderTimerSchedule        | ✅        | - | Timer schedule for unread message reminder job; Cron style schedule; e.g.) Trigger every 5 minutes. ➡️ 0 */5 * * * * |
| UnreadDelayMinutes   | ✅        | - | Unread message criteria minutes. |
| UnreadMessageReminderSendGridTemplateId | ✅ | - | Send grid teamplate id |
| CallingReminderQueueName | ✅     | - | Queue name base for Calling Reminder job; Actual queue name is `$(CallingReminderQueueName)$(Stage)`; |
| CallingReminderTableName | ✅     | - | Table name base for Calling Reminder job; Actual table name is `$(CallingReminderTableName)$(Stage)` |
| CallingReminderTimerSchedule | ✅ | - | Timer schedule for Calling Reminder job |
| CallingReminderBasis | ✅ | - | Remind basis minutes |
| SendGridApiKey       | ✅        | - | Sendgrid api key |
| SendGridSenderEmail  | ✅        | - | Sender email address |
| SendGridSenderName   | ✅        | - | Sender display name |
| SendBirdApiKey       | ✅        | - | Sendbird api key (master only) |
| NotificationApiOidcName | | - | Notification api OIDC name |
| NotificationApiName | | - | Notification api Api name |
| NotificationApiBaseUrl | | - | Notification api base url |
| NotificationApiEnabled | ✅ | false |  Notification api uses or not |
| NotificationHubConnectionString | ✅ | - | Azure Notification Hub connection string |
| NotificationHubName | ✅ | - | Azure Notification Hub name |
| Debug                |           | false | Show more information |


```json
{
    "Stage": "<stage>",
    "AzureWebJobsStorage": "<Azure Storage Account connection string; with queue-endpoint, table-endpoint>",
    "Database": "<Azure SQL Database connection string>",
    "UnreadMessageReminderQueueName": "<queue name base>",
    "UnreadMessageReminderTableName": "<table name base>",
    "UnreadMessageReminderTimerSchedule": "0 */1 * * * *",
    "UnreadDelayMinutes": 2,
    "CallingReminderQueueName": "<queue name base>",
    "CallingReminderTableName": "<table name base>",
    "CallingReminderTimerSchedule": "0 */1 * * * *",
    "CallingReminderBasis": 30,
    "SendBirdApiKey": "<sendbird api key; MUST USE master api key>",
    "SendGridApiKey": "<sendgrid api key>",
    "SendGridSenderEmail": "<sender email address>",
    "SendGridSenderName": "<sender name>",
    "NotificationApiOidcName": "<oidc>",
    "NotificationApiName": "<name>",
    "NotificationApiBaseUrl": "<base url>",
    "NotificationApiEnabled": false,
    "NotificationHubConnectionString": "<Azure notification hub connection string>",
    "NotificationHubName": "<Azure notification hub name>",
    "Debug": true,
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
    "UnreadMessageReminderQueueName": "<queue name base>",
    "UnreadMessageReminderTableName": "<table name base>",
    "UnreadMessageReminderTimerSchedule": "0 */1 * * * *",
    "UnreadDelayMinutes": 2,
    "CallingReminderQueueName": "<queue name base>",
    "CallingReminderTableName": "<table name base>",
    "CallingReminderTimerSchedule": "0 */1 * * * *",
    "CallingReminderBasis": 30,
    "SendBirdApiKey": "<sendbird api key; MUST USE master api key>",
    "SendGridApiKey": "<sendgrid api key>",
    "SendGridSenderEmail": "<sender email address>",
    "SendGridSenderName": "<sender name>",
    "NotificationApiOidcName": "<oidc>",
    "NotificationApiName": "<name>",
    "NotificationApiBaseUrl": "<base url>",
    "NotificationApiEnabled": false,
    "NotificationHubConnectionString": "<Azure notification hub connection string>",
    "NotificationHubName": "<Azure notification hub name>",
    "Debug": true,
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