using Gma.System.MouseKeyHook;
using MahApps.Metro.Controls;
using SimWinInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using System.Security.Cryptography;
using Utilities;

namespace test
{
    public partial class MainWindow : MetroWindow
    {
        #region DLL Import

        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Ansi)]
        public static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", EntryPoint = "SetWindowText", CharSet = CharSet.Ansi)]
        public static extern bool SetWindowText(IntPtr hWnd, String strNewWindowName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Winapi)]
        public static extern short GetKeyState(int keyCode);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT pPoint);

        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("KERNEL32.DLL", EntryPoint = "RtlZeroMemory")]
        public static extern bool ZeroMemory(IntPtr Destination, int Length);

        #endregion DLL Import

        #region Global Variables

        #region Bools

        private bool clickable = true;                                                                      //Create bool for allowing clicks to pass.

        #endregion Bools

        #region Events

        public static IKeyboardMouseEvents m_GlobalHook;                                                    //Variable for our global mouse hooks.
        private globalKeyboardHook gkh = new globalKeyboardHook();                                          //Variable for our global key hooks.
        private Keys[] allKeys = null;                                                                      //Variable for all keys.
        public static Keys lockVariables = Keys.Scroll;                                                     //Variable to hold our current lock method.
        public static Key lockVariablesM = Key.LeftShift;                                                   //Variable to hold our current lock method.
        public static Key lockVariablesMC = Key.LeftCtrl;                                                   //Variable to hold our current lock method.
        public static IntPtr foreground;                                                                    //Our foreground windows process id.

        private Keys[] movementKeys = new Keys[]
        { Keys.Left, Keys.Down, Keys.Right, Keys.Up, Keys.A, Keys.S, Keys.D, Keys.W, Keys.Q, Keys.E };      //Add our movement keys into an array from the start.

        #endregion Events

        #region Integers

        public static int characterCount = 0;                                                               //Count of how many characters are being used.
        public static double mouseX = 0;                                                                    //Mouse pointers x position.
        public static double mouseY = 0;                                                                    //Mouse pointers y position.
        public static float activeXR = 1920;                                                                //Active windows x resolution.
        public static float activeYR = 1080;                                                                //Active windows y resolution.
        public static float activeXP = 0;                                                                   //Active windows x position.
        public static float activeYP = 0;                                                                   //Active windows y position.
        private float switchXR = 1920;                                                                      //Active windows x resolution.
        private float switchYR = 1080;                                                                      //Active windows y resolution.
        private float switchXP = 0;                                                                         //Active windows x position.
        private float switchYP = 0;                                                                         //Active windows y position.
        private const uint WM_KEYDOWN = 0x100;                                                              //Hex key for the key down event.
        private const uint WM_KEYUP = 0x0101;                                                               //Hex key for the key up event.
        private int hotkeyIndex = 0;                                                                        //Current custom hotkeys.

        #endregion Integers

        #region Lists

        private List<CheckboxDropdown> keysList = new List<CheckboxDropdown>();                             //List to hold all keys that can be broadcast.
        private List<CheckboxDropdown> movementKeysList = new List<CheckboxDropdown>();                     //List to hold all movement keys that can be broadcast.
        private List<IntPtr> curSlots = new List<IntPtr>();                                                 //List to hold all of our current sessions.

        #endregion Lists

        #region Strings

        public static string gameName;                                                                      //Grab the game name and set it for later use.
        public static string foregroundGame;                                                                //Which window is foreground.
        public static string target;                                                                        //Who is the target for our overlay.
        private string curFormation = "";                                                                   //What is our current formation we're on?
        private string configPath = @".\broadcast_config.ini";                                              //Path and name of the file that we will be saving and loading from.
        private string configPathEn = @".\broadcast_config.ini.aes";                                        //Path and name of the file that we will be saving and loading from.
        private string ecdcps = "$b=8mQWPVTLRMv@Ph!k4anJPkL$#2P5&Fud*P%v&Reh7trEYEaD#UBekC=7v@ua+byqnDjPbMEwpy5HTjrPYQ5aJ!%VHtb2*5SkMLTWw$$!vWfqeynVmJEV^2mFDr*3nJN*@eWcPfch+TQmnrkJ%ukD6KQ*g8aT^_JcEazM-=c@daj=b!NYcVk-9MFkES3%p#!CG#UWHRJzQ259MvM=LcertK%ANmUCpzPfKg%=_SNymqe=6Rg&FV#LQe7%wXe$n";

        #endregion Strings

        #region Timers

        private DispatcherTimer ct = new DispatcherTimer();                                                 //Create the timer.

        #endregion Timers

        #endregion Global Variables

        #region Initialization

        //Loading of our program.
        public MainWindow()
        {
            InitializeComponent();                                                                          //Create ui components.

            Subscribe();                                                                                    //Start our mouse hook.

            #region Mouse Timer

            DispatcherTimer dt = new DispatcherTimer();                                                     //Create the timer.
            dt.Tick += new EventHandler(timer_tick);                                                        //Create the run timer.
            dt.Interval = new TimeSpan(0, 0, 0, 0, 25);                                                     //Set the run time for timer.
            dt.Start();                                                                                     //Start our mouse timer.

            #endregion Mouse Timer

            #region Key Mapping

            allKeys = Enum.GetValues(typeof(Keys)).Cast<Keys>().ToArray();                                  //Grab all keys and put them into an array.

            foreach (Keys hooks in allKeys)                                                                 //Loop through each key in our array.
            {
                gkh.HookedKeys.Add(hooks);                                                                  //Add all keys to our global hook so we can catch them.
                keysList.Add(new CheckboxDropdown(false, hooks.ToString()));                                //Add all keys to our checkbox dropdown list.
                cboFormationKeys.Items.Add(hooks);                                                          //Add all keys to our combobox for formations.
            }

            foreach (Keys key in movementKeys)
            {
                movementKeysList.Add(new CheckboxDropdown(false, key.ToString()));
            }

            ddlBasicBroadcast.ItemsSource = keysList;                                                       //Add all keys to our checkbox dropdown for usage.
            ddlMovementBroadcast.ItemsSource = movementKeysList;                                            //Add all movement keys to our checkbox dropdown for usage.
            gkh.KeyDown += new System.Windows.Forms.KeyEventHandler(gkh_KeyDown);                           //Start our global hook watcher for key presses.
            gkh.KeyUp += new System.Windows.Forms.KeyEventHandler(gkh_KeyUp);                               //Start our global hook watcher for key releases.

            #endregion Key Mapping

            LoadConfig();
        }

        #endregion Initialization

        #region User Interface

        #region OnChange Updates

        //Change our lock key based on the selection made.
        private void cboLockKey_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cboLockKey.SelectedIndex == 0) lockVariables = Keys.Scroll;                                 //Sets our lock variable to scroll lock.
            if (cboLockKey.SelectedIndex == 1) lockVariables = Keys.CapsLock;                               //Sets our lock variable to caps lock.
            if (cboLockKey.SelectedIndex == 2) lockVariables = Keys.NumLock;                                //Sets our lock variable to num lock.
        }

        //On left click in the combobox refresh our hooks.
        private void cboLockKey_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Unsubscribe();                                                                                  //Disconnect our hooks.
            Subscribe();                                                                                    //Reconnect our hooks.
        }

        //Change our left mouse lock key based on the selection made.
        private void cboMLLockKey_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboMLLockKey.SelectedIndex == 0) lockVariablesM = Key.None;
            if (cboMLLockKey.SelectedIndex == 1) lockVariablesM = Key.LeftShift;                            //Sets our lock variable to scroll lock.
            if (cboMLLockKey.SelectedIndex == 2) lockVariablesM = Key.LeftCtrl;                             //Sets our lock variable to caps lock.
            if (cboMLLockKey.SelectedIndex == 3) lockVariablesM = Key.LeftAlt;                              //Sets our lock variable to num lock.
        }

        //Change our custom left mouse lock key based on the selection made.
        private void cboCMKey_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboCMKey.SelectedIndex == 0) lockVariablesMC = Key.None;
            if (cboCMKey.SelectedIndex == 1) lockVariablesMC = Key.LeftShift;                               //Sets our lock variable to scroll lock.
            if (cboCMKey.SelectedIndex == 2) lockVariablesMC = Key.LeftCtrl;                                //Sets our lock variable to caps lock.
            if (cboCMKey.SelectedIndex == 3) lockVariablesMC = Key.LeftAlt;                                 //Sets our lock variable to num lock.
        }

        //Keeps our game name updated so we don't loose focus.
        private void txtGameLayout_TextChanged(object sender, TextChangedEventArgs e)
        {
            gameName = txtGameLayout.Text;                                                                  //Grab the game name and set it for later use.
        }

        //Set our new target to the global variable for overlays.
        private void cboCMTargets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (characterCount < cboCMTargets.Items.Count)
                cboCMTargets.SelectedIndex = -1;

            if (cboCMTargets.SelectedValue != null)
                target = cboCMTargets.SelectedValue.ToString();                                             //Set our new target to the global variable for overlays.
        }

        #endregion OnChange Updates

        #region Buttons

        //Applies the layout settings that have been supplied by the user.
        private void btnApplyLayout_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            for (int a = 0; a < 3; a++)
                if (App.Current.Windows.Count > 1)
                    for (int i = 1; i < App.Current.Windows.Count; i++)
                        if (App.Current.Windows[i].Name == "wdwWindow1")
                            App.Current.Windows[i].Close();

            if (txtGameLayout.Text != "" && characterCount > 0)                                             //Check if the game name was filled out and there is a team.
            {
                #region Setup

                curSlots.Clear();
                int sessionCount = 0;
                int toXP = 0; int toYP = 0; int toXR = 0; int toYR = 0;
                IntPtr wHnd = IntPtr.Zero;

                Process[] processes = Process.GetProcessesByName(gameName);

                foreach (Process p in processes)
                {
                    sessionCount++;

                    if (sessionCount > characterCount)
                        continue;

                    SetWindowText(p.MainWindowHandle, gameName + sessionCount);

                    toXP = Int32.Parse(lstLayoutXPosition.Items[sessionCount - 1].ToString());              //Converts our string into a number.
                    toYP = Int32.Parse(lstLayoutYPosition.Items[sessionCount - 1].ToString());              //Converts our string into a number.
                    toXR = Int32.Parse(lstLayoutXResolution.Items[sessionCount - 1].ToString());            //Converts our string into a number.
                    toYR = Int32.Parse(lstLayoutYResolution.Items[sessionCount - 1].ToString());            //Converts our string into a number.
                    wHnd = FindWindow(null, gameName + sessionCount);                                       //Find window with the current name.
                    MoveWindow(wHnd, toXP, toYP, toXR, toYR, true);                                         //Moves the game window to the supplied location.
                    curSlots.Add(wHnd);

                    IntPtr wndChecker = FindWindow(null, ("Window1" + sessionCount));                       //Find window with the current name.

                    if (wndChecker.ToString() != "0")                                                       //Check if the was a window names this already.
                        continue;                                                                           //If that window was found then don't do anymore.

                    Window1.myName = "wdwWindow" + sessionCount;
                    Window1 win1 = new Window1();                                                           //Create our mouse overlay for display mouse.
                    win1.Show();                                                                            //Show the mouse overlay.
                    win1.Width = toXR;                                                                      //Mouse overlay x resolution.
                    win1.Height = toYR;                                                                     //Mouse overlay y resolution.
                    win1.Left = toXP;                                                                       //Mouse overlay x position.
                    win1.Top = toYP;                                                                        //Mouse overlay y position.
                    SetWindowText(FindWindow(null, "Window1"), ("Window1" + sessionCount));                 //Renames the game to the same name plus current character count.
                }

                #endregion Setup
            }
        }

        //Adds another character to our team.
        private void btnAddLayout_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            characterCount++;                                                                               //Add to our character count.
            lstLayoutCharacter.Items.Add(characterCount.ToString());                                        //Populate which character in our list.
            lstLayoutXPosition.Items.Add("0");                                                              //Populate the starting x position for the screen.
            lstLayoutYPosition.Items.Add("0");                                                              //Populate the starting y position for the screen.
            lstLayoutXResolution.Items.Add("1920");                                                         //Populate the starting x resolution for the screen.
            lstLayoutYResolution.Items.Add("1080");                                                         //Populate the starting y resolution for the screen.
            cboCMTargets.Items.Add("Session " + characterCount);                                            //Populate the target character for our mouse.
        }

        //Removes the newest character from our team.
        private void btnRemoveLayout_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (characterCount > 0)                                                                         //Check if we have any characters.
            {
                cboCMTargets.Items.RemoveAt(characterCount - 1);                                            //Remove the target character for our mouse.
                lstLayoutCharacter.Items.RemoveAt(characterCount - 1);                                      //Remove the character from our list.
                lstLayoutXPosition.Items.RemoveAt(characterCount - 1);                                      //Remove the x position.
                lstLayoutYPosition.Items.RemoveAt(characterCount - 1);                                      //Remove the y position.
                lstLayoutXResolution.Items.RemoveAt(characterCount - 1);                                    //Remove the x resolution.
                lstLayoutYResolution.Items.RemoveAt(characterCount - 1);                                    //Remove the y resolution.
                characterCount--;                                                                           //Remove the character from the count.
            }
        }

        //Commit our modifed values to the correct lists and location.
        private void btnModifyLayout_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (txtXPosition.IsEnabled && txtXPosition.Text != "")                                          //Check if the textbox is enabled and isn't blank.
            {
                lstLayoutXPosition.Items[lstLayoutXPosition.SelectedIndex] =
                    txtXPosition.Text;                                                                      //Set our textbox info to the selected item text.
                txtXPosition.IsEnabled = false;                                                             //Set the textbox to not enabled.
                txtXPosition.Text = "";                                                                     //Set the textbox to blank.
            }

            if (txtYPosition.IsEnabled && txtYPosition.Text != "")                                          //Check if the textbox is enabled and isn't blank.
            {
                lstLayoutYPosition.Items[lstLayoutYPosition.SelectedIndex] =
                    txtYPosition.Text;                                                                      //Set our textbox info to the selected item text.
                txtYPosition.IsEnabled = false;                                                             //Set the textbox to not enabled.
                txtYPosition.Text = "";                                                                     //Set the textbox to blank.
            }

            if (txtXResolution.IsEnabled && txtXResolution.Text != "")                                      //Check if the textbox is enabled and isn't blank.
            {
                lstLayoutXResolution.Items[lstLayoutXResolution.SelectedIndex] =
                    txtXResolution.Text;                                                                    //Set our textbox info to the selected item text.
                txtXResolution.IsEnabled = false;                                                           //Set the textbox to not enabled.
                txtXResolution.Text = "";                                                                   //Set the textbox to blank.
            }

            if (txtYResolution.IsEnabled && txtYResolution.Text != "")                                      //Check if the textbox is enabled and isn't blank.
            {
                lstLayoutYResolution.Items[lstLayoutYResolution.SelectedIndex] =
                    txtYResolution.Text;                                                                    //Set our textbox info to the selected item text.
                txtYResolution.IsEnabled = false;                                                           //Set the textbox to not enabled.
                txtYResolution.Text = "";                                                                   //Set the textbox to blank.
            }

            btnModifyLayout.IsEnabled = false;                                                              //Set the modify button to not enabled.
        }

        //Add a custom broadcast key.
        private void btnAddBroadcast_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox txtHotkeyName =
                new System.Windows.Controls.TextBox();                                                  //Create our textbox for custom hotkey name.
            System.Windows.Controls.ComboBox cboHotkey =
                new System.Windows.Controls.ComboBox();                                                 //Create our combobox for hotkey.
            System.Windows.Controls.ComboBox cboBroadcastKey =
                new System.Windows.Controls.ComboBox();                                                 //Create our combobox for broadcast key.
            System.Windows.Controls.ComboBox cboTarget =
                new System.Windows.Controls.ComboBox();                                                 //Create our combobox for targets.

            List<CheckboxDropdown> characters = new List<CheckboxDropdown>();                           //List to hold all keys that can be broadcast.

            txtHotkeyName.Text = "Custom Hotkey";                                                       //Set our default text for the custom hotkey name.

            foreach (Keys hooks in allKeys)                                                             //Loop through each key in our array.
            {
                cboHotkey.Items.Add(hooks.ToString());                                                  //Add the key hook to our list of broadcastable keys, used for hotkey.
                cboBroadcastKey.Items.Add(hooks.ToString());                                            //Add the key hook to our list of broadcastable keys, used for broadcast.
            }

            for (int i = 0; i < characterCount; i++)                                                    //Loop through each character for setting targets.
            {
                string target = "Session " + (i + 1);                                                   //Create our string for each target.
                characters.Add(new CheckboxDropdown(false, target));                                    //Add all keys to our checkbox dropdown list.
            }

            cboTarget.ItemTemplate = System.Windows.Markup.XamlReader.Parse(
                "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
                    "<StackPanel Orientation=\"Horizontal\">" +
                        "<CheckBox Margin=\"5\" IsChecked=\"{Binding IsChecked}\" />" +
                        "<TextBlock Margin=\"5\" Text=\"{Binding Text}\" />" +
                    "</StackPanel>" +
                "</DataTemplate>") as DataTemplate;                                                     //Set our datatemplate for our custom checkbox dropdown.

            cboTarget.ItemsSource = characters;                                                         //Add all keys to our checkbox dropdown for usage.

            stkBroadcastName.Children.Add(txtHotkeyName);                                               //Add our custom hotkey name.
            stkBroadcastHotkey.Children.Add(cboHotkey);                                                 //Add our custom hotkey.
            stkBroadcastKey.Children.Add(cboBroadcastKey);                                              //Add our custom hotkey broadcast.
            stkBroadcastTarget.Children.Add(cboTarget);                                                 //Add our custom hotkey target.
        }

        //Remove a custom broadcast key that was created.
        private void btnRemoveBroadcast_Click(object sender, RoutedEventArgs e)
        {
            int hotkeyIndex = stkBroadcastName.Children.Count;                                          //Grab our count of custom hotkeys.

            if (hotkeyIndex > 0)                                                                        //Check if we have any custom hotkeys.
            {
                stkBroadcastName.Children.RemoveAt(hotkeyIndex - 1);                                    //Remove the last custom hotkey name.
                stkBroadcastHotkey.Children.RemoveAt(hotkeyIndex - 1);                                  //Remove the last custom hotkey.
                stkBroadcastKey.Children.RemoveAt(hotkeyIndex - 1);                                     //Remove the last custom hotkey broadcast.
                stkBroadcastTarget.Children.RemoveAt(hotkeyIndex - 1);                                  //Remove the last custom hotkey target.
            }
        }

        //On left click in the combobox refresh our hooks.
        private void cboMLLockKey_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Unsubscribe();                                                                              //Disconnect our hooks.
            Subscribe();                                                                                //Reconnect our hooks.
        }

        #endregion Buttons

        #region Menus

        //Closes our application when exit is clicked.
        private void mnuExit_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveConfig();
            Unsubscribe();                                                                              //Disconnect our hooks.
            Close();                                                                                    //Closes our application.
            Environment.Exit(0);                                                                        //Close everything about this applicaiton.
        }

        //Make sure we stop grabbing keys when we close the program.
        private void wdwMain_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveConfig();
            Unsubscribe();                                                                              //Disconnect our hooks.
            Environment.Exit(0);                                                                        //Close everything about this applicaiton.
        }

        //Asks for a program location then launches it as many times as you have characters.
        private void mnuLaunch_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();                                               //Opens a dialog browser.
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)                            //Waits for a selection to be made.
            {
                for (int i = 0; i < characterCount; i++)                                                //Cycles through how many characters we have.
                    Process.Start(dialog.FileName);                                                     //Opens the program for each character.
            }
        }

        #endregion Menus

        #region Setup Tab

        //Allow for modifying the value of the selected item.
        private void lstLayoutXPosition_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            txtXPosition.IsEnabled = true;                                                              //Enable our x position textbox.
            btnModifyLayout.IsEnabled = true;                                                           //Enable our modify button.
            if (lstLayoutXPosition.SelectedItem != null)                                                //Check if we have an item selected.
            {
                txtXPosition.Text = lstLayoutXPosition.SelectedItem.ToString();                         //Fill our textbox with our selected item.
            }
        }

        //Allow for modifying the value of the selected item.
        private void lstLayoutYPosition_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            txtYPosition.IsEnabled = true;                                                              //Enable our y position textbox.
            btnModifyLayout.IsEnabled = true;                                                           //Enable our modify button.
            if (lstLayoutYPosition.SelectedItem != null)                                                //Check if we have an item selected.
            {
                txtYPosition.Text = lstLayoutYPosition.SelectedItem.ToString();                         //Fill our textbox with our selected item.
            }
        }

        //Allow for modifying the value of the selected item.
        private void lstLayoutXResolution_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            txtXResolution.IsEnabled = true;                                                            //Enable our x resolution textbox.
            btnModifyLayout.IsEnabled = true;                                                           //Enable our modify button.
            if (lstLayoutXResolution.SelectedItem != null)                                              //Check if we have an item selected.
            {
                txtXResolution.Text = lstLayoutXResolution.SelectedItem.ToString();                     //Fill our textbox with our selected item.
            }
        }

        //Allow for modifying the value of the selected item.
        private void lstLayoutYResolution_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            txtYResolution.IsEnabled = true;                                                            //Enable our y resolution textbox.
            btnModifyLayout.IsEnabled = true;                                                           //Enable our modify button.
            if (lstLayoutYResolution.SelectedItem != null)                                              //Check if we have an item selected.
            {
                txtYResolution.Text = lstLayoutYResolution.SelectedItem.ToString();                     //Fill our textbox with our selected item.
            }
        }

        #endregion Setup Tab

        #endregion User Interface

        #region Broadcast Keys

        //Watches for all keys that have been released.
        private void gkh_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            #region Movement Keys

            if (System.Windows.Forms.Control.IsKeyLocked(lockVariables))                                //Check if our lock key is on.
                if (txtGameLayout.Text != "" && characterCount > 0)                                     //Check if the game name was filled out and there is a team.
                    foreach (CheckboxDropdown keys in ddlMovementBroadcast.Items)                       //Loop through our basic broadcasts.
                    {
                        if (keys.IsChecked == true && e.KeyCode.ToString() == keys.Text)                //Check if a checked key matches a released key.
                        {
                            Keys passKey = e.KeyCode;                                                   //The hotkey we wish to pass to the background worker.

                            BackgroundWorker keyMovementReleased = new BackgroundWorker();              //Create our background worker so we can send information to another thread.
                            keyMovementReleased.DoWork += keyMovementReleased_DoWork;                   //Progress through the do work function of the background worker.
                            keyMovementReleased.RunWorkerAsync(passKey);                                //Perform the background actions using the background worker.
                        }
                    }

            #endregion Movement Keys
        }

        //Watches for all keys that have been pressed.
        private void gkh_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            #region Basic Broadcasting

            if (System.Windows.Forms.Control.IsKeyLocked(lockVariables))                                //Check if our lock key is on.
                if (txtGameLayout.Text != "" && characterCount > 0)                                     //Check if the game name was filled out and there is a team.
                    foreach (CheckboxDropdown keys in ddlBasicBroadcast.Items)                          //Loop through our basic broadcasts.
                    {
                        if (keys.IsChecked == true && e.KeyCode.ToString() == keys.Text)                //Check if a checked key matches a released key.
                        {
                            Keys passKey = e.KeyCode;                                                   //The hotkey we wish to pass to the background worker.

                            BackgroundWorker keyPressed = new BackgroundWorker();                       //Create our background worker so we can send information to another thread.
                            keyPressed.DoWork += keyPressed_DoWork;                                     //Progress through the do work function of the background worker.
                            keyPressed.RunWorkerAsync(passKey);                                         //Perform the background actions using the background worker.
                        }
                    }

            #endregion Basic Broadcasting

            #region Custom Hotkeys

            hotkeyIndex = stkBroadcastName.Children.Count;                                              //Grab our hotkey index.

            if (System.Windows.Forms.Control.IsKeyLocked(lockVariables))                                //Check if our lock key is on.
                if (txtGameLayout.Text != "" && characterCount > 0)                                     //Check if the game name was filled out and there is a team.
                    for (int i = 0; i < hotkeyIndex; i++)                                               //Loop through our hotkey index.
                    {
                        string hotkey =
                            ((System.Windows.Controls.ComboBox)stkBroadcastHotkey.Children[i]).Text;    //Grab our text for the correct hotkey.
                        string broadcast =
                            ((System.Windows.Controls.ComboBox)stkBroadcastKey.Children[i]).Text;       //Grab our text for the correct broadcast key.

                        Keys broadcastKey;                                                              //Set a keys variable for the broadcast key.
                        Enum.TryParse(broadcast, out broadcastKey);                                     //Make our broadcast key string into a key.

                        if (hotkey == e.KeyCode.ToString())                                             //Check if the hotkey matches the key released.
                            for (int x = 0; x < characterCount; x++)                                    //Loop through the character count for the total team.
                            {
                                //Loop through our targets so we broadcast correctly.
                                foreach (CheckboxDropdown targets in ((System.Windows.Controls.ComboBox)stkBroadcastTarget.Children[i]).Items)
                                {
                                    string findTarget = targets.Text;                                   //Grab our target information.
                                    findTarget = findTarget.Replace("Session ", "");                    //Remove the excess we only need the number.

                                    if (targets.IsChecked == true && findTarget == (x + 1).ToString())  //Check if the session is checked and matches the current character we're on.
                                    {
                                        IntPtr hWnd = FindWindow(null, (gameName + (x + 1)));           //Finds the game window based on the name provided.

                                        if (foreground != hWnd)                                         //Check if the active window is not the same window being broadcast to.
                                        {
                                            //Send our message about the key being pressed.
                                            SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(broadcastKey), IntPtr.Zero);
                                            Thread.Sleep(5);                                            //Wait for 5 milliseconds to make sure key down registers.
                                            //Send our message about the key being released.
                                            PostMessage(hWnd, WM_KEYUP, (IntPtr)(broadcastKey), IntPtr.Zero);
                                        }
                                    }
                                }
                            }
                    }

            #endregion Custom Hotkeys

            #region Movement Keys

            if (System.Windows.Forms.Control.IsKeyLocked(lockVariables))                                //Check if our lock key is on.
                if (txtGameLayout.Text != "" && characterCount > 0)                                     //Check if the game name was filled out and there is a team.
                    foreach (CheckboxDropdown keys in ddlMovementBroadcast.Items)                       //Loop through our basic broadcasts.
                    {
                        if (keys.IsChecked == true && e.KeyCode.ToString() == keys.Text)                //Check if a checked key matches a released key.
                        {
                            Keys passKey = e.KeyCode;                                                   //The hotkey we wish to pass to the background worker.

                            BackgroundWorker keyMovementPressed = new BackgroundWorker();               //Create our background worker so we can send information to another thread.
                            keyMovementPressed.DoWork += keyMovementPressed_DoWork;                     //Progress through the do work function of the background worker.
                            keyMovementPressed.RunWorkerAsync(passKey);                                 //Perform the background actions using the background worker.
                        }
                    }

            #endregion Movement Keys

            #region Formation Keys

            if (System.Windows.Forms.Control.IsKeyLocked(lockVariables))                                //Check if our lock key is on.
                if (txtGameLayout.Text != "" && characterCount > 0)                                     //Check if the game name was filled out and there is a team.
                    if (cboFormationKeys.SelectedValue != null)                                         //Check if we have a value otherwise don't do anything.
                        if (cboFormationKeys.SelectedValue.ToString() == e.KeyCode.ToString())          //Check if a checked key matches a released key.
                        {
                            BackgroundWorker keyFormationPressed = new BackgroundWorker();              //Create our background worker so we can send information to another thread.
                            keyFormationPressed.DoWork += keyFormationPressed_DoWork;                   //Progress through the do work function of the background worker.
                            keyFormationPressed.RunWorkerAsync(curFormation);                           //Perform the background actions using the background worker.
                        }

            #endregion Formation Keys
        }

        //Perform our key press broadcast on a different thread to avoid freezing.
        private void keyPressed_DoWork(object sender, DoWorkEventArgs e)
        {
            Keys passKey = (Keys)e.Argument;                                                            //Recreate our pass over list so we can populate and use it.

            for (int i = 0; i < characterCount; i++)                                                    //Loop through the character count for the total team.
            {
                IntPtr hWnd = FindWindow(null, (gameName + (i + 1)));                                   //Finds the game window based on the name provided.

                if (foreground != hWnd)                                                                 //Check if the active window is not the same window being broadcast to.
                {
                    SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(passKey), IntPtr.Zero);                      //Send our message about the key being pressed.
                    Thread.Sleep(5);                                                                    //Wait for 5 milliseconds to make sure our key pressed was registered.
                    PostMessage(hWnd, WM_KEYUP, (IntPtr)(passKey), IntPtr.Zero);                        //Send our message about the key being released.
                }
            }
        }

        //Perform our movement key press broadcast on a different thread to avoid freezing.
        private void keyMovementPressed_DoWork(object sender, DoWorkEventArgs e)
        {
            Keys passKey = (Keys)e.Argument;                                                            //Recreate our pass over list so we can populate and use it.

            for (int i = 0; i < characterCount; i++)                                                    //Loop through the character count for the total team.
            {
                IntPtr hWnd = FindWindow(null, (gameName + (i + 1)));                                   //Finds the game window based on the name provided.

                if (foreground != hWnd)                                                                 //Check if the active window is not the same window being broadcast to.
                {
                    SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(passKey), IntPtr.Zero);                      //Send our message about the key being pressed.
                }
            }
        }

        //Perform our movement key press broadcast on a different thread to avoid freezing.
        private void keyMovementReleased_DoWork(object sender, DoWorkEventArgs e)
        {
            Keys passKey = (Keys)e.Argument;                                                            //Recreate our pass over list so we can populate and use it.

            for (int i = 0; i < characterCount; i++)                                                    //Loop through the character count for the total team.
            {
                IntPtr hWnd = FindWindow(null, (gameName + (i + 1)));                                   //Finds the game window based on the name provided.

                if (foreground != hWnd)                                                                 //Check if the active window is not the same window being broadcast to.
                {
                    PostMessage(hWnd, WM_KEYUP, (IntPtr)(passKey), IntPtr.Zero);                        //Send our message about the key being pressed.
                }
            }
        }

        //Perform our formation key press broadcast on a different thread to avoid freezing.
        private void keyFormationPressed_DoWork(object sender, DoWorkEventArgs e)
        {
            string passKey = e.Argument.ToString();                                                     //Recreate our pass over list so we can populate and use it.
            int stepCount = 250;                                                                        //Count to change where each character will move to.

            switch (passKey)
            {
                case "Colume":
                    for (int i = 0; i < characterCount; i++)                                            //Loop through the character count for the total team.
                    {
                        IntPtr hWnd = FindWindow(null, (gameName + (i + 1)));                           //Finds the game window based on the name provided.

                        if (foreground != hWnd)                                                         //Check if the active window is not the same window being broadcast to.
                        {
                            SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.S), IntPtr.Zero);               //Send our message about the key being pressed.
                            Thread.Sleep(stepCount);                                                    //Wait for 5 milliseconds to make sure our key pressed was registered.
                            PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.S), IntPtr.Zero);                 //Send our message about the key being released

                            stepCount = stepCount + 250;                                                //Increase our count so each session moves a different amount.
                        }
                    }
                    break;

                case "Line":
                    for (int i = 0; i < characterCount; i++)                                            //Loop through the character count for the total team.
                    {
                        IntPtr hWnd = FindWindow(null, (gameName + (i + 1)));                           //Finds the game window based on the name provided.

                        if (foreground != hWnd)                                                         //Check if the active window is not the same window being broadcast to.
                        {
                            if (i % 2 == 0)                                                             //Check if the number is divisible by 2 with the remainder 0 then even.
                            {
                                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.Q), IntPtr.Zero);           //Send our message about the key being pressed.
                                Thread.Sleep(stepCount);                                                //Wait for 5 milliseconds to make sure our key pressed was registered.
                                PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.Q), IntPtr.Zero);             //Send our message about the key being released

                                stepCount = stepCount + 250;                                            //Increase our count so each session moves a different amount.
                            }
                            else
                            {
                                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.E), IntPtr.Zero);           //Send our message about the key being pressed.
                                Thread.Sleep(stepCount);                                                //Wait for 5 milliseconds to make sure our key pressed was registered.
                                PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.E), IntPtr.Zero);             //Send our message about the key being released

                                stepCount = stepCount + 250;                                            //Increase our count so each session moves a different amount.
                            }
                        }
                    }
                    break;

                case "Wedge":
                    for (int i = 0; i < characterCount; i++)                                            //Loop through the character count for the total team.
                    {
                        IntPtr hWnd = FindWindow(null, (gameName + (i + 1)));                           //Finds the game window based on the name provided.

                        if (foreground != hWnd)                                                         //Check if the active window is not the same window being broadcast to.
                        {
                            if (i % 2 == 0)                                                             //Check if the number is divisible by 2 with the remainder 0 then even.
                            {
                                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.Q), IntPtr.Zero);           //Send our message about the key being pressed.
                                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.S), IntPtr.Zero);           //Send our message about the key being pressed.
                                Thread.Sleep(stepCount);                                                //Wait for 5 milliseconds to make sure our key pressed was registered.
                                PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.S), IntPtr.Zero);             //Send our message about the key being released
                                PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.Q), IntPtr.Zero);             //Send our message about the key being released

                                stepCount = stepCount + 250;                                            //Increase our count so each session moves a different amount.
                            }
                            else
                            {
                                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.E), IntPtr.Zero);           //Send our message about the key being pressed.
                                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.S), IntPtr.Zero);           //Send our message about the key being pressed.
                                Thread.Sleep(stepCount);                                                //Wait for 5 milliseconds to make sure our key pressed was registered.
                                PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.S), IntPtr.Zero);             //Send our message about the key being released
                                PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.E), IntPtr.Zero);             //Send our message about the key being released

                                stepCount = stepCount + 250;                                            //Increase our count so each session moves a different amount.
                            }
                        }
                    }
                    break;

                case "Vee":
                    for (int i = 0; i < characterCount; i++)                                            //Loop through the character count for the total team.
                    {
                        IntPtr hWnd = FindWindow(null, (gameName + (i + 1)));                           //Finds the game window based on the name provided.

                        if (foreground != hWnd)                                                         //Check if the active window is not the same window being broadcast to.
                        {
                            if (i % 2 == 0)                                                             //Check if the number is divisible by 2 with the remainder 0 then even.
                            {
                                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.Q), IntPtr.Zero);           //Send our message about the key being pressed.
                                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.W), IntPtr.Zero);           //Send our message about the key being pressed.
                                Thread.Sleep(stepCount);                                                //Wait for 5 milliseconds to make sure our key pressed was registered.
                                PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.W), IntPtr.Zero);             //Send our message about the key being released
                                PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.Q), IntPtr.Zero);             //Send our message about the key being released

                                stepCount = stepCount + 250;                                            //Increase our count so each session moves a different amount.
                            }
                            else
                            {
                                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.E), IntPtr.Zero);           //Send our message about the key being pressed.
                                SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.W), IntPtr.Zero);           //Send our message about the key being pressed.
                                Thread.Sleep(stepCount);                                                //Wait for 5 milliseconds to make sure our key pressed was registered.
                                PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.W), IntPtr.Zero);             //Send our message about the key being released
                                PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.E), IntPtr.Zero);             //Send our message about the key being released

                                stepCount = stepCount + 250;                                            //Increase our count so each session moves a different amount.
                            }
                        }
                    }
                    break;

                case "LFlank":
                    for (int i = 0; i < characterCount; i++)                                            //Loop through the character count for the total team.
                    {
                        IntPtr hWnd = FindWindow(null, (gameName + (i + 1)));                           //Finds the game window based on the name provided.

                        if (foreground != hWnd)                                                         //Check if the active window is not the same window being broadcast to.
                        {
                            SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.Q), IntPtr.Zero);               //Send our message about the key being pressed.
                            SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.S), IntPtr.Zero);               //Send our message about the key being pressed.
                            Thread.Sleep(stepCount);                                                    //Wait for 5 milliseconds to make sure our key pressed was registered.
                            PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.S), IntPtr.Zero);                 //Send our message about the key being released
                            PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.Q), IntPtr.Zero);                 //Send our message about the key being released

                            stepCount = stepCount + 250;                                                //Increase our count so each session moves a different amount.
                        }
                    }
                    break;

                case "RFlank":
                    for (int i = 0; i < characterCount; i++)                                            //Loop through the character count for the total team.
                    {
                        IntPtr hWnd = FindWindow(null, (gameName + (i + 1)));                           //Finds the game window based on the name provided.

                        if (foreground != hWnd)                                                         //Check if the active window is not the same window being broadcast to.
                        {
                            SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.E), IntPtr.Zero);               //Send our message about the key being pressed.
                            SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.S), IntPtr.Zero);               //Send our message about the key being pressed.
                            Thread.Sleep(stepCount);                                                    //Wait for 5 milliseconds to make sure our key pressed was registered.
                            PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.S), IntPtr.Zero);                 //Send our message about the key being released
                            PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.E), IntPtr.Zero);                 //Send our message about the key being released

                            stepCount = stepCount + 250;                                                //Increase our count so each session moves a different amount.
                        }
                    }
                    break;

                case "Diamond":
                    int x = 1;
                    for (int i = 0; i < characterCount; i++)                                            //Loop through the character count for the total team.
                    {
                        IntPtr hWnd = FindWindow(null, (gameName + (i + 1)));                           //Finds the game window based on the name provided.

                        if (foreground != hWnd)                                                         //Check if the active window is not the same window being broadcast to.
                        {
                            if (i % 2 == 0)                                                             //Check if the number is divisible by 2 with the remainder 0 then even.
                            {
                                if (x % 2 == 0)                                                         //Check if the number is divisible by 2 with the remainder 0 then even.
                                {
                                    SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.W), IntPtr.Zero);       //Send our message about the key being pressed.
                                    Thread.Sleep(stepCount);                                            //Wait for 5 milliseconds to make sure our key pressed was registered.
                                    PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.W), IntPtr.Zero);         //Send our message about the key being released

                                    stepCount = stepCount + 250;                                        //Increase our count so each session moves a different amount.
                                    x++;
                                }
                                else
                                {
                                    SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.S), IntPtr.Zero);       //Send our message about the key being pressed.
                                    Thread.Sleep(stepCount);                                            //Wait for 5 milliseconds to make sure our key pressed was registered.
                                    PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.S), IntPtr.Zero);         //Send our message about the key being released

                                    stepCount = stepCount + 250;                                        //Increase our count so each session moves a different amount.
                                    x++;
                                }
                            }
                            else
                            {
                                if (x % 2 == 0)                                                         //Check if the number is divisible by 2 with the remainder 0 then even.
                                {
                                    SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.E), IntPtr.Zero);       //Send our message about the key being pressed.
                                    Thread.Sleep(stepCount);                                            //Wait for 5 milliseconds to make sure our key pressed was registered.
                                    PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.E), IntPtr.Zero);         //Send our message about the key being released

                                    stepCount = stepCount + 250;                                        //Increase our count so each session moves a different amount.
                                    x++;
                                }
                                else
                                {
                                    SendMessage(hWnd, WM_KEYDOWN, (IntPtr)(Keys.Q), IntPtr.Zero);       //Send our message about the key being pressed.
                                    Thread.Sleep(stepCount);                                            //Wait for 5 milliseconds to make sure our key pressed was registered.
                                    PostMessage(hWnd, WM_KEYUP, (IntPtr)(Keys.Q), IntPtr.Zero);         //Send our message about the key being released

                                    stepCount = stepCount + 250;                                        //Increase our count so each session moves a different amount.
                                    x++;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        #endregion Broadcast Keys

        #region Broadcast Mouse

        #region Start/Stop

        //Starts the session to watch for key and mouse presses and coordinates.
        public void Subscribe()
        {
            m_GlobalHook = Hook.GlobalEvents();                                                         //Activates the hook events.
            m_GlobalHook.MouseDownExt += GlobalHookMouseDownExt;                                        //Adds all of the global mouse hooks.
        }

        //Stops the session that's watching for key and mouse presses and coordinates.
        public void Unsubscribe()
        {
            m_GlobalHook.MouseDownExt -= GlobalHookMouseDownExt;                                        //Removes all of the global mouse hooks.
            m_GlobalHook.Dispose();                                                                     //Removes all excess global hooks.
        }

        #endregion Start/Stop

        //Watches for all mouse related events.
        private void GlobalHookMouseDownExt(object sender, MouseEventExtArgs e)
        {
            int sleepTimer = 100;
            int waitAddition = 200;

            if (System.Windows.Forms.Control.IsKeyLocked(lockVariables))                                    //Check if our lock key is active.
            {
                if (lockVariablesM != Key.None)
                {
                    if (Keyboard.IsKeyDown(lockVariablesM))                                                 //Check if our mouse lock key is active.
                    {
                        if (e.Button == MouseButtons.Left && clickable)                                     //Check if the left mouse was clicked and clickable is true.
                        {
                            clickable = false;                                                              //Make clickable false to avoid overlap.
                            if (txtGameLayout.Text != "" && characterCount > 0)                             //Check if the game name was filled out and there is a team.
                            {
                                double wdwX = (mouseX - activeXP) / activeXR;                               //Find our mouse x position relative to active window.
                                double wdwY = (mouseY - activeYP) / activeYR;                               //Find our mouse y position relative to active window.

                                List<double> positions = new List<double>();                                //Create a list of positions to pass to background worker.
                                positions.Add(wdwX);                                                        //Add our x position to our list.
                                positions.Add(wdwY);                                                        //Add our y position to our list.
                                positions.Add(mouseX);                                                      //Add our x position of the mouse.
                                positions.Add(mouseY);                                                      //Add our y position of the mouse.

                                Thread.Sleep(sleepTimer);                                                   //Wait 100 milliseconds to avoid overlaping clicks from original click.

                                BackgroundWorker leftMouseClick = new BackgroundWorker();                   //Create our background worker so we can send information to another thread.
                                leftMouseClick.DoWork += leftMouseClick_DoWork;                             //Progress through the do work function of the background worker.
                                leftMouseClick.RunWorkerAsync(positions);                                   //Perform the background actions using the background worker.

                                int waitTime = 0;                                                           //Create a wait timer to use later.
                                for (int i = 0; i < characterCount; i++)                                    //Loop through the character count for the total team.
                                    waitTime = waitTime + waitAddition;                                     //Add 200 milliseconds to our timer for each character, avoids overlap.

                                ct.Tick += new EventHandler(cttimer_tick);                                  //Create the run timer.
                                ct.Interval = new TimeSpan(0, 0, 0, 0, waitTime);                           //Set the run time for timer.
                                ct.Start();                                                                 //Start our mouse timer.
                            }
                        }
                        else if (e.Button == MouseButtons.Right)
                        {
                            IntPtr slotOne = FindWindow(null, (gameName + "1"));

                            for (int i = 0; i < characterCount; i++)                                        //Loop through the character count for the total team.
                            {
                                IntPtr wndChecker = FindWindow(null, (gameName + (i + 1)));                 //Find window with the current name.

                                if (foreground == slotOne)
                                    break;

                                if (slotOne == wndChecker)
                                    SetWindowText(slotOne, (gameName + "0"));                               //Renames the game to the same name plus current character count.

                                if (foreground == wndChecker)
                                {
                                    SetWindowText(foreground, (gameName + "1"));                            //Renames the game to the same name plus current character count.
                                    SetWindowText(slotOne, (gameName + (i + 1)));                           //Renames the game to the same name plus current character count.
                                }
                            }

                            for (int i = 0; i < characterCount; i++)                                        //Loop through the character count for the total team.
                            {
                                int toXP = Int32.Parse(lstLayoutXPosition.Items[i].ToString());             //Converts our string into a number.
                                int toYP = Int32.Parse(lstLayoutYPosition.Items[i].ToString());             //Converts our string into a number.
                                int toXR = Int32.Parse(lstLayoutXResolution.Items[i].ToString());           //Converts our string into a number.
                                int toYR = Int32.Parse(lstLayoutYResolution.Items[i].ToString());           //Converts our string into a number.
                                IntPtr wHnd = FindWindow(null, (gameName + (i + 1)));                       //Find window with the current name.
                                MoveWindow(wHnd, toXP, toYP, toXR, toYR, true);                             //Moves the game window to the supplied location.
                                curSlots.Add(wHnd);
                            }
                        }
                    }
                    else if (lockVariablesMC != Key.None)                                                       //Check if our custom mouse lock key is active.
                    {
                        if (Keyboard.IsKeyDown(lockVariablesMC))
                        {
                            if (e.Button == MouseButtons.Left && clickable)                                     //Check if the left mouse was clicked and clickable is true.
                            {
                                clickable = false;                                                              //Make clickable false to avoid overlap.
                                if (txtGameLayout.Text != "" && characterCount > 0)                             //Check if the game name was filled out and there is a team.
                                {
                                    double wdwX = (mouseX - activeXP) / activeXR;                               //Find our mouse x position relative to active window.
                                    double wdwY = (mouseY - activeYP) / activeYR;                               //Find our mouse y position relative to active window.

                                    List<double> positions = new List<double>();                                //Create a list of positions to pass to background worker.
                                    positions.Add(wdwX);                                                        //Add our x position to our list.
                                    positions.Add(wdwY);                                                        //Add our y position to our list.
                                    positions.Add(mouseX);                                                      //Add our x position of the mouse.
                                    positions.Add(mouseY);                                                      //Add our y position of the mouse.

                                    Thread.Sleep(sleepTimer);                                                          //Wait 100 milliseconds to avoid overlaping clicks from original click.

                                    BackgroundWorker cleftMouseClick = new BackgroundWorker();                  //Create our background worker so we can send information to another thread.
                                    cleftMouseClick.DoWork += cleftMouseClick_DoWork;                           //Progress through the do work function of the background worker.
                                    cleftMouseClick.RunWorkerAsync(positions);                                  //Perform the background actions using the background worker.

                                    int waitTime = 0;                                                           //Create a wait timer to use later.

                                    for (int i = 0; i < characterCount; i++)                                    //Loop through the character count for the total team.
                                    {
                                        waitTime = waitTime + waitAddition;                                              //Add 200 milliseconds to our timer for each character, avoids overlap.
                                    }

                                    ct.Tick += new EventHandler(cttimer_tick);                                  //Create the run timer.
                                    ct.Interval = new TimeSpan(0, 0, 0, 0, waitTime);                           //Set the run time for timer.
                                    ct.Start();                                                                 //Start our mouse timer.
                                }
                            }
                        }
                    }
                }
            }
        }

        //Timer for make sure our mouse doesn't overlap on all sessions.
        private void cttimer_tick(object sender, EventArgs e)
        {
            clickable = true;                                                                           //Set clickable to true so we can click in all windows again.
            ct.Stop();                                                                                  //Stop our timer as it's only for reseting clickable.
        }

        //Perform our mouse left click broadcast is on a different thread to avoid freezing.
        private void leftMouseClick_DoWork(object sender, DoWorkEventArgs e)
        {
            List<double> positions = (List<double>)e.Argument;                                          //Recreate our pass over list so we can populate and use it.

            IntPtr main = foreground;

            for (int i = 0; i < characterCount; i++)                                                    //Loop through the character count for the total team.
            {
                IntPtr wndChecker = FindWindow(null, (gameName + (i + 1)));                             //Find window with the current name.

                double xPosition =
                    (positions[0] * Int32.Parse(lstLayoutXResolution.Items[i].ToString())) + Int32.Parse(lstLayoutXPosition.Items[i].ToString());                                //Refine our x position based on the target window.
                double yPosition =
                    (positions[1] * Int32.Parse(lstLayoutYResolution.Items[i].ToString())) + Int32.Parse(lstLayoutYPosition.Items[i].ToString());                                //Refine our y position based on the target window.

                SetForegroundWindow(wndChecker);

                Thread.Sleep(20);                                                                       //Wait 20 milliseconds to register event.
                SimMouse.Act(SimMouse.Action.LeftButtonDown, (int)xPosition, (int)yPosition);           //Send a mouse left click down, to grab window.
                Thread.Sleep(10);                                                                       //Wait 10 milliseconds to register event.
                SimMouse.Act(SimMouse.Action.LeftButtonUp, (int)xPosition, (int)yPosition);             //Send a mouse left click up, to release the mouse.
            }

            SetForegroundWindow(main);
            Thread.Sleep(50);                                                                           //Wait 50 milliseconds to register event.
            SetCursorPos((int)positions[2], (int)positions[3]);
        }

        //Perform our custom mouse left click broadcast is on a different thread to avoid freezing.
        private void cleftMouseClick_DoWork(object sender, DoWorkEventArgs e)
        {
            List<double> positions = (List<double>)e.Argument;                                          //Recreate our pass over list so we can populate and use it.

            IntPtr main = foreground;
            string wndTarget = target.Replace("Session ", "");

            int targetNumber = 0;
            Int32.TryParse(wndTarget, out targetNumber);

            IntPtr wndChecker = FindWindow(null, (gameName + (targetNumber)));                          //Find window with the current name.

            double xPosition =
                (positions[0] * Int32.Parse(lstLayoutXResolution.Items[targetNumber - 1].ToString())) +
                Int32.Parse(lstLayoutXPosition.Items[targetNumber - 1].ToString());                     //Refine our x position based on the target window.
            double yPosition =
                (positions[1] * Int32.Parse(lstLayoutYResolution.Items[targetNumber - 1].ToString())) +
                Int32.Parse(lstLayoutYPosition.Items[targetNumber - 1].ToString());                     //Refine our y position based on the target window.

            Thread.Sleep(20);                                                                           //Wait 5 milliseconds to register event.
            SimMouse.Act(SimMouse.Action.LeftButtonDown, (int)xPosition, (int)yPosition);               //Send a mouse left click down, to grab window.
            Thread.Sleep(10);                                                                           //Wait 5 milliseconds to register event.
            SimMouse.Act(SimMouse.Action.LeftButtonUp, (int)xPosition, (int)yPosition);                 //Send a mouse left click up, to release the mouse.

            SetForegroundWindow(main);
            Thread.Sleep(50);                                                                           //Wait 5 milliseconds to register event.
            SetCursorPos((int)positions[2], (int)positions[3]);
        }

        #region Mouse Location

        //Timer for checking for mouse location and setting its screen value.
        private void timer_tick(object sender, EventArgs e)
        {
            POINT pnt;                                                                                  //Create a point variable to gather the mouse information.
            GetCursorPos(out pnt);                                                                      //Get the mouse location.
            mouseX = pnt.X;                                                                             //Grab the x position of the mouse.
            mouseY = pnt.Y;                                                                             //Grab the y position of the mouse.

            foreground = GetForegroundWindow();                                                         //Grab our current foreground window.

            for (int i = 0; i < characterCount; i++)                                                    //Loop through the character count for the total team.
            {
                IntPtr wndChecker = FindWindow(null, (gameName + (i + 1)));                             //Find window with the current name.

                if (foreground == wndChecker)                                                           //Check if our foreground window is our current window.
                {
                    foregroundGame = gameName + (i + 1);                                                //Set foreground game to the current window.

                    activeXR = Int32.Parse(lstLayoutXResolution.Items[i].ToString());                   //Grab our active window x resolution.
                    activeYR = Int32.Parse(lstLayoutYResolution.Items[i].ToString());                   //Grab our active window y resolution.
                    activeXP = Int32.Parse(lstLayoutXPosition.Items[i].ToString());                     //Converts our string into a number.
                    activeYP = Int32.Parse(lstLayoutYPosition.Items[i].ToString());                     //Converts our string into a number.
                }
            }
        }

        //Create the point variable.
        public struct POINT
        {
            public int X;                                                                               //We need a x axis value.
            public int Y;                                                                               //We need a y axis value.

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        #endregion Mouse Location

        #endregion Broadcast Mouse

        #region Formations

        //Set the variable to the current formation.
        private void rdoColume_Checked(object sender, RoutedEventArgs e)
        {
            curFormation = "Colume";
        }

        //Set the variable to the current formation.
        private void rdoLine_Checked(object sender, RoutedEventArgs e)
        {
            curFormation = "Line";
        }

        //Set the variable to the current formation.
        private void rdoWedge_Checked(object sender, RoutedEventArgs e)
        {
            curFormation = "Wedge";
        }

        //Set the variable to the current formation.
        private void rdoVee_Checked(object sender, RoutedEventArgs e)
        {
            curFormation = "Vee";
        }

        //Set the variable to the current formation.
        private void rdoLFlank_Checked(object sender, RoutedEventArgs e)
        {
            curFormation = "LFlank";
        }

        //Set the variable to the current formation.
        private void rdoRFlank_Checked(object sender, RoutedEventArgs e)
        {
            curFormation = "RFlank";
        }

        //Set the variable to the current formation.
        private void rdoDiamond_Checked(object sender, RoutedEventArgs e)
        {
            curFormation = "Diamond";
        }

        #endregion Formations

        #region Save & Load

        //Allows the file -> save click to call the save method.
        private void mnuSave_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
        }

        //Saves all variables so we can keep track of what the user has filled out.
        private void SaveConfig()
        {
            List<string> saveList = new List<string>();
            string temp = string.Empty;
            string temp2 = string.Empty;

            #region Setup Save

            //Add lock key to save list. 0
            temp = cboLockKey.SelectedIndex.ToString();
            saveList.Add(temp);

            //Add mouse left click lock key to save list. 1
            temp = cboMLLockKey.SelectedIndex.ToString();
            saveList.Add(temp);

            //Add game process name to save list. 2
            saveList.Add(txtGameLayout.Text);

            //Add character count to save list. 3
            saveList.Add(characterCount.ToString());

            //Add characters to save list. 4
            temp2 = "";

            if (characterCount > 0)
            {
                for (int i = 0; i < characterCount; i++)
                {
                    temp = lstLayoutXPosition.Items[i].ToString();
                    temp = temp + "," + lstLayoutYPosition.Items[i].ToString();
                    temp = temp + "," + lstLayoutXResolution.Items[i].ToString();
                    temp = temp + "," + lstLayoutYResolution.Items[i].ToString();
                    temp2 = temp2 + ";" + temp;
                }
                saveList.Add(temp2);
            }
            else
            {
                saveList.Add("null");
            }

            #endregion Setup Save

            #region Broadcast Save

            //Add basic broadcast keys to save list. 5
            temp2 = "";

            foreach (CheckboxDropdown keys in ddlBasicBroadcast.Items)
            {
                temp = keys.IsChecked.ToString() + "," + keys.Text;
                temp2 = temp2 + ";" + temp;
            }
            saveList.Add(temp2);

            //Add movement broadcast keys to save list. 6
            temp2 = "";

            foreach (CheckboxDropdown keys in ddlMovementBroadcast.Items)
            {
                temp = keys.IsChecked.ToString() + "," + keys.Text;
                temp2 = temp2 + ";" + temp;
            }
            saveList.Add(temp2);

            //Add custom hotkeys to save list. 7
            temp2 = "";
            int customCount = stkBroadcastName.Children.Count;

            if (customCount > 0)
            {
                for (int i = 0; i < customCount; i++)
                {
                    System.Windows.Controls.TextBox nmtb = new System.Windows.Controls.TextBox();
                    nmtb = (System.Windows.Controls.TextBox)stkBroadcastName.Children[i];

                    System.Windows.Controls.ComboBox hkcb = new System.Windows.Controls.ComboBox();
                    hkcb = (System.Windows.Controls.ComboBox)stkBroadcastHotkey.Children[i];

                    System.Windows.Controls.ComboBox bkcb = new System.Windows.Controls.ComboBox();
                    bkcb = (System.Windows.Controls.ComboBox)stkBroadcastKey.Children[i];

                    System.Windows.Controls.ComboBox ttcb = new System.Windows.Controls.ComboBox();
                    ttcb = (System.Windows.Controls.ComboBox)stkBroadcastTarget.Children[i];

                    string trans = ""; string trans2 = "";

                    foreach (CheckboxDropdown target in ttcb.Items)
                    {
                        trans = target.IsChecked.ToString() + ":" + target.Text;
                        trans2 = trans2 + "|" + trans;
                    }

                    temp = nmtb.Text;
                    temp = temp + "," + hkcb.SelectedIndex.ToString();
                    temp = temp + "," + bkcb.SelectedIndex.ToString();
                    temp = temp + "," + trans2;
                    temp2 = temp2 + ";" + temp;
                }
                saveList.Add(temp2);
            }
            else
            {
                saveList.Add("null");
            }

            #endregion Broadcast Save

            #region Extras Save

            //Add formation key and formation option to save list. 8
            temp = cboFormationKeys.SelectedIndex.ToString();
            temp = temp + ";" + curFormation;
            saveList.Add(temp);

            //Add custom mouse left click key to save list. 9
            temp = cboCMKey.SelectedIndex.ToString();
            temp = temp + ";" + cboCMTargets.SelectedIndex.ToString();
            saveList.Add(temp);

            #endregion Extras Save

            //Practice Save to File
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }

            foreach (string saved in saveList)
            {
                using (StreamWriter sw = new StreamWriter(configPath, true))
                {
                    sw.WriteLine(saved);
                }
            }

            // For additional security Pin the password of your files
            GCHandle gch = GCHandle.Alloc(ecdcps, GCHandleType.Pinned);

            // Encrypt the file
            //EncryptFile(ecdcps, configPath, configPathEn);
            //File.Delete(configPath);

            // To increase the security of the encryption, delete the given password from the memory !
            ZeroMemory(gch.AddrOfPinnedObject(), ecdcps.Length * 2);
            gch.Free();
        }

        //Allows the file -> load click to call the load method.
        private void mnuLoad_Click(object sender, RoutedEventArgs e)
        {
            LoadConfig();
        }

        //Loads all variables so the user doesn't have to re-enter information.
        private void LoadConfig()
        {
            List<string> loadList = new List<string>();
            string temp = string.Empty;
            int tempNum = 0;

            if (File.Exists(configPathEn))
            {
                // For additional security Pin the password of your files
                GCHandle gch = GCHandle.Alloc(ecdcps, GCHandleType.Pinned);

                // Decrypt the file
                //DecryptFile(ecdcps, configPathEn, configPath);

                // To increase the security of the decryption, delete the used password from the memory !
                ZeroMemory(gch.AddrOfPinnedObject(), ecdcps.Length * 2);
                gch.Free();
            }

            //Practive Load from File
            if (File.Exists(configPath))
            {
                string line;

                StreamReader file = new StreamReader(configPath);
                while ((line = file.ReadLine()) != null)
                {
                    loadList.Add(line);
                }
                file.Close();
            }
            else
                return;

            #region Setup Load

            //Set lock key to saved value. 0
            Int32.TryParse(loadList[0], out tempNum);
            cboLockKey.SelectedIndex = tempNum;

            //Set mouse left click lock key to saved value. 1
            Int32.TryParse(loadList[1], out tempNum);
            cboMLLockKey.SelectedIndex = tempNum;

            //Set game process name to saved value. 2
            txtGameLayout.Text = loadList[2];

            //Set character count to saved value. 3
            Int32.TryParse(loadList[3], out characterCount);

            //Set characters to saved value. 4
            if (characterCount > 0)
            {
                string wnd = loadList[4];

                List<string> wndList = new List<string>(wnd.Split(';'));
                wndList.RemoveAt(0);

                for (int i = 0; i < characterCount; i++)
                {
                    string[] tempArray = wndList[i].Split(',');
                    lstLayoutCharacter.Items.Add(i + 1);
                    lstLayoutXPosition.Items.Add(tempArray[0]);
                    lstLayoutYPosition.Items.Add(tempArray[1]);
                    lstLayoutXResolution.Items.Add(tempArray[2]);
                    lstLayoutYResolution.Items.Add(tempArray[3]);
                    cboCMTargets.Items.Add("Session " + (i + 1));
                }
            }

            #endregion Setup Load

            #region Broadcast Load

            //Set basic broadcast keys to saved value. 5
            string key = loadList[5];
            List<string> keyList = new List<string>(key.Split(';'));
            keyList.RemoveAt(0);
            int basicCount = ddlBasicBroadcast.Items.Count;
            int x = 0;

            foreach (CheckboxDropdown option in ddlBasicBroadcast.Items)
            {
                bool check;
                string[] tempArray = keyList[x].Split(',');

                if (tempArray[0] == "True")
                    check = true;
                else
                    check = false;

                keysList[x].IsChecked = check;

                if (x < basicCount)
                    x++;
            }
            CollectionViewSource.GetDefaultView(ddlBasicBroadcast.ItemsSource).Refresh();

            //Set movement broadcast keys to saved value. 6
            string mokey = loadList[6];
            List<string> moKeyList = new List<string>(mokey.Split(';'));
            moKeyList.RemoveAt(0);
            int moveCount = ddlMovementBroadcast.Items.Count;
            x = 0;

            foreach (CheckboxDropdown option in ddlMovementBroadcast.Items)
            {
                bool check;
                string[] tempArray = moKeyList[x].Split(',');

                if (tempArray[0] == "True")
                    check = true;
                else
                    check = false;

                movementKeysList[x].IsChecked = check;

                if (x < moveCount)
                    x++;
            }
            CollectionViewSource.GetDefaultView(ddlMovementBroadcast.ItemsSource).Refresh();

            //Set custom hotkeys to saved value. 7
            string chwnd = loadList[7];

            List<string> chwndList = new List<string>(chwnd.Split(';'));
            chwndList.RemoveAt(0);
            int chwndCount = chwndList.Count;
            x = 0;

            for (int i = 0; i < chwndCount; i++)
            {
                string[] tempArray = chwndList[i].Split(',');

                System.Windows.Controls.TextBox txtHotkeyName =
                    new System.Windows.Controls.TextBox();                                                  //Create our textbox for custom hotkey name.
                System.Windows.Controls.ComboBox cboHotkey =
                    new System.Windows.Controls.ComboBox();                                                 //Create our combobox for hotkey.
                System.Windows.Controls.ComboBox cboBroadcastKey =
                    new System.Windows.Controls.ComboBox();                                                 //Create our combobox for broadcast key.
                System.Windows.Controls.ComboBox cboTarget =
                    new System.Windows.Controls.ComboBox();                                                 //Create our combobox for targets.

                List<CheckboxDropdown> characters = new List<CheckboxDropdown>();                           //List to hold all keys that can be broadcast.

                txtHotkeyName.Text = tempArray[0];                                                          //Set our default text for the custom hotkey name.

                foreach (Keys hooks in allKeys)                                                             //Loop through each key in our array.
                {
                    cboHotkey.Items.Add(hooks.ToString());                                                  //Add the key hook to our list of broadcastable keys, used for hotkey.
                    cboBroadcastKey.Items.Add(hooks.ToString());                                            //Add the key hook to our list of broadcastable keys, used for broadcast.
                }

                cboHotkey.SelectedIndex = Int32.Parse(tempArray[1]);
                cboBroadcastKey.SelectedIndex = Int32.Parse(tempArray[2]);

                List<string> splitList = new List<string>(tempArray[3].Split('|'));
                splitList.RemoveAt(0);

                for (int z = 0; z < characterCount; z++)                                                    //Loop through each character for setting targets.
                {
                    List<string> splitList2 = new List<string>(splitList[z].Split(':'));
                    bool check;
                    if (splitList2[0] == "True")
                        check = true;
                    else
                        check = false;

                    string target = "Session " + (z + 1);                                                   //Create our string for each target.
                    characters.Add(new CheckboxDropdown(check, target));                                    //Add all keys to our checkbox dropdown list.
                }

                cboTarget.ItemTemplate = System.Windows.Markup.XamlReader.Parse(
                    "<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">" +
                        "<StackPanel Orientation=\"Horizontal\">" +
                            "<CheckBox Margin=\"5\" IsChecked=\"{Binding IsChecked}\" />" +
                            "<TextBlock Margin=\"5\" Text=\"{Binding Text}\" />" +
                        "</StackPanel>" +
                    "</DataTemplate>") as DataTemplate;                                                     //Set our datatemplate for our custom checkbox dropdown.

                cboTarget.ItemsSource = characters;                                                         //Add all keys to our checkbox dropdown for usage.

                stkBroadcastName.Children.Add(txtHotkeyName);                                               //Add our custom hotkey name.
                stkBroadcastHotkey.Children.Add(cboHotkey);                                                 //Add our custom hotkey.
                stkBroadcastKey.Children.Add(cboBroadcastKey);                                              //Add our custom hotkey broadcast.
                stkBroadcastTarget.Children.Add(cboTarget);                                                 //Add our custom hotkey target.
            }

            #endregion Broadcast Load

            #region Extras Load

            //Set formation key and formation option to saved value. 8
            string form = loadList[8];
            List<string> formList = new List<string>(form.Split(';'));

            Int32.TryParse(formList[0], out tempNum);
            cboFormationKeys.SelectedIndex = tempNum;

            curFormation = formList[1];

            switch (curFormation)
            {
                case "Colume":
                    rdoColume.IsChecked = true;
                    break;

                case "Line":
                    rdoLine.IsChecked = true;
                    break;

                case "Wedge":
                    rdoWedge.IsChecked = true;
                    break;

                case "Vee":
                    rdoVee.IsChecked = true;
                    break;

                case "LFlank":
                    rdoLFlank.IsChecked = true;
                    break;

                case "RFlank":
                    rdoRFlank.IsChecked = true;
                    break;

                case "Diamond":
                    rdoDiamond.IsChecked = true;
                    break;
            }

            //Set lock key to saved value. 9
            string cmClick = loadList[9];
            List<string> cmClickList = new List<string>(cmClick.Split(';'));

            Int32.TryParse(cmClickList[0], out tempNum);
            cboCMKey.SelectedIndex = tempNum;

            Int32.TryParse(cmClickList[1], out tempNum);
            cboCMTargets.SelectedIndex = tempNum;

            #endregion Extras Load

            //File.Delete(configPath);
        }

        #endregion Save & Load

        #region Encryption

        // Encrypt or decrypt a file, saving the results in another file.
        public static void EncryptFile(string password, string in_file, string out_file)
        {
            CryptFile(password, in_file, out_file, true);
        }

        public static void DecryptFile(string password, string in_file, string out_file)
        {
            CryptFile(password, in_file, out_file, false);
        }

        public static void CryptFile(string password, string in_file, string out_file, bool encrypt)
        {
            // Create input and output file streams.
            using (FileStream in_stream =
                new FileStream(in_file, FileMode.Open, FileAccess.Read))
            {
                using (FileStream out_stream =
                    new FileStream(out_file, FileMode.Create,
                        FileAccess.Write))
                {
                    // Encrypt/decrypt the input stream into
                    // the output stream.
                    CryptStream(password, in_stream, out_stream, encrypt);
                }
            }
        }

        // Encrypt the data in the input stream into the output stream.
        public static void CryptStream(string password, Stream in_stream, Stream out_stream, bool encrypt)
        {
            // Make an AES service provider.
            AesCryptoServiceProvider aes_provider = new AesCryptoServiceProvider();

            // Find a valid key size for this provider.
            int key_size_bits = 0;
            for (int i = 1024; i > 1; i--)
            {
                if (aes_provider.ValidKeySize(i))
                {
                    key_size_bits = i;
                    break;
                }
            }
            Debug.Assert(key_size_bits > 0);
            Console.WriteLine("Key size: " + key_size_bits);

            // Get the block size for this provider.
            int block_size_bits = aes_provider.BlockSize;

            // Generate the key and initialization vector.
            byte[] key = null;
            byte[] iv = null;
            byte[] salt = { 0x0, 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0xF1, 0xF0, 0xEE, 0x21, 0x22, 0x45 };
            MakeKeyAndIV(password, salt, key_size_bits, block_size_bits, out key, out iv);

            // Make the encryptor or decryptor.
            ICryptoTransform crypto_transform;
            if (encrypt)
            {
                crypto_transform = aes_provider.CreateEncryptor(key, iv);
            }
            else
            {
                crypto_transform = aes_provider.CreateDecryptor(key, iv);
            }

            // Attach a crypto stream to the output stream.
            // Closing crypto_stream sometimes throws an
            // exception if the decryption didn't work
            // (e.g. if we use the wrong password).
            try
            {
                using (CryptoStream crypto_stream =
                    new CryptoStream(out_stream, crypto_transform,
                        CryptoStreamMode.Write))
                {
                    // Encrypt or decrypt the file.
                    const int block_size = 1024;
                    byte[] buffer = new byte[block_size];
                    int bytes_read;
                    while (true)
                    {
                        // Read some bytes.
                        bytes_read = in_stream.Read(buffer, 0, block_size);
                        if (bytes_read == 0) break;

                        // Write the bytes into the CryptoStream.
                        crypto_stream.Write(buffer, 0, bytes_read);
                    }
                } // using crypto_stream
            }
            catch
            {
            }

            crypto_transform.Dispose();
        }

        // Use the password to generate key bytes.
        private static void MakeKeyAndIV(string password, byte[] salt, int key_size_bits, int block_size_bits, out byte[] key, out byte[] iv)
        {
            Rfc2898DeriveBytes derive_bytes = new Rfc2898DeriveBytes(password, salt, 1000);

            key = derive_bytes.GetBytes(key_size_bits / 8);
            iv = derive_bytes.GetBytes(block_size_bits / 8);
        }

        #endregion Encryption
    }
}

#region Checkbox Dropdown

public class CheckboxDropdown
{
    public CheckboxDropdown(bool isChecked, string text)                                            //Create our object and give it two variables, bool and string.
    {
        IsChecked = isChecked;                                                                      //Set our bool to ischecked so we know if it is or not.
        Text = text;                                                                                //Set our text so we know what the name is.
    }

    public Boolean IsChecked                                                                        //Create our bool object to use the checkbox.
    {
        get;
        set;
    }

    public String Text                                                                              //Create our text object to hold our names.
    {
        get;
        set;
    }
}

#endregion Checkbox Dropdown