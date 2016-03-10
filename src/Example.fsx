#load "TransactionHelper.fs"
#r "System.Data"
#r "System.Xml"
#r "..\\packages\\FSharp.Data.SqlClient\\lib\\net40\\FSharp.Data.SqlClient.dll"

open TransactionHelper
open FSharp.Data
open System.Data
open System.Data.SqlClient

[<Literal>]
let cs = @"Data Source=.;Initial Catalog=Test;Integrated Security=True"

type InsertCommad = SqlCommandProvider<"INSERT INTO Test VALUES(@ID, @Value)", cs>

let insert a b = lift <| fun ctx -> 
    use cmd = InsertCommad.Create(transaction = ctx.Transaction.Value) 
    cmd.Execute(a,b)

let example = tx {
    let! res1 = insert 1 "abc"
    let! res2 = insert 2 "def"
    return res1 + res2
}
    
let result = run cs example