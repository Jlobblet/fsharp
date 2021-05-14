(*
msbuild tests\fsharp\perf\tasks\FS\TaskPerf.fsproj /p:Configuration=Release
dotnet artifacts\bin\TaskPerf\Release\netcoreapp2.1\TaskPerf.dll
*)

namespace TaskPerf

//open FSharp.Control.Tasks
open System.Diagnostics
open System.Threading.Tasks
open System.IO
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open TaskBuilderTasks //.ContextSensitive // TaskBuilder.fs extension members
//open FSharp.Control.ContextSensitiveTasks // the default
open FSharp.Control // AsyncSeq
open Tests.SyncBuilder
open Tests.TaskSeq
open BenchmarkDotNet.Configs

[<AutoOpen>]
module Helpers =
    let bufferSize = 128
    let manyIterations = 10000
    let syncTask() = Task.FromResult 100
    let syncTask_async() = async.Return 100
    let asyncTask() = Task.Yield()

    let tenBindSync_task() =
        task {
            let! res1 = syncTask()
            let! res2 = syncTask()
            let! res3 = syncTask()
            let! res4 = syncTask()
            let! res5 = syncTask()
            let! res6 = syncTask()
            let! res7 = syncTask()
            let! res8 = syncTask()
            let! res9 = syncTask()
            let! res10 = syncTask()
            return res1 + res2 + res3 + res4 + res5 + res6 + res7 + res8 + res9 + res10
         }


    let tenBindSync_taskBuilder() =
        taskBuilder {
            let! res1 = syncTask()
            let! res2 = syncTask()
            let! res3 = syncTask()
            let! res4 = syncTask()
            let! res5 = syncTask()
            let! res6 = syncTask()
            let! res7 = syncTask()
            let! res8 = syncTask()
            let! res9 = syncTask()
            let! res10 = syncTask()
            return res1 + res2 + res3 + res4 + res5 + res6 + res7 + res8 + res9 + res10
         }

    let tenBindSync_async() =
        async {
            let! res1 = syncTask_async()
            let! res2 = syncTask_async()
            let! res3 = syncTask_async()
            let! res4 = syncTask_async()
            let! res5 = syncTask_async()
            let! res6 = syncTask_async()
            let! res7 = syncTask_async()
            let! res8 = syncTask_async()
            let! res9 = syncTask_async()
            let! res10 = syncTask_async()
            return res1 + res2 + res3 + res4 + res5 + res6 + res7 + res8 + res9 + res10
         }

    let tenBindAsync_task() =
        task {
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
         }

    let tenBindAsync_taskBuilder() =
        taskBuilder {
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
            do! asyncTask()
         }

    let singleTask_task() =
        task { return 1 }


    let singleTask_taskBuilder() =
        taskBuilder { return 1 }

    let singleTask_async() =
        async { return 1 }

