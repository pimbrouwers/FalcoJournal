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
      TextContent  : string }

    static member Create html text = 
        { HtmlContent  = html
          TextContent  = text }