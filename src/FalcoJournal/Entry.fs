module FalcoJournal.Entry

open Falco
open Falco.Markup
open FalcoJournal.Domain
open FalcoJournal.Provider
open FalcoJournal.Service

module Recent = 
    type Error = 
        | UnexpectedError of string
        | NoEntries 

    let service (getRecentEntries : EntryProvider.GetRecent) : ServiceHandler<unit, Entry list, Error> =
        fun () ->        
            match getRecentEntries () with
            | Error e          -> Error (UnexpectedError e.Error.Message)
            | Ok e when e = [] -> Error NoEntries
            | Ok e             -> Ok e             
            
    let view (errors : string list) (entries : Entry list)  =
        let title = "Recent Entries"

        let actions = [
            UI.Buttons.solidGray "New Entry" "#"
            UI.Buttons.solidWhite "Search" "#" ]

        UI.Layouts.master title [
            UI.Common.topBar actions
            UI.Common.pageTitle title
            UI.Common.errorSummary errors
        ]

    let handle : HttpHandler =
        let handleError error =
            let errorMessages = 
                match error with
                | UnexpectedError e -> [ e ]
                | NoEntries         -> [ "No entries" ]

            view errorMessages []
            |> Response.ofHtml

        let handleOk entries = 
            entries
            |> view [] 
            |> Response.ofHtml

        let workflow conn input =
            service (EntryProvider.getRecent conn) input

        Service.run workflow handleOk handleError ()

