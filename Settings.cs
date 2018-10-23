using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace AndroidJakDojadevol1
{

    class Settings : DialogFragment
    {
        string CurentSpeed;
        string CurrenttimeChange;
        private ArrayAdapter speedAdapter;
        private ArrayAdapter timeChangeAdapter;
        List<int> speed;
        List<int> timeChange;
        static private Spinner speedSpinner;
        static private Spinner timesChangeSpinner;
        private Button saveButton;
        static public ISharedPreferences pref = Application.Context.GetSharedPreferences("UserInfo", FileCreationMode.Private);


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {

            
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = inflater.Inflate(Resource.Layout.ConfigView, container, false);
            speedSpinner = view.FindViewById<Spinner>(Resource.Id.SpeedSpinner);
            timesChangeSpinner = view.FindViewById<Spinner>(Resource.Id.ChangeSpinner);
            speed = GenDate(1, 12, 1);
            timeChange = GenDate(0, 10, 1);
            speedAdapter = new ArrayAdapter(this.Activity, Android.Resource.Layout.SimpleListItem1, speed);
            timeChangeAdapter = new ArrayAdapter(this.Activity, Android.Resource.Layout.SimpleListItem1, timeChange);
            speedSpinner.Adapter = speedAdapter;
            timesChangeSpinner.Adapter = timeChangeAdapter;
            saveButton = view.FindViewById<Button>(Resource.Id.SaveButton);
            saveButton.Click += delegate
            {
                save();

                Toast.MakeText(this.Activity, "Ustawienia zapisane", ToastLength.Long).Show();

            };
            CurentSpeed = pref.GetString("WalkingSpeed", string.Empty);
            CurrenttimeChange = pref.GetString("MinimalTimeChanged", string.Empty);
            speedSpinner.SetSelection(Int32.Parse(CurentSpeed)-1);
            timesChangeSpinner.SetSelection(Int32.Parse(CurrenttimeChange));
            

            return view;
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            this.Dialog.Window.RequestFeature(WindowFeatures.NoTitle);
            this.Dialog.Window.SetGravity(GravityFlags.Center);
            base.OnActivityCreated(savedInstanceState);
            int Width = (Resources.DisplayMetrics.WidthPixels) - 150;
            int Height = (Resources.DisplayMetrics.HeightPixels) - 970;
            this.Dialog.Window.Attributes.Width = Width;
            this.Dialog.Window.Attributes.Height = Height;
        }

        static public List<int> GenDate(int begin, int end, int range)
        {
            List<int> h = new List<int>();
            for (int i = begin; i < end; i= i + range)
                h.Add(i);
            return h;

        }

        public void save()
        {
            ISharedPreferencesEditor editor = pref.Edit();
            editor.PutString("WalkingSpeed", speedSpinner.SelectedItem.ToString());
            editor.PutString("MinimalTimeChanged", timesChangeSpinner.SelectedItem.ToString());
            editor.Apply();
            
        }
    }
}