module FalcoJournal.Domain

open System

type Entry = 
    { EntryId      : int
      HtmlContent  : string 
      TextContent  : string
      EntryDate    : DateTime 
      ModifiedDate : DateTime }

type NewEntry = 
    { HtmlContent  : string 
      TextContent  : string
      EntryDate    : DateTime 
      ModifiedDate : DateTime }

    static member Create html text = 
        let now = DateTime.UtcNow

        { HtmlContent  = html
          TextContent  = text
          EntryDate    = now
          ModifiedDate = now }