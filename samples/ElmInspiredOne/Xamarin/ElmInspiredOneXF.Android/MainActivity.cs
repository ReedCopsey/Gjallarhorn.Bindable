//using System;

//using Android.App;
//using Android.Content.PM;
//using Android.Runtime;
//using Android.Views;
//using Android.Widget;
//using Android.OS;

//namespace CPA.Droid
//{
//    [Activity(Label = "CPA", Icon = "@drawable/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
//    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
//    {
//        protected override void OnCreate(Bundle bundle)
//        {
//            TabLayoutResource = Resource.Layout.Tabbar;
//            ToolbarResource = Resource.Layout.Toolbar;

//            base.OnCreate(bundle);

//            global::Xamarin.Forms.Forms.Init(this, bundle);
//            LoadApplication(new App());
//        }
//    }
//}

using Android.App;
using Android.OS;
using ElmInspiredXF;
using Gjallarhorn.XamarinForms;
using Program = ElmInspiredOne.Program;

namespace CPA.Droid
{
    [Activity(Label = "Elm Inspired One", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);

            var info = Framework.CreateApplicationInfo(Program.applicationCore, new MainPage());
            LoadApplication(info.CreateApp());
        }
    }
}

