using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace SpeechXNA
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        RecognizerInfo ri;
        KinectAudioSource source;
        KinectSensor kinectSensor;
        SpeechRecognitionEngine sre;
        Stream s;

        Color bgColor = Color.AliceBlue;

        int rMultiplier = 1;
        int gMultiplier = 1;
        int bMultiplier = 1;

        Texture2D kinectRGBVideo;
        SpriteFont font;
        string kinectStatus = "Not connected";
        string speechStatus = "Not connected";

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = 640;
            graphics.PreferredBackBufferHeight = 480;
        }

        protected override void Initialize()
        {
            base.Initialize();
            bgColor = Color.AliceBlue;
            KinectSensor.KinectSensors.StatusChanged += new EventHandler<StatusChangedEventArgs>(KinectSensors_StatusChanged);
            DiscoverKinectSensor();
        }

        private static RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }


        private static void SreSpeechDetected(object sender, SpeechDetectedEventArgs e)
        {

         //   Console.WriteLine("*** speech SreSpeechDetected");

        }

        private static void SreSpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
           
           if (e.Result != null)
            {
                Console.WriteLine("*** speech SreSpeechRecognitionRejected");
            }
        }

        private static void SreSpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
           // Console.WriteLine("*** speech SreSpeechHypothesized");
        }

        private void SreSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {

           // Console.WriteLine("*** speech recog");

            if (e.Result.Confidence >= 0.65)
            {

                Console.WriteLine("*** setColor asked for " + e.Result.Text + ", with a confidence of: " + e.Result.Confidence.ToString());
                speechStatus = "*** setColor asked for " + e.Result.Text + ", with a confidence of: " + e.Result.Confidence.ToString();

                rMultiplier = gMultiplier = bMultiplier = 1;

                switch (e.Result.Text)
                {
                    case "red":
                        rMultiplier = 5;
                        break;
                    case "green":
                        gMultiplier = 5;
                        break;
                    case "blue":
                        bMultiplier = 5;
                        break;
                    case "clear":
                        rMultiplier = gMultiplier = bMultiplier = 1;
                        break;
                }

            }
            else
            {
                Console.WriteLine("Heard something, but the confidence is too low: " + e.Result.Confidence.ToString());
                speechStatus = "Heard something, but the confidence is too low: " + e.Result.Confidence.ToString();
            }
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            kinectRGBVideo = new Texture2D(GraphicsDevice, 1337, 1337);

           // overlay = Content.Load<Texture2D>("overlay");
            font = Content.Load<SpriteFont>("SpriteFont1");
        }

        void kinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {

            using (ColorImageFrame colorImageFrame = e.OpenColorImageFrame())
            {
                if (colorImageFrame != null)
                {
                    byte[] pixelsFromFrame = new byte[colorImageFrame.PixelDataLength];

                    colorImageFrame.CopyPixelDataTo(pixelsFromFrame);

                    Color[] color = new Color[colorImageFrame.Height * colorImageFrame.Width];
                    kinectRGBVideo = new Texture2D(graphics.GraphicsDevice, colorImageFrame.Width, colorImageFrame.Height);

                    // Go through each pixel and set the bytes correctly.
                    // Remember, each pixel got a Red, Green and Blue channel.
                    int index = 0;
                    for (int y = 0; y < colorImageFrame.Height; y++)
                    {
                        for (int x = 0; x < colorImageFrame.Width; x++, index += 4)
                        {
                            color[y * colorImageFrame.Width + x] = new Color(rMultiplier * pixelsFromFrame[index + 2], gMultiplier * pixelsFromFrame[index + 1], bMultiplier * pixelsFromFrame[index + 0]);
                        }
                    }

                    // Set pixeldata from the ColorImageFrame to a Texture2D
                    kinectRGBVideo.SetData(color);
                }
            }
        }

        private void InitializeKinect()
        {
            kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinectSensor.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(kinectSensor_ColorFrameReady);

            try
            {
                kinectSensor.Start();

                // Obtain the KinectAudioSource to do audio capture
                source = kinectSensor.AudioSource;
                source.EchoCancellationMode = EchoCancellationMode.None; // No AEC for this sample
                source.AutomaticGainControlEnabled = false; // Important to turn this off for speech recognition

                ri = GetKinectRecognizer();

                if (ri == null)
                {
                    speechStatus = "Speech is fuxor'd";
                }
                else
                {

                    speechStatus = "using speech and stuff";
                    Console.WriteLine("*** using speech");

                    int wait = 4;
                    while (wait > 0)
                    {
                       wait--;
                       speechStatus = "Device will be ready for speech recognition in " + wait + " seconds.";
                       Thread.Sleep(1000);
                    }

                    kinectSensor.Start();


                }
            }
            catch
            {
                kinectStatus = "Unable to start the Kinect Sensor";
                
            }
          
        }

        private void DiscoverKinectSensor()
        {
            foreach (KinectSensor sensor in KinectSensor.KinectSensors)
            {
                if (sensor.Status == KinectStatus.Connected)
                {
                    // Found one, set our sensor to this
                    kinectSensor = sensor;
                    break;
                }
            }

            if (this.kinectSensor == null)
            {
                kinectStatus = "Rats! Found no Kinect Sensors connected to USB";
                speechStatus = "Cannot find microphone";
                return;
            }

            // You can use the kinectSensor.Status to check for status
            // and give the user some kind of feedback
            switch (kinectSensor.Status)
            {
                case KinectStatus.Connected:
                    {
                        kinectStatus = "Status: Connected";
                        break;
                    }
                case KinectStatus.Disconnected:
                    {
                        kinectStatus = "Status: Disconnected";
                        break;
                    }
                case KinectStatus.NotPowered:
                    {
                        kinectStatus = "Status: Connect the power";
                        break;
                    }
                default:
                    {
                        kinectStatus = "Status: Error";
                        break;
                    }
            }

            // Init the found and connected device
            if (kinectSensor.Status == KinectStatus.Connected)
            {
                InitializeKinect();
            }

            sre = new SpeechRecognitionEngine(ri.Id);

            var colors = new Choices();
            colors.Add("red");
            colors.Add("green");
            colors.Add("blue");
            colors.Add("clear");

            var gb = new GrammarBuilder { Culture = ri.Culture };
            gb.Culture = ri.Culture;

            // Specify the culture to match the recognizer in case we are running in a different culture.                                 
            gb.Append(colors);

            // Create the actual Grammar instance, and then load it into the speech recognizer.
            var g = new Grammar(gb);

            sre.LoadGrammar(g);
            sre.SpeechRecognized += SreSpeechRecognized;
            sre.SpeechHypothesized += SreSpeechHypothesized;
            sre.SpeechRecognitionRejected += SreSpeechRecognitionRejected;
            sre.SpeechDetected += SreSpeechDetected;

            s = source.Start();

            sre.SetInputToAudioStream(
                        s, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            sre.RecognizeAsync(RecognizeMode.Multiple);
            speechStatus = "Recognizing speech.";
            
        }

        void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (this.kinectSensor == e.Sensor)
            {
                if (e.Status == KinectStatus.Disconnected ||
                    e.Status == KinectStatus.NotPowered)
                {
                    this.kinectSensor = null;
                    this.DiscoverKinectSensor();
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            base.Update(gameTime);
  
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();
            spriteBatch.Draw(kinectRGBVideo, new Rectangle(0, 0, 640, 480), Color.White);
            spriteBatch.DrawString(font, speechStatus, new Vector2(10, 5), Color.White);
            spriteBatch.DrawString(font, kinectStatus, new Vector2(10, 20), Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }
        
        protected override void UnloadContent()
        {
            kinectSensor.Stop();
            kinectSensor.Dispose();
        }

    }
}