[<MemoryDiagnoser>]
[<GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)>]
[<CategoriesColumn>]
type Benchmarks() =

    [<BenchmarkCategory("ManyWriteFile"); Benchmark(Baseline=true)>]
    member _.ManyWriteFile_CSharpTasks () =
        TaskPerfCSharp.ManyWriteFileAsync().Wait();

    [<BenchmarkCategory("ManyWriteFile");Benchmark>]
    member _.ManyWriteFile_Task () =
        let path = Path.GetTempFileName()
        task {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)
            for i = 1 to manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)
        }
        |> fun t -> t.Wait()
        File.Delete(path)


    [<BenchmarkCategory("ManyWriteFile");Benchmark>]
    member _.ManyWriteFile_TaskUsingTaskBuilder () =
        let path = Path.GetTempFileName()
        taskBuilder {
            let junk = Array.zeroCreate bufferSize
            use file = File.Create(path)
            for i = 1 to manyIterations do
                do! file.WriteAsync(junk, 0, junk.Length)
        }
        |> fun t -> t.Wait()
        File.Delete(path)

    // Slow
    //[<BenchmarkCategory("ManyWriteFile");Benchmark>]
    //member _.ManyWriteFile_FSharpAsync () =
    //    let path = Path.GetTempFileName()
    //    async {
    //        let junk = Array.zeroCreate bufferSize
    //        use file = File.Create(path)
    //        for i = 1 to manyIterations do
    //            do! Async.AwaitTask(file.WriteAsync(junk, 0, junk.Length))
    //    }
    //    |> Async.RunSynchronously
    //    File.Delete(path)

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark(Baseline=true)>]
    member _.NonAsyncBinds_CSharpTasks() = 
         for i in 1 .. manyIterations*100 do 
             TaskPerfCSharp.TenBindsSync_CSharp().Wait() 

    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_Task() = 
        for i in 1 .. manyIterations*100 do 
             tenBindSync_task().Wait() 


    [<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    member _.NonAsyncBinds_TaskUsingTaskBuilder() = 
        for i in 1 .. manyIterations*100 do 
             tenBindSync_taskBuilder().Wait() 

    //[<BenchmarkCategory("NonAsyncBinds"); Benchmark>]
    //member _.NonAsyncBinds_FSharpAsync() = 
    //    for i in 1 .. manyIterations*100 do 
    //         tenBindSync_async() |> Async.RunSynchronously |> ignore

    [<BenchmarkCategory("AsyncBinds"); Benchmark(Baseline=true)>]
    member _.AsyncBinds_CSharpTasks() = 
         for i in 1 .. manyIterations do 
             TaskPerfCSharp.TenBindsAsync_CSharp().Wait() 

    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.AsyncBinds_Task() = 
         for i in 1 .. manyIterations do 
             tenBindAsync_task().Wait() 


    [<BenchmarkCategory("AsyncBinds"); Benchmark>]
    member _.AsyncBinds_TaskUsingTaskBuilder() = 
         for i in 1 .. manyIterations do 
             tenBindAsync_taskBuilder().Wait() 

    //[<Benchmark>]
    //member _.AsyncBinds_FSharpAsync() = 
    //     for i in 1 .. manyIterations do 
    //         tenBindAsync_FSharpAsync() |> Async.RunSynchronously 

    [<BenchmarkCategory("SingleSyncTask"); Benchmark(Baseline=true)>]
    member _.SingleSyncTask_CSharpTasks() = 
         for i in 1 .. manyIterations*500 do 
             TaskPerfCSharp.SingleSyncTask_CSharp().Wait() 

    [<BenchmarkCategory("SingleSyncTask"); Benchmark>]
    member _.SingleSyncTask_Task() = 
         for i in 1 .. manyIterations*500 do 
             singleTask_task().Wait() 


    [<BenchmarkCategory("SingleSyncTask"); Benchmark>]
    member _.SingleSyncTask_TaskUsingTaskBuilder() = 
         for i in 1 .. manyIterations*500 do 
             singleTask_taskBuilder().Wait() 

    //[<BenchmarkCategory("SingleSyncTask"); Benchmark>]
    //member _.SingleSyncTask_FSharpAsync() = 
    //     for i in 1 .. manyIterations*500 do 
    //         singleTask_async() |> Async.RunSynchronously |> ignore

    [<BenchmarkCategory("sync"); Benchmark(Baseline=true)>]
    member _.SyncBuilderLoop_NormalCode() = 
        for i in 1 .. manyIterations do 
                    let mutable res = 0
                    for i in Seq.init 1000 id do
                       res <- i + res

    [<BenchmarkCategory("sync"); Benchmark>]
    member _.SyncBuilderLoop_WorkflowCode() = 
        for i in 1 .. manyIterations do 
             sync { let mutable res = 0
                    for i in Seq.init 1000 id do
                       res <- i + res }

    [<BenchmarkCategory("TinyVariableSizedList"); Benchmark(Baseline=true)>]
    member _.TinyVariableSizedList_Builtin() = Tests.ListBuilders.Examples.tinyVariableSizeBuiltin()


    [<BenchmarkCategory("TinyVariableSizedList"); Benchmark()>]
    member _.TinyVariableSizedList_NewBuilder() = Tests.ListBuilders.Examples.tinyVariableSizeNew()


    [<BenchmarkCategory("VariableSizedList"); Benchmark(Baseline=true)>]
    member _.VariableSizedList_Builtin() = Tests.ListBuilders.Examples.variableSizeBuiltin()

    [<BenchmarkCategory("VariableSizedList"); Benchmark>]
    member _.VariableSizedList_NewBuilder() = Tests.ListBuilders.Examples.variableSizeNew()


    [<BenchmarkCategory("FixedSizedList"); Benchmark(Baseline=true)>]
    member _.FixedSizeList_Builtin() = Tests.ListBuilders.Examples.fixedSizeBase()

    [<BenchmarkCategory("FixedSizedList"); Benchmark>]
    member _.FixedSizeList_NewBuilder() = Tests.ListBuilders.Examples.fixedSizeC()


    [<BenchmarkCategory("TinyVariableSizedArray"); Benchmark(Baseline=true)>]
    member _.TinyVariableSizedArray_Builtin() = Tests.ArrayBuilders.Examples.tinyVariableSizeBuiltin()

    [<BenchmarkCategory("TinyVariableSizedArray"); Benchmark>]
    member _.TinyVariableSizedArray_NewBuilder() = Tests.ArrayBuilders.Examples.tinyVariableSizeNew()


    [<BenchmarkCategory("VariableSizedArray"); Benchmark(Baseline=true)>]
    member _.VariableSizedArray_Builtin() = Tests.ArrayBuilders.Examples.variableSizeBuiltin()

    [<BenchmarkCategory("VariableSizedArray"); Benchmark>]
    member _.VariableSizedArray_NewBuilder() = Tests.ArrayBuilders.Examples.variableSizeNew()


    [<BenchmarkCategory("FixedSizedArray"); Benchmark(Baseline=true)>]
    member _.FixedSizeArray_Builtin() = Tests.ArrayBuilders.Examples.fixedSizeBase()


    [<BenchmarkCategory("FixedSizedArray"); Benchmark>]
    member _.FixedSizeArray_NewBuilder() = Tests.ArrayBuilders.Examples.fixedSizeC()


    [<BenchmarkCategory("MultiStepOption"); Benchmark(Baseline=true)>]
    member _.MultiStepOption_OldBuilder() = Tests.OptionBuilders.Examples.multiStepOldBuilder()

    [<BenchmarkCategory("MultiStepOption"); Benchmark>]
    member _.MultiStepOption_NewBuilder() = Tests.OptionBuilders.Examples.multiStepNewBuilder()

    [<BenchmarkCategory("MultiStepOption"); Benchmark>]
    member _.MultiStepOption_NoBuilder() = Tests.OptionBuilders.Examples.multiStepNoBuilder()


    [<BenchmarkCategory("MultiStepValueOption"); Benchmark(Baseline=true)>]
    member _.MultiStepValueOption_OldBuilder() = Tests.OptionBuilders.Examples.multiStepOldBuilderV()

    [<BenchmarkCategory("MultiStepValueOption"); Benchmark>]
    member _.MultiStepValueOption_NewBuilder() = Tests.OptionBuilders.Examples.multiStepNewBuilderV()

    [<BenchmarkCategory("MultiStepValueOption"); Benchmark>]
    member _.MultiStepValueOption_NoBuilder() = Tests.OptionBuilders.Examples.multiStepNoBuilderV()


    [<BenchmarkCategory("taskSeq"); Benchmark>]
    member _.NestedForLoops_TaskSeqUsingRawResumableCode() = 
        Tests.TaskSeq.Examples.perf2() |> TaskSeq.iter ignore

    [<BenchmarkCategory("taskSeq"); Benchmark>]
    member _.AsyncSeq_NestedForLoops() = 
        Tests.TaskSeq.Examples.perf2_AsyncSeq() |> AsyncSeq.iter ignore |> Async.RunSynchronously

    [<BenchmarkCategory("taskSeq"); Benchmark(Baseline=true)>]
    member _.NestedForLoops_CSharpAsyncEnumerable() = 
        TaskPerfCSharp.perf2_AsyncEnumerable() |> TaskSeq.iter ignore


module Main = 

    [<EntryPoint>]
    let main argv = 
        printfn "Testing that the tests run..."

        //Benchmarks().ManyWriteFile_CSharpTasks()
        //Benchmarks().ManyWriteFile_Task ()
        //Benchmarks().ManyWriteFile_TaskUsingTaskBuilder ()
        //Benchmarks().ManyWriteFile_FSharpAsync ()
        //Benchmarks().NonAsyncBinds_CSharpTasks() 
        //Benchmarks().NonAsyncBinds_Task() 
        //Benchmarks().NonAsyncBinds_TaskUsingTaskBuilder() 
        //Benchmarks().NonAsyncBinds_FSharpAsync() 
        //Benchmarks().AsyncBinds_CSharpTasks() 
        //Benchmarks().AsyncBinds_Task() 
        //Benchmarks().AsyncBinds_TaskUsingTaskBuilder() 
        //Benchmarks().SingleSyncTask_CSharpTasks()
        //Benchmarks().SingleSyncTask_Task() 
        //Benchmarks().SingleSyncTask_TaskUsingTaskBuilder() 
        //Benchmarks().SingleSyncTask_FSharpAsync()

        //printfn "Sample t1..."
        //Tests.akSeqBuilder.Examples.t1() |> TaskSeq.iter (printfn "t1(): %s")
        //printfn "Sample t2..."
        //Tests.TaskSeqBuilder.Examples.t2() |> TaskSeq.iter (printfn "t2(): %s")
        //printfn "Sample perf1(2)..."
        //Tests.TaskSeqBuilder.Examples.perf1(2) |> TaskSeq.iter (printfn "perf1(2): %d")
        //printfn "Sample perf1(3)..."
        //Tests.TaskSeqBuilder.Examples.perf1(3) |> TaskSeq.iter (printfn "perf1(3): %d") 
        //printfn "Sample perf2..."
        //Tests.TaskSeqBuilder.Examples.perf2() |> TaskSeq.iter (printfn "perf2: %d")

        Tests.TaskSeq.Examples.perf2_AsyncSeq() |> AsyncSeq.toArrayAsync |> Async.RunSynchronously |> Array.sum |> (printf "%d."); printfn ""
        Tests.TaskSeq.Examples.perf2() |> TaskSeq.toArray |> Array.sum |> (printfn "%d.")
        TaskPerfCSharp.perf2_AsyncEnumerable() |> TaskSeq.toArray |> Array.sum |> (printfn "%d.")
        
        printfn "Running benchmarks..."
        let results = BenchmarkRunner.Run<Benchmarks>()
        0  