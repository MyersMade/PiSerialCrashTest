## Pi Serial Crash Test

This code collection is here to prove/disprove an issue I've encountered when trying to send data out a serial port using a USB to RS-232 serial device using Windows.Devices.SerialCommunication in a UWP app design to run in Windows IOT Core on a Raspberry Pi 3.

### Application Oerview

This application loads a listview with 100 text string items and sets the first one as active.

Two buttons are provided to the user:

- Single Line Feed : This button sends the currently selected line to the serial port.
- Continuous Feed : This button acts as a run/pause button for send all of the text strings in order to the serial port.

### Testing Hardware

- Windows 10 Desktop PC
- Raspberry Pi 3 with Windows IOT Core Ver. 10.0.14393.953
- External USB to Serial Converter (FTDI FT232RL chipset - FT232R USB UART)
- Null Modem Cable to Target RS-232 Device (i.e. Windows PC with Terminal Emulator such as CoolTerm)

### Issue

When this application is run on the Windows 10 Desktop PC it functions as intened.

When ran from the Raspberry Pi, the application functions as intended, except for when the target RS-232 device has it's RTS line low (or serial cable unplugged).  At that point the IOT OS will hang and eventually go to a blue screen with a watchdog fault.

It is intended to examine the value returned by the .StoreAsync() method following it's execution or timeout based on the .WriteTimeout parameter. 

```markdown
                Task<UInt32> storeAsyncTask;
                storeAsyncTask = PortDataWriter.StoreAsync().AsTask();    //<----This is where it hangs in IOT Core
                uint x = await storeAsyncTask;
```

This works properly in the Windows Desktop environment, but the line above is what appears to cause the Windows IOT Core enviroment to hang.
