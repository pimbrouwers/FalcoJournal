module FalcoJournal.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Data.Sqlite
open Microsoft.Extensions.DependencyInjection
open System.Data

// ------------
// Database
// ------------
type DbConnectionFactory = unit -> IDbConnection

let connectionString path = sprintf "Data Source=%s\\FalcoJournal.db; Version=3; Cache=Shared; Pooling=True; Max Pool Size=100;" path

// ------------
// Register services
// ------------
let configureServices (connectionString : string) (services : IServiceCollection) =    
    let connectionFactory () =
        new SqliteConnection(connectionString) :> IDbConnection

    services.AddSingleton<DbConnectionFactory>(connectionFactory)
            .AddFalco() |> ignore

// ------------
// Activate middleware
// ------------
let configureApp (endpoints : HttpEndpoint list) (ctx : WebHostBuilderContext) (app : IApplicationBuilder) =    
    let devMode = StringUtils.strEquals ctx.HostingEnvironment.EnvironmentName "Development"    
    
    app.UseWhen(devMode, fun app ->app.UseDeveloperExceptionPage())
       .UseWhen(not(devMode), fun app -> app.UseFalcoExceptionHandler(Error.serverError))
       .UseFalco(endpoints) 
       .Run(HttpHandler.toRequestDelegate Error.notFound) |> ignore

// -----------
// Configure Web host
// -----------
let configureWebHost (connectionString : string) (endpoints : HttpEndpoint list) (webHost : IWebHostBuilder) =    
    webHost
        .ConfigureServices(configureServices connectionString)
        .Configure(configureApp endpoints)

[<EntryPoint>]
let main args =
    if args.Length <> 1 then
        failwith "Must provide a db directory"
    
    webHost args {
        configure (configureWebHost (connectionString args.[0]))
        endpoints [            
            all "/" [
                GET, Entry.Recent.handle ]
        ]
    }
    0