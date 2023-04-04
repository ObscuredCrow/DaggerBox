using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace test
{
    public partial class Window1 : Window
    {
        #region DLL Import

        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Ansi)]
        public static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        #endregion DLL Import

        #region Global Variables

        public static double screenX = 0;
        public static double screenY = 0;
        public static string myName;
        private string checkName = myName.Replace("wdwWindow", "");
        private string checkForeground;

        #endregion Global Variables

        #region Initialization

        public Window1()
        {
            InitializeComponent();

            #region Mouse Timer

            DispatcherTimer dt = new DispatcherTimer();                                                 //Create the timer.
            dt.Tick += new EventHandler(timer_tick);                                                    //Create the run timer.
            dt.Interval = new TimeSpan(0, 0, 0, 0, 15);                                                 //Set the run time for timer.
            dt.Start();                                                                                 //Start our mouse timer.

            #endregion Mouse Timer
        }

        #endregion Initialization

        #region Mouse Timer

        //Timer to make sure our image duplicates mouse location on all sessions.
        private void timer_tick(object sender, EventArgs e)
        {
            if (System.Windows.Forms.Control.IsKeyLocked(MainWindow.lockVariables))                                 //Check if our lock key is active.
                if(MainWindow.lockVariablesM != Key.None)
                    if (Keyboard.IsKeyDown(MainWindow.lockVariablesM))                                          //Check if our mouse lock key is active.
                    {
                        checkForeground = MainWindow.foregroundGame.Replace(MainWindow.gameName, "");           //Set our foreground check for comparing later.

                        if (checkForeground != checkName)
                        {
                            imgMouse.Visibility = Visibility.Visible;                                           //Make our mouse duplicate image active.

                            double toXR = canMain.ActualWidth;                                                  //Grab our x resolution from the overlay.
                            double toYR = canMain.ActualHeight;                                                 //Grab our y resolution from the overlay.
                            double tempX = MainWindow.mouseX - MainWindow.activeXP;                             //Set our temp x position of the mouse.
                            double tempY = MainWindow.mouseY - MainWindow.activeYP;                             //Set our temp y position of the mouse.
                            screenX = (tempX / MainWindow.activeXR) * toXR;                                     //Perform math to get the ratio for x resolution.
                            screenY = (tempY / MainWindow.activeYR) * toYR;                                     //Perform math to get the ratio for y resolution.

                            Canvas.SetLeft(imgMouse, screenX);                                                  //Set our mouse duplicate x axis.
                            Canvas.SetTop(imgMouse, screenY);                                                   //Set our mouse duplicate y axis.
                        }
                    }
                    else if (MainWindow.lockVariablesMC != Key.None)                                                //Check if our custom mouse lock key is active.
                    {
                        if (Keyboard.IsKeyDown(MainWindow.lockVariablesMC))
                        {
                            string checkTarget = "";                                                                //Create our variable for checking the target.
                            if (MainWindow.target != null)
                                checkTarget = MainWindow.target.Replace("Session ", "");                            //Set our target check for later.

                            if (checkTarget == checkName)                                                           //Check if our target matches the overlay.
                            {
                                imgMouse.Visibility = Visibility.Visible;                                           //Make our mouse duplicate image active.

                                double toXR = canMain.ActualWidth;                                                  //Grab our x resolution from the overlay.
                                double toYR = canMain.ActualHeight;                                                 //Grab our y resolution from the overlay.
                                double tempX = MainWindow.mouseX - MainWindow.activeXP;                             //Set our temp x position of the mouse.
                                double tempY = MainWindow.mouseY - MainWindow.activeYP;                             //Set our temp y position of the mouse.
                                screenX = (tempX / MainWindow.activeXR) * toXR;                                     //Perform math to get the ratio for x resolution.
                                screenY = (tempY / MainWindow.activeYR) * toYR;                                     //Perform math to get the ratio for y resolution.

                                Canvas.SetLeft(imgMouse, screenX);                                                  //Set our mouse duplicate x axis.
                                Canvas.SetTop(imgMouse, screenY);                                                   //Set our mouse duplicate y axis.
                            }
                        }
                        else imgMouse.Visibility = Visibility.Hidden;                                                //Make our mouse duplicate image not active.
                    }
                    else imgMouse.Visibility = Visibility.Hidden;                                                //Make our mouse duplicate image not active.
        }

        #endregion Mouse Timer
    }
}