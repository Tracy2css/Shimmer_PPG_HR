using System;
using System.Drawing;
using System.Threading;
using LSL;
using ShimmerAPI;

namespace LSLExamples
{
    class SendData
    {
        ShimmerLogAndStreamSystemSerialPort shimmer;
        StreamOutlet outlet;

        public static void Main(string[] args)
        {
            SendData p = new SendData();
            p.start();
            
        }

        public void start()
        {
            // // Create stream info and outlet with 7 channels (3 for Accel, 2 for GSR, 1 for PPG)
            using StreamInfo info = new StreamInfo("ShimmerGSRPPG", "GSR_PPG", 6, 51.2, channel_format_t.cf_float32, "Shimmer12345");
            outlet = new StreamOutlet(info);
            
            /*
            while (!Console.KeyAvailable)
            {
                // generate random data and send it
                for (int k = 0; k < data.Length; k++)
                    data[k] = rnd.Next(-100, 100);
                outlet.push_sample(data);
                Thread.Sleep(10);
            }*/
            int enabledSensors = ((int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_A_ACCEL | (int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_GSR | (int)ShimmerBluetooth.SensorBitmapShimmer3.SENSOR_INT_A13);
            //int enabledSensors = ((int)Shimmer.SensorBitmapShimmer3.SENSOR_A_ACCEL | (int)Shimmer.SensorBitmapShimmer3.SENSOR_EXG1_24BIT | (int)Shimmer.SensorBitmapShimmer3.SENSOR_EXG2_24BIT); 

            double samplingRate = 51.2;
            //byte[] defaultECGReg1 = new byte[10] { 0x00, 0xA0, 0x10, 0x40, 0x40, 0x2D, 0x00, 0x00, 0x02, 0x03 }; //see ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG1
            //byte[] defaultECGReg2 = new byte[10] { 0x00, 0xA0, 0x10, 0x40, 0x47, 0x00, 0x00, 0x00, 0x02, 0x01 }; //see ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG2
            byte[] defaultECGReg1 = ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG1; //also see ShimmerBluetooth.SHIMMER3_DEFAULT_TEST_REG1 && ShimmerBluetooth.SHIMMER3_DEFAULT_EMG_REG1
            byte[] defaultECGReg2 = ShimmerBluetooth.SHIMMER3_DEFAULT_ECG_REG2; //also see ShimmerBluetooth.SHIMMER3_DEFAULT_TEST_REG2 && ShimmerBluetooth.SHIMMER3_DEFAULT_EMG_REG2

            shimmer = new ShimmerLogAndStreamSystemSerialPort("ShimmerID1", "COM15", 51.2, 0, 4, enabledSensors, false, false, false, 0, 0, defaultECGReg1, defaultECGReg2, false);
            shimmer.UICallback += this.HandleEvent;
            shimmer.Connect();
            System.Console.WriteLine("IN ABOUT 5 SECONDS STREAMING WILL START AFTER THE BEEP");
            Thread.Sleep(5000);
            System.Console.Beep();

            shimmer.StartStreaming();
        }

        public void HandleEvent(object sender, EventArgs args)
        {
            CustomEventArgs eventArgs = (CustomEventArgs)args;
            int indicator = eventArgs.getIndicator();

            switch (indicator)
            {
                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_STATE_CHANGE:
                    System.Diagnostics.Debug.Write(((ShimmerBluetooth)sender).GetDeviceName() + " State = " + ((ShimmerBluetooth)sender).GetStateString() + System.Environment.NewLine);
                    int state = (int)eventArgs.getObject();
                    if (state == (int)ShimmerBluetooth.SHIMMER_STATE_CONNECTED)
                    {
                        System.Console.WriteLine("Connected");
                    }
                    else if (state == (int)ShimmerBluetooth.SHIMMER_STATE_CONNECTING)
                    {
                        System.Console.WriteLine("Connecting");
                    }
                    else if (state == (int)ShimmerBluetooth.SHIMMER_STATE_NONE)
                    {
                        System.Console.WriteLine("Disconnected");
                    }
                    else if (state == (int)ShimmerBluetooth.SHIMMER_STATE_STREAMING)
                    {
                        System.Console.WriteLine("Streaming");
                    }
                    break;

                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_NOTIFICATION_MESSAGE:
                    // Handle any notification messages here if necessary
                    break;

                case (int)ShimmerBluetooth.ShimmerIdentifier.MSG_IDENTIFIER_DATA_PACKET:
                    // Retrieve the ObjectCluster from the event arguments
                    ObjectCluster objectCluster = (ObjectCluster)eventArgs.getObject();

                    // Get accelerometer data for X, Y, Z axes
                    SensorData dataAccelX = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_X, "CAL");
                    SensorData dataAccelY = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Y, "CAL");
                    SensorData dataAccelZ = objectCluster.GetData(Shimmer3Configuration.SignalNames.LOW_NOISE_ACCELEROMETER_Z, "CAL");

                    // Get GSR data (resistance and conductance)
                    SensorData dataGSR = objectCluster.GetData(Shimmer3Configuration.SignalNames.GSR, "CAL");
                    SensorData dataGSR_Conductance = objectCluster.GetData(Shimmer3Configuration.SignalNames.GSR_CONDUCTANCE, "CAL");

                    // Get PPG data
                    SensorData dataPPG = objectCluster.GetData(Shimmer3Configuration.SignalNames.INTERNAL_ADC_A13, "CAL");

                    // Ensure all data is not null before proceeding
                    if (dataAccelX != null && dataAccelY != null && dataAccelZ != null && dataGSR != null && dataGSR_Conductance != null && dataPPG != null)
                    {
                        // Create an array to hold the data
                        float[] dataArray = new float[6];
                        dataArray[0] = (float)dataAccelX.Data;
                        dataArray[1] = (float)dataAccelY.Data;
                        dataArray[2] = (float)dataAccelZ.Data;
                        dataArray[3] = (float)dataGSR.Data;
                        dataArray[4] = (float)dataGSR_Conductance.Data;
                        dataArray[5] = (float)dataPPG.Data; ;

                        // Print the data to the console (for debugging)
                        System.Console.WriteLine(string.Join(", ", dataArray));

                        // Send the data array to LSL
                        outlet.push_sample(dataArray);
                    }
                        break;
            }
        }

    }
}
