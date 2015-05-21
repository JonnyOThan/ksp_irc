/*
KSPIRC - Internet Relay Chat plugin for Kerbal Space Program.
Copyright (C) 2013 Maik Schreiber

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KSPIRC
{
    internal class RectStorage
    {
        [Persistent]
        internal float x, y, width, height;

        public Rect Restore()
        {
            Rect ret = new Rect();
            ret.x = x;
            ret.y = y;
            ret.width = width;
            ret.height = height;

            return ret;
        }

        public RectStorage Store(Rect source)
        {
            this.x = source.x;
            this.y = source.y;
            this.width = source.width;
            this.height = source.height;

            return this;
        }
    }

    class IRCConfig : IPersistenceLoad, IPersistenceSave
    {

        [Persistent]
        internal string host = "irc.esper.net";

        [Persistent]
        internal int port = 5555;

        [Persistent]
        internal bool secure = false;

        [Persistent]
        internal string user = null;

        [Persistent]
        internal string serverPassword = null;

        [Persistent]
        internal string nick = "";

        [Persistent]
        internal bool forceSimpleRender = false;

        [Persistent]
        internal string channels = "";

        [Persistent]
        internal bool tts = false;

        [Persistent]
        internal int ttsVolume = 100;

        [Persistent]
        internal bool debug = false;

        [Persistent]
        internal Dictionary<string, RectStorage> windowRects = new Dictionary<string, RectStorage>();

        private string settingsFile = KSPUtil.ApplicationRootPath + "GameData/KSPIRC/irc.cfg";
            

        public IRCConfig()
        {
            ConfigNode settingsConfigNode = ConfigNode.Load(settingsFile) ?? new ConfigNode();
            ConfigNode.LoadObjectFromConfig(this, settingsConfigNode);
        }

        public void Save()
        {
            ConfigNode cnSaveWrapper = ConfigNode.CreateConfigFromObject(this);
            cnSaveWrapper.Save(settingsFile);
        }

        public void SetWindowRect(string name, Rect value)
        {
            RectStorage temp = new RectStorage();
            temp.Store(value);
            windowRects[name] = temp;
        }

        public bool GetWindowRect(string name, ref Rect destination)
        {
            RectStorage temp;
            if (windowRects.TryGetValue(name, out temp))
            {
                destination = temp.Restore();
                return true;
            }

            return false;
        }

        #region Interface Methods
        /// <summary>
        /// Wrapper for our overridable functions
        /// </summary>
        void IPersistenceLoad.PersistenceLoad()
        {
            OnDecodeFromConfigNode();
        }
        /// <summary>
        /// Wrapper for our overridable functions
        /// </summary>
        void IPersistenceSave.PersistenceSave()
        {
            OnEncodeToConfigNode();
        }   
        
        /// <summary>
        /// This overridable function executes whenever the object is loaded from a config node structure. Use this for complex classes that need decoding from simple confignode values
        /// </summary>
        public virtual void OnDecodeFromConfigNode() { }
        /// <summary>
        /// This overridable function executes whenever the object is encoded to a config node structure. Use this for complex classes that need encoding into simple confignode values
        /// </summary>
        public virtual void OnEncodeToConfigNode() { }
        #endregion
    }
}
