# CDC SQL Monitor
You can monitor Sql Server Database changes by using CDC or CT.

CT - Change tracking. This will return only primary key value from changed table. This is supported on all sql server versions.

CDC - Change data capture. This will return changed columns on table. This is not supported on all sql server versions.

# Install
[Install-Package CDCSqlMonitor](https://www.nuget.org/packages/CDCSqlMonitor/1.0.1/)



## CT - Chnage Tracking
Compatibility – All

### Prerequisities
Tables must have primary key column. It can be any type.


### Setup
**For Change Tracking in SQL Server you need to enable it on the database first.**
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


**Listen to changes**
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

**Note that you need to keep track of the changes by yourself.** On the first run, it will return all changes on all described tables. \
In this example i had AMytable with 3 changes. If i stop and run the program again, then i need only new changes, so i will need to pass parameter where to start from.

Otherwise it will return all changes that have been occured since last cleanup. In this example this was 7 days (CHANGE_RETENTION = 7 DAYS).

Example:
```csharp

//1. Keep track of change version
private static void Monitor_OnRecordChnaged(object sender, CDCSqlMonitor.CT.EventArgs.DataChangedEventArgs e)
{
   SaveLastChange(e.LastChangeVersion);
}
//2. On startup pass saved parameter
void Start()
{
   var mySavedLastChange = GetLastChange();
   CT.Monitor monitor = new CT.Monitor(connectionString, 5, mySavedLastChange);	
}

	
```

## CDC - Change Data Capture

Compatibility: Azure SQL managed instance only, Standard, Developer and Enterprise Editions 2016 and up
 
 ### Prerequisities
Tables must have primary key column. It can be any type.

### Setup
**For Change Tracking in SQL Server you need to enable it on the database first.**
```sql
    --enable cdc on database
	EXEC sys.sp_cdc_enable_db
	--disable cdc on databse
	EXEC sys.sp_cdc_disable_db
```
Next you need to enable Change tracking on the tables as well. This can be done only manually for CDC.

CT can be enabled in code. \
CDC can not be enabled in code. Only manually.\
Reason: Enabling cdc on tables need sysadmin rights on database. Applications never have those.
```sql
    --enable on table
	EXEC sys.sp_cdc_enable_table @source_schema='dbo', @source_name= 'MyTable', @role_name=NULL
    --disable on table
	EXEC sys.sp_cdc_disable_table @source_schema='dbo', @source_name= 'MyTable', @capture_instance = 'all'
```

Now you have enabled Change Data tracking on your table(s).\
**Do not forget to configure capture and cleanup jobs!**\
More information: https://www.sqlservercentral.com/blogs/setting-up-change-data-capture-cdc

Check default configuration:
```sql
Select db_name(database_id) database_name, job_type, B.name,
maxtrans,continuous,pollinginterval,retention,threshold from msdb.dbo.cdc_jobs A
inner join msdb.dbo.sysjobs B on A.job_id= B.job_id
Order by job_type asc
```

Configure change job
```sql
EXECUTE sys.sp_cdc_change_job   
    @job_type = N'capture',  
    @maxscans = 500,  
    @pollinginterval = 5;--value in seconds
```

Confuigure cleanup job
```sql
EXECUTE sys.sp_cdc_change_job   
    @job_type = N'cleanup',  
    @retention = 1440; --value in minutes. Default is 3 days
```


### Using
Code:
```csharp
   static void Main(string[] args)
        {
            var connectionString = "Your Connectionstring";
            CDC.Monitor monitor = new CDC.Monitor(connectionString, 5);// 5 - Interval in seconds
            monitor.OnError += Monitor_OnError;
            monitor.OnRecordChnaged += Monitor_OnRecordChnaged;
           

            monitor.Start();
            Console.ReadLine();
        }
```


**Listen to changes**
```csharp
        private void Monitor_OnRecordChnaged(object sender, DataChangedEventArgs e)
        {
            foreach (var item in e.ChangedEntities)
            {
                Debug.WriteLine("Operation: " + item.ChangeType.ToString() + "  Table: " + item.TableName + "\n");
                foreach (var col in item.Columns) 
                {
                    Debug.WriteLine("Column: " + col.Name + "  Value: " + col.Value+ (col.OldValue != null ? "OldValue: "+col.OldValue :"") +"\n");
                }
                Debug.WriteLine("\n");
            }
        }

        private static void Monitor_OnError(object sender, CDCSqlMonitor.CT.EventArgs.ErrorEventArgs e)
        {
            Debug.WriteLine(e.Exception.StackTrace);
        }
```

Output:
```sh
Operation: UPDATE_NEW_VALUE  Table: MyTable
Column: ID  Value: 10 OldValue: 10
Column: Name  Value: row changed OldValue: row inserted
Column: Time  Value: 

Operation: INSERT  Table: MyTable
Column: ID  Value: 10
Column: Name  Value: row inserted
Column: Time  Value: 

Operation: DELETE  Table: MyTable
Column: ID  Value: 10
Column: Name  Value: row changed
Column: Time  Value:
```


If you want to see updates as sepparate rows, then you can access Raw data from DataChangedEventArgs:

```csharp
    private void Monitor_OnRecordChnaged(object sender, DataChangedEventArgs e)
        {
            foreach (var item in e.RawData)
            {
               //Do stuff
            }
        }
```

**Note that you need to keep track of the changes by yourself.** On the first run, it will return all changes on all tables. \
In this example i had MyTable with 3 changes. If i stop and run the program again, then i need only new changes, so i will need to pass parameter where to start from.

Otherwise it will return all changes that have been occured since last cleanup. In this example this was 1 day. ( @retention = 1440;).

Example:
```csharp

//1. Keep track of change version
private static void Monitor_OnRecordChnaged(object sender, CDCSqlMonitor.CDC.EventArgs.DataChangedEventArgs e)
{
   SaveLastChange(e.LastSequenceValue);//byte array with length of 10(byte[10]). Becuase in Sql server __$seqval is binary(10).
}
//2. On startup pass saved parameter
void Start()
{
   var mySavedLastChange = GetLastChange();
   CDC.Monitor monitor = new CDC.Monitor(connectionString, 5, mySavedLastChange);	
}

	
```


**Thanks to**
 
https://stackoverflow.com/questions/5288434/how-to-monitor-sql-server-table-changes-by-using-c
 
https://www.c-sharpcorner.com/uploadfile/satisharveti/introduction-to-cdc-change-data-capture-of-sql-server/
	
https://docs.microsoft.com/en-us/sql/relational-databases/system-tables/cdc-capture-instance-ct-transact-sql?view=sql-server-ver15

