module FalcoJournal.Domain

open System

type Entry = 
    { Content      : string 
      EntryDate    : DateTime 
      ModifiedDate : DateTime }

    static member Create content = 
        let now = DateTime.UtcNow

        { Content      = content 
          EntryDate    = now
          ModifiedDate = now }