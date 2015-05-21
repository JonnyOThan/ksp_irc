﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


// TODO: Change to your plugin's namespace here.
namespace KSPIRC
{
    
    /// <summary>
    /// The global tool bar manager.
    /// </summary>
    public class SpeechLibWrapper
    {
        private static SpeechLib.SpVoice speechVoice = null;
        private static int volume = 100;

        /// <summary>
        /// Whether the SpeechLib Plugin is available.
        /// </summary>
        public static bool IsSpeechLibAvailable()
        {
            return (Application.platform == RuntimePlatform.WindowsPlayer);
        }

        public static int GetVolume()
        {
            return volume;
        }

        public static void SetVolume(int level)
        {
            if (!IsSpeechLibAvailable())
            {
                return;
            }

            volume = level;

            initSpeechVoice();

            if (speechVoice.Volume != volume)
            {
                speechVoice.Volume = volume;
            }
        }

        public static void Speak(string message)
        {
            if (!IsSpeechLibAvailable())
            {
                return;
            }

            initSpeechVoice();

            speechVoice.Speak(message, SpeechLib.SpeechVoiceSpeakFlags.SVSFlagsAsync);
        }

        private static void initSpeechVoice()
        {
            if (speechVoice == null)
            {
                speechVoice = new SpeechLib.SpVoice();
                speechVoice.Volume = volume;
                speechVoice.SynchronousSpeakTimeout = 30;
                speechVoice.Rate = -1;
            }
            else
            {
                speechVoice.Volume = volume;
            }
        }
    }
}