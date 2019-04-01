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
        
        let issue21Btn = window.FindButton("Issue21")
        issue21Btn.Click()
        Wait.For(TimeSpan.FromSeconds(1.0))

        let btn = window.FindButton("1")
        btn.Click()

        // to make sure that there is no any exception that kills app
        Wait.For(TimeSpan.FromSeconds(5.0))
        Assert.IsFalse(application.HasExited)

        application.Close() |> ignore
        Assert.IsTrue(application.HasExited)

    [<Test>]
    let ``issue: #24 - ModalDialog doesn't call dispose for the DataContext of a window. #24``() = 
        let current = TestContext.CurrentContext.WorkDirectory
        let path = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", "..", appPath))
        Assert.True(File.Exists(path))

        use application = Application.AttachOrLaunch(path)
        let window = application.GetMainWindow(new Nullable<TimeSpan>(TimeSpan.FromSeconds(15.0)))

        Assert.AreEqual("Ui tests", window.Title)
        
        let issue24Btn = window.FindButton("Issue24")
        issue24Btn.Click()
        Wait.For(TimeSpan.FromSeconds(1.0))

        let lb = window.FindListBox()
        Assert.AreEqual(5, lb.Items.Count)

        let addBtn = window.FindButton("AddRandom")
        addBtn.Click()

        Wait.For(TimeSpan.FromSeconds(1.0))
        Assert.AreEqual(window.ModalWindows.Count, 1)

        let dialogWnd = window.ModalWindows |> Seq.head
        
        Assert.IsTrue(dialogWnd.Title.StartsWith("Current length"))

        let confirmBtn = dialogWnd.FindButton("Confirm")
        confirmBtn.Click()

        Wait.For(TimeSpan.FromSeconds(1.0))

        Assert.AreEqual(6, lb.Items.Count)

        application.Close() |> ignore
        Assert.IsTrue(application.HasExited)