namespace Gjallarhorn.Bindable.Tests.Ui

module Collection = 
    open NUnit.Framework
    open Gu.Wpf.UiAutomation
    open System.Windows
    open System.IO
    open System

    let appPath = @"samples\DemoForUiTests\bin\Debug\net471\DemoForUiTests.exe"

    [<Test>]
    let ``issue: #21 - InvalidOperationException: Added item does not appear at given index '1'``() = 
        let current = TestContext.CurrentContext.WorkDirectory
        let path = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", "..", appPath))
        Assert.True(File.Exists(path))

        use application = Application.AttachOrLaunch(path)
        let window = application.GetMainWindow(new Nullable<TimeSpan>(TimeSpan.FromSeconds(15.0)))

        Assert.AreEqual("Ui tests", window.Title)
        
        let btn = window.FindButton("1")
        btn.Click()

        // to make sure that there is no any exception that kills app
        Wait.For(TimeSpan.FromSeconds(5.0))
        Assert.IsFalse(application.HasExited)

        application.Close() |> ignore
        Assert.IsTrue(application.HasExited)