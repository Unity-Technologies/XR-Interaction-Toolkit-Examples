using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core
{

    public class GroupByShader : IGroupByFilter
    {
        public string GetName()
        {
            return "Shader";
        }

        public string GetDescription(GameObjectFilterInfo fi)
        {
            return "shader=" + fi.shaderName;
        }

        public int Compare(GameObjectFilterInfo a, GameObjectFilterInfo b)
        {
            return a.shaderName.CompareTo(b.shaderName);
        }
    }

}



