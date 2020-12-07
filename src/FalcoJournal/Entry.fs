module FalcoJournal.Entry

open System
open System.Data
open Falco
open Falco.Markup
open Microsoft.Extensions.Logging
open Validus
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

// ------------
// GET /
// ------------
module Index = 
    type Error = 
        | UnexpectedError
        | NoEntries 

    let service (getRecentEntries : EntryProvider.GetAll) : ServiceHandler<unit, EntrySummary list, Error> =        
        fun input ->        
            match getRecentEntries input with
            | Error _          -> Error UnexpectedError
            | Ok e when e = [] -> Error NoEntries
            | Ok e             -> Ok e
                        
    let view (errors : string list) (entries : EntrySummary list)  =
        let title = "Journal Entries"

        let actions = [ Buttons.solidGray "New Entry" Urls.entryCreate ]

        let entryElems =
            entries |> List.map (fun e ->
                Elem.a [ Attr.href (Urls.entryEdit e.EntryId)
                         Attr.class' "db mb4 no-underline white-90" ] [
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
            service (EntryProvider.getAll log conn) input

        Service.run workflow handleOk handleError ()

type NewEntryModel = 
    { HtmlContent : string 
      TextContent : string }
          
    static member Create html text = 
        { HtmlContent = html
          TextContent = text }

    static member Empty =
        NewEntryModel.Create String.Empty String.Empty

// ------------
// GET /entry/create
// ------------
module New =
    let view (errors : string list) (newEntry : NewEntryModel) =
        let title = "A place for your thoughts..."
        
        let actions = [
            Forms.submit [ Attr.value "Save Entry"; Attr.form "entry-editor" ] 
            Buttons.solidWhite "Cancel" Urls.index ]

        let editorContent = 
            match newEntry.HtmlContent with
            | str when StringUtils.strEmpty str -> 
                Elem.li [] [] 

            | _ ->
                Text.raw newEntry.HtmlContent

        Layouts.master title [
            Common.topBar actions
            Common.errorSummary errors
            Partials.bulletEditor Urls.entryCreate editorContent          
        ]

    let handle : HttpHandler =
        view [] NewEntryModel.Empty
        |> Response.ofHtml

// ------------
// POST /entry/create
// ------------
module Create =     
    type Error =
        | InvalidInput of string list
        | UnexpectedError

    let service (createEntry : EntryProvider.Create) : ServiceHandler<NewEntryModel, unit, Error> =                
        let validateInput (input : NewEntryModel) : Result<NewEntry, Error> =
            let result = NewEntry.Create input.HtmlContent input.TextContent
                
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

    let handle : HttpHandler =
        let handleError (input : NewEntryModel) (error : Error) = 
            let errorMessages =                 
                match error with 
                | UnexpectedError     -> [ "Something went wrong" ]
                | InvalidInput errors -> errors

            New.view errorMessages input
            |> Response.ofHtml

        let handleOk () : HttpHandler =
            Response.redirect Urls.index false

        let workflow (log : ILogger) (conn : IDbConnection) (input : NewEntryModel) =
            service (EntryProvider.create log conn) input

        let formMap (form : FormCollectionReader) : NewEntryModel =
            { HtmlContent = form.GetString "html_content" ""
              TextContent = form.GetString "text_content" "" }

        Request.mapForm 
            formMap 
            (Service.run workflow handleOk handleError)

type EditEntryModel = 
    { EntryId     : int 
      HtmlContent : string 
      TextContent : string }
    
    static member Create entryId html text = 
        { EntryId     = entryId 
          HtmlContent = html
          TextContent = text }

// ------------
// GET /entry/edit/{id}
// ------------
module Edit = 
    type Error = 
        | UnknownEntry
        | UnexpectedError

    let routeMap (route : RouteCollectionReader) =
        route.GetInt32 "id" -1

    let service (getEntry : EntryProvider.Get) : ServiceHandler<int, EditEntryModel, Error> =
        fun input ->
            match getEntry input with
            | Error _         -> Error UnexpectedError
            | Ok None         -> Error UnknownEntry
            | Ok (Some entry) -> 
                EditEntryModel.Create entry.EntryId entry.HtmlContent entry.TextContent
                |> Ok

    let view (errors : string list) (entry : EditEntryModel) = 
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

        Request.mapRoute 
            routeMap 
            (Service.run workflow handleOk handleError)

// ------------
// POST /entry/edit/{id}
// ------------
module Update = 
    type Error =
        | InvalidInput of string list
        | UnexpectedError
            
    let service (updateEntry : EntryProvider.Update) : ServiceHandler<EditEntryModel, unit, Error> =                
            let validateInput (input : EditEntryModel) : Result<UpdateEntry, Error> =
                let result = UpdateEntry.Create input.EntryId input.HtmlContent input.TextContent
                    
                match result with
                | Success newEntry -> Ok newEntry
                | Failure errors   -> 
                    errors 
                    |> ValidationErrors.toList
                    |> InvalidInput
                    |> Error

            let commitEntry entry : Result<unit, Error> =
                match updateEntry entry with
                | Error e -> Error UnexpectedError
                | Ok ()   -> Ok ()
                
            fun input ->
                input
                |> validateInput
                |> Result.bind commitEntry

    let handle : HttpHandler =
        let handleError (input : EditEntryModel) (error : Error) : HttpHandler =
            let errorMessages =                 
                match error with 
                | UnexpectedError     -> [ "Something went wrong" ]
                | InvalidInput errors -> errors

            Edit.view errorMessages input
            |> Response.ofHtml

        let handleOk () : HttpHandler = 
            Response.redirect Urls.index false

        let workflow (log : ILogger) (conn : IDbConnection) (input : EditEntryModel) =
            service (EntryProvider.update log conn) input

        let handleForm (entryId : int) : HttpHandler =
            let formMap (entryId : int) (form : FormCollectionReader) : EditEntryModel =
                { EntryId = entryId 
                  HtmlContent = form.GetString "html_content" "" 
                  TextContent = form.GetString "text_content" "" }

            Request.mapForm 
                (formMap entryId) 
                (Service.run workflow handleOk handleError)

        Request.mapRoute Edit.routeMap handleForm
            
