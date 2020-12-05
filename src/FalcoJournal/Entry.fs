module FalcoJournal.Entry

open System
open Falco
open Falco.Markup
open FalcoJournal.Domain
open FalcoJournal.Provider
open FalcoJournal.Service
open FalcoJournal.UI

module private Partials = 
    let bulletEditor action htmlContent textContent = 
        Elem.form [ Attr.id "create-entry"
                    Attr.method "post"
                    Attr.action action ] [

            Elem.ul [ Attr.id "bullet-editor"
                      Attr.class' "mh0 pl3 outline-0"
                      Attr.create "contenteditable" "true" ] [    
                
                htmlContent
            ]
            
            Elem.input [ Attr.id "bullet-editor-html" 
                         Attr.type' "hidden" 
                         Attr.name "html_content" ]      
                         
            Elem.input [ Attr.id "bullet-editor-text" 
                         Attr.type' "hidden" 
                         Attr.name "text_content"
                         Attr.value textContent ]      
        ]

module Create = 
    type Input = 
        { HtmlContent : string
          TextContent : string }

        static member Empty = 
            { HtmlContent = String.Empty 
              TextContent = String.Empty }

    type Error =
        | UnexpectedError of Input
        | InvalidInput of Input * string list

    let service (createEntry : EntryProvider.Create) : ServiceHandler<Input, unit, Error> =                
        fun input ->
            let entry = NewEntry.Create input.HtmlContent input.TextContent
            match createEntry entry with
            | Error _ -> Error (UnexpectedError input)
            | Ok ()   -> Ok ()

    let view (errors : string list) (input : Input) =
        let title = "A place for your thoughts..."
        
        let actions = [
            Forms.submit [ Attr.value "Save Entry"; Attr.form "create-entry" ] ]

        let editorContent = 
            match input.HtmlContent with
            | str when StringUtils.strEmpty str -> 
                Elem.li [] [] 

            | _ ->
                Text.raw input.HtmlContent

        Layouts.master title [
            Common.topBar actions
            Common.errorSummary errors
            Partials.bulletEditor Urls.entryCreate editorContent input.TextContent
        ]

    let handle : HttpHandler =
        view [] Input.Empty
        |> Response.ofHtml 

    let handleSubmit : HttpHandler =
        let handleError error = 
            let (input, errorMessages) =                 
                match error with 
                | UnexpectedError input -> 
                    input, [ "Something went wrong" ]

                | InvalidInput (input, errors) ->
                    input, errors

            view errorMessages input
            |> Response.ofHtml

        let handleOk () =
            Response.redirect Urls.index false

        let workflow conn input =
            service (EntryProvider.create conn) input

        let formMap (form : FormCollectionReader) =
            { HtmlContent = form.GetString "html_content" ""
              TextContent = form.GetString "text_content" "" }

        Request.mapForm formMap (Service.run workflow handleOk handleError)

module Recent = 
    type Error = 
        | UnexpectedError
        | NoEntries 

    type EntryModel = 
        { EntryId  : int
          yyyyMMdd : string          
          Summary  : string }

    let service (getRecentEntries : EntryProvider.GetRecent) : ServiceHandler<unit, EntryModel list, Error> =
        let queryData () : Result<Entry list, Error> = 
            match getRecentEntries () with
            | Error _          -> Error UnexpectedError
            | Ok e when e = [] -> Error NoEntries
            | Ok e             -> Ok e

        let mapEntries entries : EntryModel list =
            let toEntryModel (entry : Entry) = 
                let summary =
                    if entry.TextContent.Length > 50 then 
                        entry.TextContent.Substring(0,47) |> sprintf "%s..." 
                    else 
                        entry.TextContent

                { EntryId  = entry.EntryId
                  yyyyMMdd = entry.EntryDate.ToString("yyyy/MM/dd HH:MM")
                  Summary  = summary }

            entries
            |> List.map toEntryModel

        fun input ->        
            queryData input
            |> Result.map mapEntries
            
    let view (errors : string list) (entries : EntryModel list)  =
        let title = "Recent Entries"

        let actions = [
            Buttons.solidGray "New Entry" Urls.entryCreate
            Buttons.solidWhite "Search" "#" ]

        let entryElems =
            entries |> List.map (fun e ->
                Elem.a [ Attr.href (Urls.entryEdit e.EntryId)
                         Attr.class' "db mb4 no-underline gray" ] [
                    Elem.div [ Attr.class' "mb1 f6 code moon-gray" ] [ Text.raw e.yyyyMMdd  ] 
                    Elem.div [] [ Text.raw e.Summary ] 
                    Elem.div [ Attr.class' "w1 mt3 bt b--moon-gray" ] [] ])

        Layouts.master title [
            Common.topBar actions
            Common.pageTitle title
            Common.errorSummary errors

            Elem.div [] entryElems
        ]

    let handle : HttpHandler =
        let handleError error =
            let errorMessages = 
                match error with
                | UnexpectedError _ -> [ "Something went wrong" ]
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

