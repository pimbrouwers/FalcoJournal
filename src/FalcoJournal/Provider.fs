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
            VALUES (@html_content, @text_content, @entry_date, @modified_date)"
            
            dbCommand conn {
                cmdText  sql
                cmdParam [ "html_content", SqlType.String entry.HtmlContent
                           "text_content", SqlType.String entry.TextContent
                           "entry_date", SqlType.DateTime entry.EntryDate 
                           "modified_date", SqlType.DateTime entry.ModifiedDate ]
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

