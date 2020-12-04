module FalcoJournal.Provider

open System
open System.Data
open Donald

type DbConnectionFactory = unit -> IDbConnection

