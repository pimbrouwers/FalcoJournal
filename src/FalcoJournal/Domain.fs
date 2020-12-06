module FalcoJournal.Domain

open System

type Entry = 
    { EntryId      : int
      HtmlContent  : string 
      TextContent  : string
      EntryDate    : DateTime 
      ModifiedDate : DateTime }

type EntrySummary = 
    { EntryId   : int 
      EntryDate : DateTime
      Summary   : string }

type NewEntry = 
    { HtmlContent : string 
      TextContent : string }

    static member Create html text = 
        { HtmlContent = html
          TextContent = text }

type UpdateEntry =
    { EntryId     : int
      HtmlContent : string 
      TextContent : string }

    static member Create entryId html text = 
        { EntryId     = entryId 
          HtmlContent = html
          TextContent = text }