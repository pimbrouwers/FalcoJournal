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
    let bulletEditor action htmlContent = 
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
                         Attr.name "text_content" ]      
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
            | Error e -> Error UnexpectedError
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
            Partials.bulletEditor Urls.entryCreate editorContent          
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

        let workflow log conn input =
            service (EntryProvider.create log conn) input

        let formMap (form : FormCollectionReader) =
            { HtmlContent = form.GetString "html_content" ""
              TextContent = form.GetString "text_content" "" }

        Request.mapForm 
            formMap 
            (Service.run workflow handleOk handleError)

module Edit = 
    type Error = 
        | UnknownEntry
        | UnexpectedError

    let service (getEntry : EntryProvider.Get) : ServiceHandler<int, Entry, Error> =
        fun input ->
            match getEntry input with
            | Error _         -> Error UnexpectedError
            | Ok None         -> Error UnknownEntry
            | Ok (Some entry) -> Ok entry

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
            Partials.bulletEditor formAction editorContent            
        ]

    let handle : HttpHandler =
        let handleError _ _ =
            Error.notFound
            
        let handleOk entry : HttpHandler =
            entry
            |> view []
            |> Response.ofHtml

        let workflow log conn input =
            service (EntryProvider.get log conn) input

        let routeMap (route : RouteCollectionReader) =
            route.GetInt32 "id" -1
        
        Request.mapRoute 
            routeMap 
            (Service.run workflow handleOk handleError)

module Recent = 
    type Error = 
        | UnexpectedError
        | NoEntries 

    let service (getRecentEntries : EntryProvider.GetRecent) : ServiceHandler<unit, EntrySummary list, Error> =        
        fun input ->        
            match getRecentEntries input with
            | Error _          -> Error UnexpectedError
            | Ok e when e = [] -> Error NoEntries
            | Ok e             -> Ok e
                        
    let view (errors : string list) (entries : EntrySummary list)  =
        let title = "Recent Entries"

        let actions = [
            Buttons.solidGray "New Entry" Urls.entryCreate
            Buttons.solidWhite "Search" "#" ]

        let entryElems =
            entries |> List.map (fun e ->
                Elem.a [ Attr.href (Urls.entryEdit e.EntryId)
                         Attr.class' "db mb4 no-underline gray" ] [
                    Elem.div [ Attr.class' "mb1 f6 code moon-gray" ] [ Text.raw (e.EntryDate.ToString("yyyy/MM/dd HH:MM")) ] 
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

        let workflow log conn input =
            service (EntryProvider.getRecent log conn) input

        Service.run workflow handleOk handleError ()

