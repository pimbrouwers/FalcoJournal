﻿module FalcoJournal.Service

open System.Data
open Falco
open Microsoft.Extensions.Logging
open FalcoJournal.Provider

/// Work to be done that has input and will generate output
type ServiceHandler<'input, 'output, 'error> = 'input -> Result<'output, 'error>

/// An HttpHandler to execute services, and can help reduce code
/// repetition by acting as a composition root for injecting
/// dependencies for logging, database, http etc.
let run
    (serviceHandler: ILogger -> IDbConnection -> ServiceHandler<'input, 'output, 'error>)
    (handleOk : 'output -> HttpHandler)
    (handleError : 'input -> 'error -> HttpHandler)
    (input : 'input) : HttpHandler =
    fun ctx ->
        let connectionFactory = ctx.GetService<DbConnectionFactory>()
        use connection = connectionFactory ()
        let log = ctx.GetLogger "FalcoJournal.Service"
                
        let respondWith = 
            match serviceHandler log connection input with
            | Ok output -> handleOk output
            | Error error -> handleError input error

        respondWith ctx