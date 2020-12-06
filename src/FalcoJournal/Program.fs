module FalcoJournal.Program

open System.Data
open System.Data.SQLite
open Falco
open Falco.Routing
open Falco.HostBuilder
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open FalcoJournal.Common.Endpoints
open FalcoJournal.Provider

// ------------
// Register services
// ------------
let configureServices (connectionFactory : DbConnectionFactory) (services : IServiceCollection) =    
    services.AddSingleton<DbConnectionFactory>(connectionFactory)
            .AddFalco() |> ignore

// ------------
// Activate middleware
// ------------
let configureApp (endpoints : HttpEndpoint list) (ctx : WebHostBuilderContext) (app : IApplicationBuilder) =    
    let devMode = StringUtils.strEquals ctx.HostingEnvironment.EnvironmentName "Development"    
    
    app.UseStaticFiles()
       .UseWhen(devMode, fun app ->app.UseDeveloperExceptionPage())
       .UseWhen(not(devMode), fun app -> app.UseFalcoExceptionHandler(Error.serverError))       
       .UseFalco(endpoints) 
       .Run(HttpHandler.toRequestDelegate Error.notFound) |> ignore

// -----------
// Configure Web host
// -----------
let configureWebHost (endpoints : HttpEndpoint list) (webHost : IWebHostBuilder) =        
    let appConfiguration : IConfiguration =     
        ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Local.json")
            .Build()
            :> IConfiguration   

    let connectionString = appConfiguration.GetConnectionString("Default")
    let connectionFactory () = new SQLiteConnection(connectionString, true) :> IDbConnection

    webHost
        .UseConfiguration(appConfiguration)
        .ConfigureServices(configureServices connectionFactory)
        .Configure(configureApp endpoints)

[<EntryPoint>]
let main args =            
    webHost args {
        configure configureWebHost
        endpoints [ 
            get ``/``
                Entry.Recent.handle
            
            all ``/entry/create``
                [ GET, Entry.Create.handle 
                  POST, Entry.Create.handleSubmit ]

            all ``/entry/edit/{id}`` 
                [ GET, Entry.Edit.handle ]
        ]
    }

    0