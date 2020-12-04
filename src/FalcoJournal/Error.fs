module FalcoJournal.Error

open Falco
open Falco.Markup 

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
    let doc = UI.Layouts.master "Not Found" [ 
            UI.Common.pageTitle "Not found"
            Elem.div [] [ Text.raw "The page you've request could not be found." ]
        ]
    
    Response.withStatusCode 404 
    >> Response.ofHtml doc

