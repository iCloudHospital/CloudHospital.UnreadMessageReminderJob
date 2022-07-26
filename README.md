## Azure Functions

* HTTP Trigger
* Timer Trigger
* Queue Trigger

### Http trigger

Insert wrapped event data from HTTP request body into table storage.

HTTP 요청 본문을 테이블 스토리지에 입력합니다.

### Timer trigger

Enqueue event data that inserted into table storage 5 minutes before.

5분전에 테이블 스토리지에 입력된  이벤트 데이터를 대기열(Queue storage)에 넣습니다.

### Queue trigger

Process dequeued event data.

대기열에서 빠져나온 이벤트 데이터를 처리합니다.

## Settings

```json
{
    "AzureWebJobsStorage": "<Azure Storage Account connection string; with queue-endpoint, table-endpoint>",
    "Database": "<Azure SQL Database connection string>",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
}
```

```bash
$ echo '{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "<Azure Storage Account connection string; with queue-endpoint, table-endpoint>",
    "Database": "<Azure SQL Database connection string>",
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
    "id":"d325e334-2f2f-4d6b-aa69-7a3084408d58",
    "groupId":"test-channel",
    "message":"Hello World!! from postman 🚀",
    "created":"2022-07-26T05:38:00Z"
}
```

