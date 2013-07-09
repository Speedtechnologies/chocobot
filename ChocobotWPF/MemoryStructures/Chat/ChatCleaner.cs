﻿// ChatCleaner.cs


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Chocobot.MemoryStructures.Chat
{
    internal class ChatCleaner : INotifyPropertyChanged
    {
        #region Property Bindings

        private static bool _colorFound;
        private string _result;

        private bool ColorFound
        {
            get { return _colorFound; }
            set
            {
                _colorFound = value;
                RaisePropertyChanged();
            }
        }

        public string Result
        {
            get { return _result; }
            private set
            {
                _result = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Declarations
        public const RegexOptions DefaultOptions = RegexOptions.Compiled | RegexOptions.ExplicitCapture;
        private static readonly Regex Checks = new Regex(@"^00(20|21|23|27|28|46|47|48|49|5C)$", DefaultOptions);

        #endregion

        public ChatCleaner(byte[] bytes, CultureInfo ci, out bool jp)
        {
            Result = Process(bytes, ci, out jp)
                .Trim();
        }

        /// <summary>
        /// </summary>
        /// <param name="bytes"> </param>
        /// <param name="ci"> </param>
        /// <param name="jp"> </param>
        /// <returns> </returns>
        private string Process(byte[] bytes, CultureInfo ci, out bool jp)
        {
            jp = false;
            var line = HttpUtility.HtmlDecode(Encoding.UTF8.GetString(bytes.ToArray()))
                                  .Replace("  ", " ");
            try
            {
                var autoTranslateList = new List<byte>();
                var newList = new List<byte>();
                var check = Encoding.UTF8.GetString(bytes.Take(4)
                                                         .ToArray());
                for (var x = 0; x < bytes.Count(); x++)
                {
                    if (bytes[x] == 2)
                    {
                        //2 46 5 7 242 2 210 3
                        //2 29 1 3
                        var length = bytes[x + 2];
                        if (length > 1)
                        {
                            x = x + 3;
                            autoTranslateList.Add(Convert.ToByte('['));
                            while (bytes[x] != 3)
                            {
                                autoTranslateList.AddRange(Encoding.UTF8.GetBytes(bytes[x].ToString("X2")));
                                x++;
                            }
                            autoTranslateList.Add(Convert.ToByte(']'));
                            newList.AddRange(autoTranslateList.ToArray());
                            autoTranslateList.Clear();
                        }
                        else
                        {
                            x = x + 4;
                            newList.Add(32);
                            newList.Add(bytes[x]);
                        }
                    }
                    else
                    {
                        if (bytes[x] > 127)
                        {
                            jp = true;
                        }
                        newList.Add(bytes[x]);
                    }
                }
                var jpc = (ci.TwoLetterISOLanguageName == "ja");
                var cleaned = jpc ? HttpUtility.HtmlDecode(Encoding.UTF8.GetString(bytes.ToArray()))
                                               .Replace("  ", " ") : HttpUtility.HtmlDecode(Encoding.UTF8.GetString(newList.ToArray()))
                                                                                .Replace("  ", " ");
                autoTranslateList.Clear();
                newList.Clear();
                cleaned = cleaned.Replace("", "⇒");
                cleaned = cleaned.Replace("[01010101]", "");
                cleaned = cleaned.Replace("[CF010101]", "");
                line = cleaned;
            }
            catch (Exception ex)
            {
                
            }
            return line;
        }

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        #endregion
    }
}