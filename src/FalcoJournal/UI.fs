module FalcoJournal.UI

open Falco.Markup

module Elem =    
    let script = Elem.tag "script"

module Attr =
    let form v = Attr.create "form" v

/// Reusable components
module Common = 
    /// Display a list of errors as <ul>...</ul>
    let errorSummary (errors : string list) =
        match errors.Length with
        | n when n > 0 ->
            Elem.ul [ Attr.class' "mt0 pa2 pl4 red bg-washed-red ba br1 b--red" ] 
                    (errors |> List.map (fun e -> Elem.li [] [ Text.raw e ]))

        | _ -> 
            Elem.div [] []

    /// Page title as <h1></h1>
    let pageTitle (title : string) =
        Elem.h1 [ Attr.class' "pb3 code tc gray" ] [ Text.raw title ]

    /// Top bar with pluggable actions
    let topBar (actions : XmlNode list) =
        Elem.header [ Attr.class' "pv4" ] [
            Elem.nav [ Attr.class' "flex items-center" ] [
                Elem.a [ Attr.class' "f4 f3-l gray no-underline" 
                         Attr.href Urls.index ] [ Text.raw "Falco Journal" ]
                Elem.div [ Attr.class' "flex-grow-1 tr" ] actions ] ]

/// Button link elements
module Buttons =
    let solidGray label url = 
        let attr =  [ 
            Attr.href url
            Attr.class' "dib pa2 bg-light-gray gray no-underline bn br1" ]

        Elem.a attr [ Text.raw label ]

    let solidWhite label url = 
        let attr =  [ 
            Attr.href url
            Attr.class' "dib pa2 ph3-l gray no-underline bn br1" ]

        Elem.a attr [ Text.raw label ]

/// Form elements
module Forms =
    let inputText attr = 
        // safely combine custom attributes with defaults
        let defaultAttr = [ 
            Attr.type' "text"
            Attr.class' "pa2 ba b--silver br1" ]

        let mergedAttr = attr |> Attr.merge defaultAttr
        Elem.input mergedAttr 

    let submit attr =
        // safely combine custom attributes with defaults
        let defaultAttr = [ 
            Attr.type' "submit"
            Attr.class' "dib pa2 bg-light-gray gray bn br1"]

        let mergedAttr = attr |> Attr.merge defaultAttr 
        Elem.input mergedAttr

/// Website layouts
module Layouts =
    let private tachyonsUrl = "https://unpkg.com/tachyons@4.12.0/css/tachyons.min.css"
    
    let private head (htmlTitle : string) : XmlNode list = 
        [
            Elem.meta  [ Attr.charset "UTF-8" ]
            Elem.meta  [ Attr.httpEquiv "X-UA-Compatible"; Attr.content "IE=edge, chrome=1" ]
            Elem.meta  [ Attr.name "viewport"; Attr.content "width=device-width, initial-scale=1" ]                
            Elem.title [] [ Text.rawf "%s | FalcoJournal" htmlTitle ]
            Elem.link  [ Attr.href tachyonsUrl; Attr.rel "stylesheet" ]                 
        ]
        
    /// Master layout which accepts a title and content for <body></body>
    let master (htmlTitle : string) (content : XmlNode list) =
        Elem.html [ Attr.lang "en"; ] [
            Elem.head [] (head htmlTitle)
            Elem.body [ Attr.class' "mw7 center ph3 f4-l georgia gray" ] [ 
                Elem.main [] content 
                Elem.script [ Attr.src "/index.js"; Attr.type' "text/javascript" ] [] ] ]