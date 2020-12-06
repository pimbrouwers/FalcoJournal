module FalcoJournal.Domain

open System
open Validus
open Validus.Operators

type Entry = 
    { EntryId      : int
      HtmlContent  : string 
      TextContent  : string
      EntryDate    : DateTime }

type EntrySummary = 
    { EntryId   : int 
      EntryDate : DateTime
      Summary   : string }

type NewEntry = 
    { HtmlContent : string 
      TextContent : string }

    static member Create html text : ValidationResult<NewEntry> =
        let htmlValidator : Validator<string> = 
            Validators.String.notEmpty None
            <+> Validators.String.notEquals "<li></li>" (Some (sprintf "%s must not be empty"))

        let create html text = 
            { HtmlContent = html
              TextContent = text }            

        create
        <!> htmlValidator "HTML" html
        <*> Validators.String.notEmpty None "Text" text

type UpdateEntry =
    { EntryId     : int
      HtmlContent : string 
      TextContent : string }

    static member Create entryId html text : ValidationResult<UpdateEntry> =        
        let create (entryId : int) (entry : NewEntry) = 
            { EntryId = entryId 
              HtmlContent = entry.HtmlContent 
              TextContent = entry.TextContent }

        create 
        <!> Validators.Int.greaterThan 0 (Some (sprintf "Invalid %s")) "Entry ID" entryId 
        <*> NewEntry.Create html text