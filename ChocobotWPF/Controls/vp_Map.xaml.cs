﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Algorithms;
using Chocobot.Datatypes;
using Chocobot.MemoryStructures.Character;
using Chocobot.Utilities.Memory;
using Chocobot.MemoryStructures.Map;
using Chocobot.Utilities.Navigation;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace Chocobot.Controls
{
    /// <summary>
    /// Interaction logic for vp_Map.xaml
    /// </summary>
    public partial class vp_Map : UserControl
    {
        private BitmapImage _map;

        private Character _user;
        private readonly List<Character> _players = new List<Character>();
        private readonly List<Character> _monsters = new List<Character>();
        private readonly List<Character> _npcs = new List<Character>();
        private readonly List<Character> _fate = new List<Character>();
        private MapInfo _mapinfo;

        private readonly NavigationHelper _navigation = new NavigationHelper();
        private readonly NavigationHelper _pathWalker = new NavigationHelper();

        public bool ShowMonsters = false;
        public bool ShowNpc = false;
        public bool ShowPlayers = false;

        private const short XPixelCount = 1024;
        private const short YPixelCount = 1024;
        private double _mapScaleX = 1.0;
        private double _mapScaleY = 1.0;

        private ushort _mapIndex;
        private MapNavArr _mapArr;
        private IPathFinder _pathFinder = null;
        private List<PathFinderNode> _selectedPath = null;
        private List<Coordinate> _selectedPathCoords; 
        private void RefreshMap()
        {

            _map = null;

            Uri filePath = new Uri(@"Maps\" + _mapIndex + ".gif", UriKind.Relative);
            _map = System.IO.File.Exists(@"Maps\" + _mapIndex + ".gif") ? new BitmapImage(filePath) : new BitmapImage(new Uri(@"Maps\0.gif", UriKind.Relative));
            _mapinfo = Map.Instance.GetMapInfo();

            _mapArr = _mapinfo.HasNavCoordinates ? _mapArr = new MapNavArr(_mapinfo.WaypointGroups) : null;
                
            
        }

        public vp_Map()
        {
            InitializeComponent();

            if (MemoryLocations.Database.Count == 0)
                return;

            Map.Instance.Refresh();
            _mapIndex = Map.Instance.MapIndex;

            RefreshMap();
            System.Diagnostics.Debug.Print(_mapIndex.ToString(CultureInfo.InvariantCulture));

            _navigation.WaypointIndexChanged += WaypointIndex_Changed;

        }

        private void WaypointIndex_Changed(object sender, int index)
        {
            
        }

        public void StartRecord()
        {
            
            _navigation.Record();
        }

        public void StopRecording()
        {
            _navigation.StopRecording();
            _mapinfo.WaypointGroups.Add(_navigation.Waypoints.ToList());
            _navigation.Waypoints = new ObservableCollection<Coordinate>();

            _mapinfo.HasNavCoordinates = true;

            _mapArr = _mapinfo.HasNavCoordinates ? _mapArr = new MapNavArr(_mapinfo.WaypointGroups) : null;
        }

        public void Refresh()
        {

            Map.Instance.Refresh();

            if(Map.Instance.MapIndex != _mapIndex)
            {
                _mapIndex = Map.Instance.MapIndex;
                RefreshMap();
            }

            InvalidateVisual();
        }

        private Coordinate WorldToMap(Coordinate coord)
        {

            Coordinate origin = new Coordinate((float)((XPixelCount / 2)),
                                   (float)((YPixelCount / 2) + 2), 0);
            Coordinate newCoord;

            try
            {
                newCoord = new Coordinate { X = (float)((coord.X * _mapinfo.XScale) + origin.X) * (float)_mapScaleX, Y = (float)((coord.Y * _mapinfo.YScale) + origin.Y) * (float)_mapScaleY };

            }
            catch (Exception)
            {

                newCoord = new Coordinate { X = (float)(0.0), Y = (float)((0.0)) };
            }
            
            return newCoord;

        }

        private Coordinate MapToWorld(Coordinate coord)
        {

            Coordinate origin = new Coordinate((float)((XPixelCount / 2)),
                                   (float)((YPixelCount / 2) + 2), 0);

            Coordinate newCoord;

            try
            {
                newCoord = new Coordinate { X = (float)(((coord.X / (float)_mapScaleX) - origin.X) / _mapinfo.XScale), Y = (float)(((coord.Y / (float)_mapScaleY) - origin.Y) / _mapinfo.YScale) };

            }
            catch (Exception)
            {

                newCoord = new Coordinate { X = (float)(0.0), Y = (float)((0.0)) };
            }

            return newCoord;

        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (MemoryLocations.Database.Count == 0)
                return;

            _mapScaleX = this.ActualWidth / XPixelCount;
            _mapScaleY = this.ActualHeight / YPixelCount;

            MemoryFunctions.GetCharacters(_monsters, _fate, _players, ref _user);
            MemoryFunctions.GetNPCs(_npcs);

            // Calculate the 2d map coordinates from 3d world coordinates.
            Coordinate userMapCoord = WorldToMap(_user.Coordinate);

            // Draw the map
            drawingContext.DrawImage(_map, new Rect(new Point(0, 0), new Size(this.ActualWidth, this.ActualHeight)));


            DrawPaths(drawingContext);
            DrawMonsters(drawingContext);
            DrawPlayers(drawingContext);
            DrawNPCs(drawingContext);

            // Draw the user coordinates
            drawingContext.DrawEllipse(Brushes.Cyan, new Pen(new SolidColorBrush(Colors.Cyan), 2), new Point(userMapCoord.X, userMapCoord.Y), 3, 3);


        }

        private void DrawNPCs(DrawingContext drawingContext)
        {
//// Draw the NPCs
            if (ShowNpc)
            {
                foreach (Character npc in _npcs)
                {
                    Coordinate npcMapCoord = WorldToMap(npc.Coordinate);

                    drawingContext.DrawEllipse(Brushes.DarkGray, new Pen(new SolidColorBrush(Colors.DarkGray), 1),
                                               new Point(npcMapCoord.X, npcMapCoord.Y), 1, 1);
                }
            }
        }

        private void DrawPlayers(DrawingContext drawingContext)
        {
            // Draw the Players
            if (ShowPlayers)
            {
                foreach (Character player in _players)
                {
                    Coordinate playerMapCoord = WorldToMap(player.Coordinate);

                    drawingContext.DrawEllipse(Brushes.Blue, new Pen(new SolidColorBrush(Colors.Blue), 1),
                                               new Point(playerMapCoord.X, playerMapCoord.Y), 1, 1);
                }
            }
        }


        private void DrawMonsters(DrawingContext drawingContext)
        {
            // Draw the monsters
            if (ShowMonsters)
            {
                foreach (Character monster in _monsters)
                {
                    Coordinate monsterMapCoord = WorldToMap(monster.Coordinate);

                    if (monster.IsFate)
                        drawingContext.DrawEllipse(Brushes.Magenta, new Pen(new SolidColorBrush(Colors.Lime), 6),
                                                   new Point(monsterMapCoord.X, monsterMapCoord.Y), 1, 1);
                    else
                        drawingContext.DrawEllipse(Brushes.Red, new Pen(new SolidColorBrush(Colors.Red), 1),
                                                   new Point(monsterMapCoord.X, monsterMapCoord.Y), 1, 1);
                }
            }
        }

        private void DrawPaths(DrawingContext drawingContext)
        {


            Pen p = new Pen(Brushes.MediumAquamarine, 2);
            Pen p2 = new Pen(Brushes.Magenta, 3);

            if (_pathWalker.IsPlaying == false)
            {
                foreach (List<Coordinate> waypointgroup in _mapinfo.WaypointGroups)
                {
                    for (int i = 5; i < waypointgroup.Count; i += 5)
                    {

                        Coordinate pnt1 = WorldToMap(waypointgroup[i - 5]);
                        Coordinate pnt2 = WorldToMap(waypointgroup[i]);

                        drawingContext.DrawLine(p, new Point(pnt1.X, pnt1.Y), new Point(pnt2.X, pnt2.Y));
                    }

                }


                for (int i = 5; i < _navigation.Waypoints.Count; i += 5)
                {
                    Coordinate pnt1 = WorldToMap(_navigation.Waypoints[i - 5]);
                    Coordinate pnt2 = WorldToMap(_navigation.Waypoints[i]);

                    drawingContext.DrawLine(p, new Point(pnt1.X, pnt1.Y), new Point(pnt2.X, pnt2.Y));
                }

            }


            if (_selectedPath != null)
            {

                for (int i = 5; i < _selectedPathCoords.Count; i += 5)
                {

                    Coordinate pnt1 = _selectedPathCoords[i - 5];
                    Coordinate pnt2 = _selectedPathCoords[i];


                    drawingContext.DrawLine(p2, new Point(pnt1.X, pnt1.Y), new Point(pnt2.X, pnt2.Y));
                }

            }

        }

        private void map_MouseUp(object sender, MouseButtonEventArgs e)
        {

            Point p = e.GetPosition(this);
            Coordinate clickCoordinate = MapToWorld(new Coordinate((float) p.X, (float) p.Y, 0));
            System.Diagnostics.Debug.Print(clickCoordinate.X + "," + clickCoordinate.Y);

            if(_mapArr != null)
            {
                CoordinateInt startIndex = _mapArr.GetClosestIndex(_user.Coordinate);
                CoordinateInt endIndex = _mapArr.GetClosestIndex(clickCoordinate);

                _pathFinder = new PathFinderFast(_mapArr.MapArr)
                                  {
                                      Formula = HeuristicFormula.Manhattan,
                                      Diagonals = true,
                                      HeavyDiagonals = false,
                                      HeuristicEstimate = (int) 2,
                                      PunishChangeDirection = false,
                                      TieBreaker = true,
                                      SearchLimit = (int) 100000,
                                      DebugProgress = false,
                                      DebugFoundPath = false
                                  };



                _selectedPath = _pathFinder.FindPath(new System.Drawing.Point(startIndex.X, startIndex.Y), new System.Drawing.Point(endIndex.X, endIndex.Y));

                _mapArr.Save(startIndex, endIndex);

                _selectedPathCoords = new List<Coordinate>();

                if (_selectedPath == null)
                {
                    MessageBox.Show("No Path Found");
                    return;
                }

                _selectedPath.Reverse();

               

                for (int i = 0; i < _selectedPath.Count; i += 1)
                {

                    Coordinate pnt1 =
                        WorldToMap(new Coordinate(_selectedPath[i].X / MapNavArr.ArrScale + _mapArr.Min.X,
                                                  _selectedPath[i].Y / MapNavArr.ArrScale + _mapArr.Min.Y, 0));

                    _selectedPathCoords.Add(pnt1);

                }


            }
            
        }


        public void PlaySelectedPath()
        {
            if (_selectedPath == null)
                return;

            _pathWalker.Waypoints.Clear();

            _pathWalker.Loop = false;
            _pathWalker.Sensitivity = 2.0;

            for (int i = 0; i < _selectedPath.Count; i++)
            {

                Coordinate pnt1 =
                    (new Coordinate(_selectedPath[i].X / MapNavArr.ArrScale + _mapArr.Min.X,
                                              _selectedPath[i].Y / MapNavArr.ArrScale + _mapArr.Min.Y, 0));

                _pathWalker.Waypoints.Add(pnt1);
            }

            _pathWalker.CleanWaypoints(3.0);


            _pathWalker.Save("D:\\test.nav");
            _pathWalker.Start();
        }



        public void StopSelectedPath()
        {


            _pathWalker.Stop();

        }

        public void SaveNav()
        {
            _mapinfo.SaveNav();
        }


    }
}
