module FalcoJournal.Entry

open System
open Falco
open Falco.Markup
open Validus
open Validus.Operators
open FalcoJournal.Domain
open FalcoJournal.Provider
open FalcoJournal.Service
open FalcoJournal.UI

module private Partials = 
    let bulletEditor action htmlContent textContent = 
        Elem.form [ Attr.id "entry-editor"
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
        | InvalidInput of string list
        | UnexpectedError

    let service (createEntry : EntryProvider.Create) : ServiceHandler<Input, unit, Error> =                
        let validateInput input : Result<NewEntry, Error> =
            let result =
                NewEntry.Create
                <!> Validators.String.notEmpty None "Html" input.HtmlContent
                <*> Validators.String.notEmpty None "Text" input.TextContent
                
            match result with
            | Success newEntry -> Ok newEntry
            | Failure errors   -> 
                errors 
                |> ValidationErrors.toList
                |> InvalidInput
                |> Error

        let commitEntry entry : Result<unit, Error> =
            match createEntry entry with
            | Error _ -> Error UnexpectedError
            | Ok ()   -> Ok ()
            
        fun input ->
            input
            |> validateInput
            |> Result.bind commitEntry

    let view (errors : string list) (input : Input) =
        let title = "A place for your thoughts..."
        
        let actions = [
            Forms.submit [ Attr.value "Save Entry"; Attr.form "entry-editor" ] 
            Buttons.solidWhite "Cancel" Urls.index ]

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
        let handleError input error = 
            let errorMessages =                 
                match error with 
                | UnexpectedError     -> [ "Something went wrong" ]
                | InvalidInput errors -> errors

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

module Edit = 
    type Error = 
        | UnknownEntry
        | UnexpectedError

    let view (errors : string list) (entry : Entry) = 
        let title = "A place for your thoughts..."
        
        let actions = [
            Forms.submit [ Attr.value "Save Entry"; Attr.form "entry-editor" ] 
            Buttons.solidWhite "Cancel" Urls.index ]

        let formAction = Urls.entryEdit entry.EntryId
        let editorContent = Text.raw entry.HtmlContent

        Layouts.master title [
            Common.topBar actions
            Common.errorSummary errors
            Partials.bulletEditor formAction editorContent entry.TextContent            
        ]

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
        let handleError _ error =
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

