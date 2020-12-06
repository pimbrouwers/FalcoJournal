[<AutoOpen>]
module FalcoJournal.Common

module Urls =
    let index = "/"

    let entryCreate = "/entry/create"
    let entryEdit entryId = sprintf "/entry/edit/%O" entryId

module Endpoints =
    let ``/`` = Urls.index
    
    let ``/entry/create`` = Urls.entryCreate
    let ``/entry/edit/{id}`` = Urls.entryEdit "{id}"
