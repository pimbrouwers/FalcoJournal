module FalcoJournal.Error

open Falco
open Falco.Markup 
open FalcoJournal.UI

/// Invalid CSRF token, returns HTTP 400 with empty response
let invalidCsrfToken : HttpHandler = 
    Response.withStatusCode 400 
    >> Response.ofEmpty

/// Server Error (500) 
let serverError : HttpHandler =
    Response.withStatusCode 500 
    >> Response.ofPlainText "Server error"

/// Not found (404) HTML response
let notFound : HttpHandler =
    let doc = 
        let actions = [
            Buttons.solidGray "New Entry" Urls.entryCreate
            Buttons.solidWhite "Search" "#" ]

        Layouts.master "Not Found" [            
            Common.topBar actions
            Common.pageTitle "Not found"
            Elem.div [] [ Text.raw "The page you've request could not be found." ]
        ]
    
    Response.withStatusCode 404 
    >> Response.ofHtml doc

