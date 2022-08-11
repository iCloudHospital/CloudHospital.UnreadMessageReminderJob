## UnreadMessageReminder

### Http triggers

POST: /api/CallingReminderConsultationUpdatedWebHook

payload: 

```plaintext
{
    string      id, 
    string      patientId
    datetime    confirmedDateStart
    int         consultationType
    string      hospitalId
    string      hospitalName
    string      hospitalWebsiteUrl
    bool        isOpen
}
```

Insert wrapped event data from HTTP request body into table storage.

If isOpen is false, remove entry from table storage.

### Timer trigger

When the data is satisfied, puts the data into a queue. (+- 30)


### Queue trigger

Process dequeued data.