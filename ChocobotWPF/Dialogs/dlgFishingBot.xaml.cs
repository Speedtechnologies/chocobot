﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Chocobot.Datatypes;
using Chocobot.MemoryStructures.Character;
using Chocobot.Utilities.Memory;
using Chocobot.Utilities.Navigation;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace Chocobot.Dialogs
{
    /// <summary>
    /// Interaction logic for dlgFishingBot.xaml
    /// </summary>
    public partial class dlgFishingBot : MetroWindow
    {


        // Record from start fishing spot to 2nd fishing spot.. then record path back...

        private readonly DispatcherTimer _fishMonitor = new DispatcherTimer();
        private readonly Stopwatch _fishtimer = new Stopwatch();
        private readonly NavigationHelper _fishPath1 = new NavigationHelper();
        private readonly NavigationHelper _fishPath2 = new NavigationHelper();
        private byte _currPath = 1;

        public dlgFishingBot()
        {
            InitializeComponent();

            _fishMonitor.Tick += thread_FishMonitor_Tick;
            _fishMonitor.Interval = new TimeSpan(0, 0, 0, 0, 350);

            _fishPath1.NavigationFinished += FishPath_NavigationFinished;
            _fishPath2.NavigationFinished += FishPath_NavigationFinished;


        }

        private void FishPath_NavigationFinished(object sender)
        {
            _fishMonitor.Start();
        }

        private void thread_FishMonitor_Tick(object sender, EventArgs e)
        {
            List<Character> monsters = new List<Character>();
            List<Character> fate = new List<Character>();
            List<Character> players = new List<Character>();
            Character user = null;

            MemoryFunctions.GetCharacters(monsters, fate, players, ref user);

            this.Title = user.Status.ToString("X") + user.Status.ToString();

            switch (user.Status)
            {

                case CharacterStatus.Idle:
                case CharacterStatus.Fishing_Idle:

                    if (_fishtimer.IsRunning == false)
                    {
                        _fishtimer.Reset();
                        _fishtimer.Start();
                    } else
                    {
                        if (_fishtimer.Elapsed.Seconds >= 8 && (_fishPath1.Waypoints.Count > 0 || _fishPath2.Waypoints.Count > 0))
                        {

                            _fishtimer.Reset();
                            _fishMonitor.Stop();

                            while (user.Status != CharacterStatus.Idle)
                            {
                                Utilities.Keyboard.KeyBoardHelper.KeyPress(Keys.D4);
                                user.Refresh();
                            }

                            if (_currPath == 1)
                            {
                                if (_fishPath1.Waypoints.Count == 0)
                                    return;

                                _fishPath1.Start();
                                _currPath = 2;
                            } else
                            {
                                if (_fishPath2.Waypoints.Count == 0)
                                    return;

                                _fishPath2.Start();
                                _currPath = 1;
                            }

                            return;
                        }
                    }

                    Utilities.Keyboard.KeyBoardHelper.KeyPress(Keys.D2);
                    break;
                case CharacterStatus.Fishing_Cast1:
                case CharacterStatus.Fishing_Cast2:
                case CharacterStatus.Fishing_Cast3:
                case CharacterStatus.Fishing_Cast4:
                    if (_fishtimer.IsRunning)
                    {
                        _fishtimer.Reset();
                        _fishtimer.Stop();
                    }

                    break;
                case CharacterStatus.Fishing_FishOnHook:
                case CharacterStatus.Fishing_FishOnHook2:
                case CharacterStatus.Fishing_FishOnHook3:
                case CharacterStatus.Fishing_FishOnHook4:
                    Utilities.Keyboard.KeyBoardHelper.KeyPress(Keys.D3);
                    break;
            }
        }

        private void btn_StartFishing_Click(object sender, RoutedEventArgs e)
        {
            _fishMonitor.Start();
        }

        private void btn_StopFishing_Click(object sender, RoutedEventArgs e)
        {
            _fishMonitor.Stop();
            _fishPath1.Stop();
            _fishPath2.Stop();
        }

        private void btn_LoadPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == false)
                return;

            _fishPath1.Waypoints.Clear();
            _fishPath1.Load(dlg.FileName);
            _fishPath1.Loop = false;

            lbl_PathWaypoints.Content = "Waypoints: " + _fishPath1.Waypoints.Count.ToString(CultureInfo.InvariantCulture);

        }

        private void btn_LoadPath2_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == false)
                return;

            _fishPath2.Waypoints.Clear();
            _fishPath2.Load(dlg.FileName);
            _fishPath2.Loop = false;

            lbl_Path2Waypoints.Content = "Waypoints: " + _fishPath2.Waypoints.Count.ToString(CultureInfo.InvariantCulture);
        }
    }
}