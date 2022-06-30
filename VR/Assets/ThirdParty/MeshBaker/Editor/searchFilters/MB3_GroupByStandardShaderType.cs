using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core
{

    public class MB3_GroupByStandardShaderType : IGroupByFilter
    {
        public string GetName()
        {
            return "Standard Rendering Mode";
        }

        public string GetDescription(GameObjectFilterInfo fi)
        {
            return "renderingMode=" + fi.standardShaderBlendModesName;
        }

        public int Compare(GameObjectFilterInfo a, GameObjectFilterInfo b)
        {
            return a.standardShaderBlendModesName.CompareTo(b.standardShaderBlendModesName);
        }
    }

}



