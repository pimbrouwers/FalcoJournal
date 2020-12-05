[<AutoOpen>]
module FalcoJournal.Common

module Endpoints =
    let ``/`` = "/"    
    
    let ``/entry/create`` = "/entry/create"

module Urls =
    let index = Endpoints.``/``

    let entryCreate = Endpoints.``/entry/create``