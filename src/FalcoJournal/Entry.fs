module FalcoJournal.Entry

open Falco
open Falco.Markup
open FalcoJournal.Domain

module Recent = 
    let view (entries : Entry list) =
        let title = "Recent Entries"

        let actions = [
            UI.Buttons.solidGray "New Entry" "#"
            UI.Buttons.solidWhite "Search" "#" ]

        UI.Layouts.master title [
            UI.Common.topBar actions
            UI.Common.pageTitle title
        ]

    let handle : HttpHandler =
        view []
        |> Response.ofHtml 

