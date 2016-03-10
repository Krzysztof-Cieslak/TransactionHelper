module TransactionHelper
        
open System.Data
open System.Data.SqlClient

type TransactionContext = {
    Connection : SqlConnection
    Transaction : SqlTransaction option
}

type Transaction<'a> = Transaction of (TransactionContext -> 'a option)

type TransactionBuilder(level: IsolationLevel) =
    let confirmOpen (con: SqlConnection) =
        if con.State <> ConnectionState.Open then
            con.Open()
        con
  
    let run (Transaction block) context = 
        try
            block context
        with
        | _ -> None 
    
    let runDelay f context = run (f()) context
    
    let complete result (transaction: SqlTransaction) =
        match result with
        | Some _ -> transaction.Commit()
        | _ -> transaction.Rollback()
        result
  
    member this.Return(result) = Transaction(fun _ -> Some result)
  
    member this.ReturnFrom(m) = m
  
    member this.Bind(m, f) = Transaction(fun context -> 
        match run m context with
        | Some out -> run (f out) context
        | _ -> None)
  
    member this.Delay(f) = Transaction(fun context -> 
        match context.Transaction with
        | Some t -> 
            runDelay f context
        | None ->
            let con = confirmOpen context.Connection
            use trans = con.BeginTransaction ()
            let res = runDelay f {Connection = con; Transaction = Some trans}
            complete res trans)


let transaction level = TransactionBuilder(level)

let cancel = 
    Transaction (fun _ -> None)

let runTransaction (Transaction block) context = 
    block context

let tx = transaction IsolationLevel.ReadCommitted

let lift fn = Transaction (fn >> Some)

let run cs trans = 
    use conn = new SqlConnection(cs) 
    runTransaction trans {Connection = conn; Transaction = None}