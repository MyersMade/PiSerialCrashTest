using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace PiSerialCrashTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //-----Declare page's class level variables

        private bool EnableFeed = false;
        private bool FeedActive = false;

        DeviceInformationCollection DeviceList;
        SerialDevice Port;
        DataWriter PortDataWriter;
        CoreDispatcherPriority DispatchPriority = CoreDispatcherPriority.Normal;

        //-----Execute at page load-----
        public MainPage()
        {
            this.InitializeComponent();
            
            for (int i = 0; i<=99; i++)
            { FilePreview.Items.Add((string)"Text to send line " + i.ToString()); }
            FilePreview.SelectedIndex = 0;
            FilePreview.ScrollIntoView(FilePreview.SelectedItem);
        }

        //-----Open com port when page is navigated to
        async protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            DeviceList = await DeviceInformation.FindAllAsync(SerialDevice.GetDeviceSelector());

            if (!DeviceList[0].Id.ToString().Contains("ACPI"))
            { Port = await SerialDevice.FromIdAsync(DeviceList[0].Id); }
            else
            { Port = await SerialDevice.FromIdAsync(DeviceList[1].Id); }
            
            Port.BaudRate = 9600;
            Port.DataBits = 8;
            Port.Parity = SerialParity.None;
            Port.StopBits = SerialStopBitCount.One;
            Port.Handshake = SerialHandshake.RequestToSend;

            Port.ReadTimeout = TimeSpan.FromMilliseconds(1000);
            Port.WriteTimeout = TimeSpan.FromMilliseconds(1000);

            PortDataWriter = new DataWriter(Port.OutputStream);

            Port.PinChanged += MyPinChanged;

            base.OnNavigatedTo(e);
        }

        //-----Close Com Port when page is navigated away from
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            //await PortDataWriter.FlushAsync();
            PortDataWriter.DetachStream();
            PortDataWriter = null;
            Port.Dispose();

            base.OnNavigatingFrom(e);
        }

        //-----Handle Single Line Feed Button-----
        private void SingleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!FeedActive)
            {
                EnableFeed = false;

                Task t1 = new Task(async () => { await FeedTask(); });
                t1.Start();
                t1.Wait();
            }
        }

        //-----Handle Continuous Feed Button-----
        private void FeedBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!EnableFeed)
            {
                if (!FeedActive)
                {
                    FilePreview.IsEnabled = false;
                    SingleBtn.IsEnabled = false;
                    FeedBtn.Icon = new SymbolIcon(Symbol.Pause);

                    EnableFeed = true;

                    Task t1 = new Task(async () => { await FeedTask(); }, TaskCreationOptions.LongRunning);
                    t1.Start();
                }
            }
            else
            {
                FilePreview.IsEnabled = true;
                SingleBtn.IsEnabled = true;
                FeedBtn.Icon = new SymbolIcon(Symbol.Play);

                EnableFeed = false;
            }
        }

        //-----Task for Feeding Lines to Serial Port
        private async Task FeedTask()
        {
            FeedActive = true;

            string s = "";
            int i = 0;
            int c = 0;

            await this.Dispatcher.RunAsync(DispatchPriority, () =>
            {
                i = FilePreview.SelectedIndex;
                c = FilePreview.Items.Count;
            });

            while (i < c)
            {
                //*****Update Controls in the UI thread*****
                await this.Dispatcher.RunAsync(DispatchPriority, () =>
                {
                    s = FilePreview.SelectedItem.ToString();
                });

                s = s += "\r\n";
                PortDataWriter.WriteString(s);

                Task<UInt32> storeAsyncTask;
                storeAsyncTask = PortDataWriter.StoreAsync().AsTask();
                uint x = await storeAsyncTask;

                if (x != 0)
                {
                    i++;

                    if (i < c)
                    {
                        //*****Update Controls in the UI thread*****
                        await this.Dispatcher.RunAsync(DispatchPriority, () =>
                        {
                            FilePreview.SelectedIndex = i;
                            FilePreview.ScrollIntoView(FilePreview.SelectedItem);
                        });
                    }

                    else
                    {
                        //*****Update Controls in the UI thread*****
                        await this.Dispatcher.RunAsync(DispatchPriority, () =>
                        {
                            FilePreview.SelectedIndex = 0;
                            FilePreview.ScrollIntoView(FilePreview.SelectedItem);
                        });

                        EnableFeed = false;
                    }
                }

                if (EnableFeed == false) { break; }

            }

            //*****Update Controls in the UI thread*****
            await this.Dispatcher.RunAsync(DispatchPriority, () => 
            {
                SingleBtn.IsEnabled = true;
                FeedBtn.Icon = new SymbolIcon(Symbol.Play);
            });

            FeedActive = false;
        }

        private void MyPinChanged(SerialDevice serialdevice, PinChangedEventArgs pin)
        {
            System.Diagnostics.Debug.WriteLine("which pin changed:" + pin.PinChange.ToString());
        }
    }
}
