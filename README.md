# CDC SQL Monitor
You can monitor Sql Server Database changes by using CDC or CT.

CT - Change tracking. This will return only primary key value from changed table. This is supported on all sql server versions.

CDC - Change data capture. This will return changed columns on table. This is not supported on all sql server versions.

## CT - Chnage Tracking
Compatibility – All

### Prerequisities
Tables must have primary key column. It can be any type.


### Setup
**Step 1 - For Change Tracking in SQL Server you need to enable it on the database first.**
```sql
ALTER DATABASE [AdventureWorks2017] SET CHANGE_TRACKING = ON(CHANGE_RETENTION = 7 DAYS, AUTO_CLEANUP = ON)
```
Next you need to enable Change tracking on the tables as well. This can be done manually or with code.

Manually:
```sql
ALTER TABLE AMytable ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON)
ALTER TABLE AMytable2 ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = ON)
```
### Using
Code:
```csharp
   static void Main(string[] args)
        {
            var connectionString = "Your Connectionstring";
            CT.Monitor monitor = new CT.Monitor(connectionString, 5);//5 seconds is the polling interval
            monitor.OnError += Monitor_OnError; //event for errors
            monitor.OnRecordChnaged += Monitor_OnRecordChnaged; //event for datachanges
            
            //Primary key columns can be any type (int, bigint, string, etc)
            monitor.AddTable("dbo.AMytable", "ID"); //Your table name and primary key column name
            monitor.AddTable("dbo.AMytable2", "PRIMARYKEYCOL"); //Your table name and primary key column name
            
            monitor.Setup();

            monitor.Start();
            Console.ReadLine();
        }
```


**Step 2 - Listen to changes**
```csharp
private static void Monitor_OnRecordChnaged(object sender, CDCSqlMonitor.CT.EventArgs.DataChangedEventArgs e)
        {
            foreach (var item in e.ChangedEntities) 
            {
                Debug.WriteLine("Operation: "+ item.ChangeType.ToString()+"  Table: " +item.TableName +" ID: "+ item.PrimaryKeyValue + " ChangeVersion: "+item.SYS_CHANGE_VERSION + "\n");
            }            
        }

        private static void Monitor_OnError(object sender, CDCSqlMonitor.CT.EventArgs.ErrorEventArgs e)
        {
            Debug.WriteLine(e.Exception.StackTrace);
        }
```
Output:
```sh
Operation: I  Table: dbo.AMytable2 ID: stringID ChangeVersion: 24
Operation: D  Table: dbo.AMytable2 ID: newstringID ChangeVersion: 26
Operation: U  Table: dbo.AMytable2 ID: stringID ChangeVersion: 27
```

Note that you need to keep track of the changes by yourself. On the first run, it will return all changes on all described tables. In this example i had AMytable with 3 changes. If i run the program next time, then i need only new changes, so i will ignore other changes. 
Example:
```csharp
private static void Monitor_OnRecordChnaged(object sender, CDCSqlMonitor.CT.EventArgs.DataChangedEventArgs e)
        {
            var mySavedLastChange = GetLastChange();//
            foreach (var item in e.ChangedEntities) 
            {
                if(item.SYS_CHANGE_VERSION <= mySavedLastChange)
                {
                    //Do stuff
                    if(item.SYS_CHANGE_VERSION > mySavedLastChange)
                    {
                        SaveLastChange(item.SYS_CHANGE_VERSION);//save last change number and read it later
                    }
                }
            }            
        }
```

## CDC - Change Data Capture

Compatibility: Azure SQL managed instance only, Standard, Developer and Enterprise Editions 2016 and up
 
 ### Prerequisities
Tables must have primary key column. It can be any type.

### Setup
**Step 1 - For Change Tracking in SQL Server you need to enable it on the database first.**
```sql
    --enable cdc on database
	EXEC sys.sp_cdc_enable_db
	--disable cdc on databse
	EXEC sys.sp_cdc_disable_db
```
Next you need to enable Change tracking on the tables as well. This can be done only manually for CDC.

CT can be enabled in code. 
CDC can not be enabled in code. Only manually.
Reason: Enabling cdc on tables need sysadmin rights on database. Applications never have those.
```sql
    --enable on table
	EXEC sys.sp_cdc_enable_table @source_schema='dbo', @source_name= 'MyTable', @role_name=NULL
    --disable on table
	EXEC sys.sp_cdc_disable_table @source_schema='dbo', @source_name= 'MyTable', @capture_instance = 'all'
```









