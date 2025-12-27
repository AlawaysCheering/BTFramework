using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Framework.Common.Debug
{
    public class DebugConfig 
    {
        public bool Enable = true;
        public bool LogError = true;
        public string LogPrefix = "";
        public bool ShowLogTime = true;
        public bool ShowFrameCount=true;
        public bool ShowThreadId = true;
        public bool ShowColorName = true;
        public bool LogFileEnable = true;
        public bool FpsShowEnable = true;
        public string LogFilePath => Application.persistentDataPath + "/";

        public string LogFileName => Application.productName + " " + DateTime.Now.ToString("yyyy-MM-dd HH-mm") + ".log";
    }
}
