using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

using AForge;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Imaging;
using AForge.Imaging.Filters;


namespace FlightSim_AMS
{
    public class MotionDetector
    {
        
        private FilterInfoCollection videoDevices;
        // image processing stuff
        ColorFiltering colorFilter = new ColorFiltering();
        Grayscale grayscaleFilter = Grayscale.CommonAlgorithms.BT709;
        BlobCounter blobCounter = new BlobCounter();
        VideoCaptureDevice videoSource;
        Rectangle objectRect;
        Bitmap image;
        int objectX, objectY;
        int screenHeight, screenWidth;
        int currentSourceID;
        //int redMin = 130;
        //int redMax = 255;

        public MotionDetector(int sh, int sw)
        {
            screenHeight=sh;
            screenWidth=sw;
            GetCameras();
            colorFilter.Red = new IntRange(Convert.ToInt32(130), Convert.ToInt32(255));
            colorFilter.Green = new IntRange(Convert.ToInt32(30), Convert.ToInt32(120));
            colorFilter.Blue = new IntRange(Convert.ToInt32(30), Convert.ToInt32(120));
            AForge.Imaging.RGB fc = new RGB(Color.White);
            //colorFilter.FillColor = fc;
            blobCounter.MinWidth =10;
            blobCounter.MinHeight = 10;
            blobCounter.MaxWidth =200;
            blobCounter.MaxHeight =200;
            blobCounter.FilterBlobs = true;
            blobCounter.ObjectsOrder = ObjectsOrder.Size;
        }

        private void GetCameras(){
            // collect cameras list
            try
            {
                // enumerate video devices
                videoDevices = new FilterInfoCollection( FilterCategory.VideoInputDevice );
                
                if ( videoDevices.Count == 0 ){
                    throw new ApplicationException();}
            }
            catch ( ApplicationException )
            {
                videoDevices = null;
            }
        }

        public void setColorFilterRedMin(int min){
            
            colorFilter.Red = new IntRange(Convert.ToInt32(30), Convert.ToInt32(120));
        }


        public bool isCameraAvailable()
        {
            if (videoDevices != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public String ConnectToFeed()
        {
            if (currentSourceID==(videoDevices.Count-1)){
                currentSourceID=0;
            } else {
                currentSourceID++;
            }
            Connect(currentSourceID);
            return videoDevices[currentSourceID].Name;
        }
        // Connect camera
        private bool Connect(int camID)
        {
            // close previois connection if any
            Disconnect();

            // connect to camera
            //videoSource = new VideoCaptureDevice(videoDevices[camerasCombo.SelectedIndex].MonikerString);
            if (videoDevices!=null)
            {
                videoSource = new VideoCaptureDevice(videoDevices[camID].MonikerString);
            }
            else
            {
                videoSource = new VideoCaptureDevice("dummyvideo"); 
            }
            videoSource.DesiredFrameSize = new Size(320, 240);
            videoSource.DesiredFrameRate = 15;

            videoSource.NewFrame += new AForge.Video.NewFrameEventHandler(videoSource_NewFrame);
            videoSource.Start();

            return true;
        }

        // Disconnect camera
        public void Disconnect()
       {
            // stop camera
           try
           {
               videoSource.SignalToStop();
               videoSource.WaitForStop();
           }
           catch (Exception e) { e.ToString(); }
        }

        public Bitmap GetFrame()
        {
            if (image == null)
            {
                
                return new Bitmap(1, 1);
            }
            else
            {
                return image;
            }
        }

        // New video frame has arrived
        private void videoSource_NewFrame(object sender,NewFrameEventArgs eventArgs )
        {
            image = eventArgs.Frame;
            bool showOnlyObjects = true;//onlyObjectsCheck.Checked;
                Bitmap objectsImage = null;
                // color filtering
                if (showOnlyObjects)
                {
                    objectsImage = image;
                    colorFilter.ApplyInPlace(image);
                }
                else
                {
                    objectsImage = colorFilter.Apply(image);
                }
                // lock image for further processing
                BitmapData objectsData = objectsImage.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly, image.PixelFormat);
                // grayscaling
                UnmanagedImage grayImage = grayscaleFilter.Apply(new UnmanagedImage(objectsData));
                // unlock image
                objectsImage.UnlockBits(objectsData);
                // locate blobs 
                blobCounter.ProcessImage(grayImage);
                Rectangle[] rects = blobCounter.GetObjectsRectangles();
                
            if (rects.Length > 0)
                {
                    objectRect = rects[0];

                    // draw rectangle around derected object
                    //Graphics g = Graphics.FromImage(image);
                    //using (Pen pen = new Pen(Color.FromArgb(160, 255, 160), 3))
                    //{
                    //    g.DrawRectangle(pen, objectRect);
                    //}

                    //g.Dispose();

                    objectX = (objectRect.X + objectRect.Width / 2) * screenWidth / image.Width;
                    objectY = (objectRect.Y + objectRect.Height / 2) * screenHeight / image.Height;
                        //Cursor.Position = new Point(objectX, objectY);

                }
                else
                {
                }

                // free temporary image
                if (!showOnlyObjects)
                {
                    objectsImage.Dispose();
                }
                grayImage.Dispose();
            }

        public int getX()
        {
            return screenWidth- objectX;
        }

        public int getY()
        {
            return objectY;
        }

        public void setRMax(int val)
        {
            colorFilter.Red = new IntRange(Convert.ToInt32(colorFilter.Red.Min), Convert.ToInt32(val));
        }
        public void setGMax(int val)
        {
            colorFilter.Green = new IntRange(Convert.ToInt32(colorFilter.Green.Min), Convert.ToInt32(val));
        }
        public void setBMax(int val)
        {
            colorFilter.Blue = new IntRange(Convert.ToInt32(colorFilter.Blue.Min), Convert.ToInt32(val));
        }
        public void setRMin(int val)
        {
            colorFilter.Red = new IntRange(Convert.ToInt32(val),Convert.ToInt32(colorFilter.Red.Max) );
        }
        public void setGMin(int val)
        {
            colorFilter.Green = new IntRange(Convert.ToInt32(val),Convert.ToInt32(colorFilter.Green.Max) );
        }
        public void setBMin(int val)
        {
            colorFilter.Blue = new IntRange(Convert.ToInt32(val),Convert.ToInt32(colorFilter.Blue.Max) );
        }

        public int getRMax()
        {
            return colorFilter.Red.Max;
        }
        public int getGMax()
        {
            return colorFilter.Green.Max;
        }
        public int getBMax()
        {
            return colorFilter.Blue.Max;
        }
        public int getRMin()
        {
            return colorFilter.Red.Min;
        }
        public int getGMin()
        {
            return colorFilter.Green.Min;
        }
        public int getBMin()
        {
            return colorFilter.Blue.Min;
        }
    }
}
