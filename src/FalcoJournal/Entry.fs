module FalcoJournal.Entry

open System
open Falco
open Falco.Markup
open FalcoJournal.Domain
open FalcoJournal.Provider
open FalcoJournal.Service
open FalcoJournal.UI

module private Partials = 
    let bulletEditor action editorContent = 
        Elem.form [ Attr.id "create-entry"
                    Attr.method "post"
                    Attr.action action ] [

            Elem.ul [ Attr.id "bullet-editor"
                      Attr.class' "mh0 pl3 outline-0 dark-gray"
                      Attr.create "contenteditable" "true" ] [    
                
                editorContent
            ]
            
            Elem.input [ Attr.id "bullet-editor-input" 
                         Attr.type' "hidden" 
                         Attr.name "content" ]         
        ]

module Create = 
    type Input = 
        { Content : string }

        static member Empty = 
            { Content = String.Empty }

    type Error =
        | InvalidInput of Input * string list


    let view (errors : string list) (input : Input) =
        let title = "A place for your thoughts..."
        
        let actions = [
            Forms.submit [ Attr.value "Save Entry"; Attr.form "create-entry" ] ]

        let editorContent = 
            match input.Content with
            | str when StringUtils.strEmpty str -> 
                Elem.li [] [] 

            | _ ->
                Text.raw input.Content

        Layouts.master title [
            Common.topBar actions
            Common.pageTitle title
            Common.errorSummary errors
            Partials.bulletEditor Urls.entryCreate editorContent
        ]

    let handle : HttpHandler =
        view [] Input.Empty
        |> Response.ofHtml 

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
            Buttons.solidGray "New Entry" Urls.entryCreate
            Buttons.solidWhite "Search" "#" ]

        Layouts.master title [
            Common.topBar actions
            Common.pageTitle title
            Common.errorSummary errors
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

