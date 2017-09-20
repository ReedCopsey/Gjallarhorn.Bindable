namespace Views

open FsXaml
open System.Security
open System.Windows
open System.ComponentModel

// ----------------------------------     View      ---------------------------------- 
// Our platform specific view type
type App = XAML<"App.xaml">
type MainWin = XAML<"MainWindow.xaml">
type RequestView = XAML<"RequestView.xaml">
type ProcessControl = XAML<"ProcessControl.xaml">


type RequestDialogBase = XAML<"RequestDialog.xaml">
type RequestDialog() =
    inherit RequestDialogBase()

    override this.CloseClick (_sender, _e) = this.Close()


type LoginControlBase = XAML<"LoginControl.xaml">
type LoginControl() as self =
    inherit LoginControlBase()

    static let passwordProperty = DependencyProperty.Register("Password", typeof<SecureString>, typeof<LoginControl>, PropertyMetadata(new SecureString()))

    let updateValue (o : LoginControl) (pwd : SecureString) =
        let p : DependencyProperty = LoginControl.PasswordProperty
        o.SetValue(p, pwd)

    do
        self.pw.PasswordChanged |> Observable.add (fun _ -> updateValue self self.pw.SecurePassword)

    member this.Password 
        with get() = (this.GetValue(LoginControl.PasswordProperty) :?> SecureString)
        and set(v : SecureString) = this.SetValue(LoginControl.PasswordProperty, box v)

    static member PasswordProperty = passwordProperty
