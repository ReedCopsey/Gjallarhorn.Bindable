namespace Gjallarhorn.Bindable.Tests.Ui

module Collection = 
    open NUnit.Framework
    open Gu.Wpf.UiAutomation
    open System.Windows
    open System.Diagnostics
    open System.IO
    open System

    let local = "Gjallarhorn.Bindable"
    let appPath = @"\samples\DemoForUiTests\bin\Debug\net471\DemoForUiTests.exe"

    // needs clean up
    let rec navigateToParent name path = 
        let parent = Directory.GetParent(path)
        if parent = null then None
        elif parent.Name = name then
            parent.FullName |> Some
        else 
            navigateToParent name parent.FullName

    [<Test>]
    let ``issue: #21 - InvalidOperationException: Added item does not appear at given index '1'``() = 
        let current = TestContext.CurrentContext.WorkDirectory
        let parent = navigateToParent local current
        Assert.IsTrue(parent.IsSome)
        let path = parent.Value + appPath
        Assert.True(File.Exists(path))

        use application = Application.AttachOrLaunch(path)
        let window = application.GetMainWindow()
        window.WaitUntilResponsive()
        Wait.UntilInputIsProcessed()

        let first = Point(250.,300.)
        let second = Point(400.0, 300.0)

        Wait.For(TimeSpan.FromSeconds(0.5))
        Mouse.MoveTo(first)
        Mouse.LeftClick(first)
        Wait.For(TimeSpan.FromSeconds(0.5))
        Mouse.MoveTo(second)
        Mouse.LeftClick(second)
        Wait.For(TimeSpan.FromSeconds(0.5))
        Mouse.MoveTo(first)
        Mouse.LeftClick(first)
        Wait.For(TimeSpan.FromSeconds(0.5))
        Mouse.MoveTo(second)
        Mouse.LeftClick(second)
        Wait.For(TimeSpan.FromSeconds(0.5))
        Mouse.MoveTo(first)
        Mouse.RightClick(first)
        Wait.For(TimeSpan.FromSeconds(0.5))
        Mouse.MoveTo(second)
        Mouse.RightClick(second)        
        Wait.For(TimeSpan.FromSeconds(0.5))       
        Mouse.MoveTo(first)
        Mouse.LeftClick(first)

        // to make sure that there is no any exception that kills app
        Wait.For(TimeSpan.FromSeconds(15.0))
        Assert.IsFalse(application.HasExited)

        application.Close() |> ignore
        Assert.IsTrue(application.HasExited)