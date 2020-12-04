module FalcoJournal.Provider

open System
open System.Data
open Donald
open FalcoJournal.Domain

type DbConnectionFactory = unit -> IDbConnection

module EntryProvider =  
    let private fromDataReader (rd : IDataReader) : Entry =
        { Content      = rd.ReadString "content"
          EntryDate    = rd.ReadDateTime "entry_date"
          ModifiedDate = rd.ReadDateTime "modified_date" }

    type GetRecent = unit -> DbResult<Entry list>
    let getRecent (conn : IDbConnection) : GetRecent =
        fun () ->
            let sql = "
            SELECT    content
                    , entry_date
                    , modified_date
            FROM      entry
            ORDER BY  entry_date DESC
            LIMIT     100"
            dbCommand conn {
                cmdText sql
            }
            |> DbConn.query fromDataReader

