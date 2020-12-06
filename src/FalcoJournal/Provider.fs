module FalcoJournal.Provider

open System
open System.Data
open Donald
open FalcoJournal.Domain

type DbConnectionFactory = unit -> IDbConnection

module EntryProvider =  
    let private fromDataReader (rd : IDataReader) : Entry =
        { EntryId      = rd.ReadInt32 "entry_id"
          HtmlContent  = rd.ReadString "html_content"
          TextContent  = rd.ReadString "text_content"
          EntryDate    = rd.ReadDateTime "entry_date"
          ModifiedDate = rd.ReadDateTime "modified_date" }

    type Create = NewEntry -> DbResult<unit>
    let create (conn : IDbConnection) : Create =
        fun entry ->            
            let sql = "
            INSERT INTO entry (html_content, text_content, entry_date, modified_date)
            VALUES (@html_content, @text_content, DATE('now'), DATE('now'))"
            
            dbCommand conn {
                cmdText  sql
                cmdParam [ "html_content", SqlType.String entry.HtmlContent
                           "text_content", SqlType.String entry.TextContent ]
            }
            |> DbConn.exec 

    type GetRecent = unit -> DbResult<Entry list>
    let getRecent (conn : IDbConnection) : GetRecent =
        fun () ->
            let sql = "
            SELECT    entry_id  
                    , html_content
                    , text_content
                    , entry_date
                    , modified_date
            FROM      entry
            ORDER BY  DATETIME(entry_date) DESC
            LIMIT     100"
            dbCommand conn {
                cmdText sql
            }
            |> DbConn.query fromDataReader

    type Update = Entry -> DbResult<unit>
    let update (conn : IDbConnection) : Update =
        fun entry ->
            let sql = "
                UPDATE  entry
                SET     html_content = @html_content
                      , text_content = @text_content
                      , modified_date = DATE('now')
                WHERE   entry_id = @entry_id"

            dbCommand conn {
                cmdText  sql 
                cmdParam [ "html_content", SqlType.String entry.HtmlContent                           
                           "text_content", SqlType.String entry.TextContent
                           "entry_id", SqlType.Int32 entry.EntryId ]
            }
            |> DbConn.exec
