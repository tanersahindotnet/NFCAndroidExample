using System;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using Android.App;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;

namespace NFCAndroidExample
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
    [IntentFilter(new[] { NfcAdapter.ActionNdefDiscovered, NfcAdapter.ActionTagDiscovered, Intent.CategoryDefault })]
    public class MainActivity : AppCompatActivity
    {
        private NfcAdapter _nfcAdapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);
            var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            //Set NFC Adapter
            _nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (_nfcAdapter == null)
            {
                var alert = new Android.App.AlertDialog.Builder(this).Create();
                alert.SetMessage("NFC is not supported on this device.");
                alert.SetTitle("NFC Unavailable");
                alert.Show();
            }
            else
            {
                //Set events and filters
                var tagDetected = new IntentFilter(NfcAdapter.ActionTagDiscovered);
                var ndefDetected = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
                var techDetected = new IntentFilter(NfcAdapter.ActionTechDiscovered);
                var filters = new[] { ndefDetected, tagDetected, techDetected };

                var intent = new Intent(this, GetType()).AddFlags(ActivityFlags.SingleTop);

                var pendingIntent = PendingIntent.GetActivity(this, 0, intent, 0);

                // Gives your current foreground activity priority in receiving NFC events over all other activities.
                _nfcAdapter.EnableForegroundDispatch(this, pendingIntent, filters, null);
            }
        }

        /// <summary>
        /// If there's a new detection OnNewIntent will catch it
        /// </summary>
        /// <param name="intent"></param>
        protected override void OnNewIntent(Intent intent)
        {
            if (intent.Action != NfcAdapter.ActionTagDiscovered) return;
            var myTag = (Tag)intent.GetParcelableExtra(NfcAdapter.ExtraTag);
            if (myTag == null) return;
            var tagIdBytes = myTag.GetId();
            var tagIdString = ByteArrayToString(tagIdBytes); //Byte array converted to string
            var reverseHex = LittleEndian(tagIdString); // Reversed hex converted to hex
            var cardId = Convert.ToInt64(reverseHex, 16); //Convert to decimal decimal to get the final value
            var alertMessage = new Android.App.AlertDialog.Builder(this).Create();
            alertMessage.SetMessage("CardId:" + cardId); // Here's the id of card
            alertMessage.Show();

            // Extra: Check if there's any Ndef message
            var rawMessages = intent.GetParcelableArrayExtra(NfcAdapter.ExtraNdefMessages);
            if (rawMessages == null) return;
            var msg = (NdefMessage)rawMessages[0];
            // Get NdefRecord which contains the actual data
            var record = msg.GetRecords()[0];
            if (record == null) return;
            // The data is defined by the Record Type Definition (RTD) specification available from http://members.nfc-forum.org/specs/spec_list/
            if (record.Tnf != NdefRecord.TnfWellKnown) return;
            // Get the transmitted data
            var data = Encoding.ASCII.GetString(record.GetPayload());
            alertMessage.SetMessage("Data:" + data); // Here's the id of card
            alertMessage.Show();
        }

        /// <summary>
        /// In my case card id values returned as reversed hex format.
        /// To solve that problem need to convert reversed hex to hex.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private static string LittleEndian(string num)
        {
            var number = Convert.ToInt32(num, 16);
            var bytes = BitConverter.GetBytes(number);
            return bytes.Aggregate("", (current, b) => current + b.ToString("X2"));
        }

        /// <summary>
        ///  Below a code snippet for writing to a discovered tag. This method can be called when a new intent (tag discovered) has been fired.
        /// I'm using the Ndef class to the actual writing of the data to the tag.
        /// The first step is to create the different parts payload, record, and the Ndef message which will be written on the tag.
        /// Don't forget to call the Connect method before writing to the tag, otherwise, an exception will be thrown.
        /// </summary>
        /// <param name="intent"></param>
        /// <param name="content"></param>
        public void WriteToTag(Intent intent, string content)
        {
            if (!(intent.GetParcelableExtra(NfcAdapter.ExtraTag) is Tag tag)) return;
            Ndef ndef = Ndef.Get(tag);
            if (ndef == null || !ndef.IsWritable) return;
            var payload = Encoding.ASCII.GetBytes(content);
            var mimeBytes = Encoding.ASCII.GetBytes("text/plain");
            var record = new NdefRecord(NdefRecord.TnfWellKnown, mimeBytes, new byte[0], payload);
            var ndefMessage = new NdefMessage(new[] { record });

            ndef.Connect();
            ndef.WriteNdefMessage(ndefMessage);
            ndef.Close();
        }

        public static string ByteArrayToString(byte[] ba)
        {
            var shb = new SoapHexBinary(ba);
            return shb.ToString();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            var id = item.ItemId;
            return id == Resource.Id.action_settings || base.OnOptionsItemSelected(item);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}