module FalcoJournal.Provider

open System.Data
open Donald
open Microsoft.Extensions.Logging
open FalcoJournal.Domain

/// Represents the creation of a new IDbConnection
type DbConnectionFactory = unit -> IDbConnection

module private DbResult = 
    /// Log DbResult, if error, and return
    let logError (log : ILogger) (dbResult : DbResult<'a>) : DbResult<'a> =
        match dbResult with
        | Ok _     -> dbResult
        | Error ex ->
            log.LogError(ex.Error, sprintf "DB ERROR: Failed to execute\n%s" ex.Statement)        
            dbResult

module EntryProvider =  
    type Create = NewEntry -> DbResult<unit>
    let create (log : ILogger) (conn : IDbConnection) : Create =
        fun entry ->            
            let sql = "
            INSERT INTO entry (html_content, text_content, entry_date, modified_date)
            VALUES (@html_content, @text_content, DATETIME('now'), DATETIME('now'));"
            
            dbCommand conn {
                cmdText  sql
                cmdParam [ "html_content", SqlType.String entry.HtmlContent
                           "text_content", SqlType.String entry.TextContent ]
            }
            |> DbConn.exec 
            |> DbResult.logError log

    type Get = int -> DbResult<Entry option>
    let get (log : ILogger) (conn : IDbConnection) =
        fun entryId ->
            let sql = "
            SELECT    entry_id
                    , html_content
                    , text_content
                    , entry_date
            FROM      entry
            WHERE     entry_id = @entry_id"

            let fromDataReader (rd : IDataReader) : Entry =
                { EntryId      = rd.ReadInt32 "entry_id"
                  HtmlContent  = rd.ReadString "html_content"
                  TextContent  = rd.ReadString "text_content"
                  EntryDate    = rd.ReadDateTime "entry_date" }

            dbCommand conn {
                cmdText  sql
                cmdParam [ "entry_id", SqlType.Int32 entryId ]
            }
            |> DbConn.querySingle fromDataReader
            |> DbResult.logError log

    type GetAll = unit -> DbResult<EntrySummary list>
    let getAll (log : ILogger) (conn : IDbConnection) : GetAll =
        fun () ->
            let sql = "
            SELECT    entry_id  
                    , DATETIME(entry_date) AS entry_date
                    , SUBSTR(text_content, 0, 50) AS summary
            FROM      entry
            ORDER BY  DATETIME(entry_date) DESC, DATETIME(modified_date) DESC"

            let fromDataReader (rd : IDataReader) = 
                { EntryId   = rd.ReadInt32 "entry_id"
                  EntryDate = rd.ReadDateTime "entry_date"                
                  Summary   = rd.ReadString "summary" }

            dbCommand conn {
                cmdText sql
            }
            |> DbConn.query fromDataReader
            |> DbResult.logError log

    type Update = UpdateEntry -> DbResult<unit>
    let update (log : ILogger) (conn : IDbConnection) : Update =
        fun entry ->
            let sql = "
                UPDATE  entry
                SET     html_content = @html_content
                      , text_content = @text_content
                      , modified_date = DATETIME('now')
                WHERE   entry_id = @entry_id;"

            dbCommand conn {
                cmdText  sql 
                cmdParam [ "html_content", SqlType.String entry.HtmlContent                           
                           "text_content", SqlType.String entry.TextContent
                           "entry_id", SqlType.Int32 entry.EntryId ]
            }
            |> DbConn.exec
            |> DbResult.logError log
