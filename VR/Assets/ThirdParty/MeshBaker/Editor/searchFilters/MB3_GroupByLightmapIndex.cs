using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.Core
{

    public class GroupByLightmapIndex : IGroupByFilter
    {
        public string GetName()
        {
            return "Lightmap Index";
        }

        public string GetDescription(GameObjectFilterInfo fi)
        {
            return "lightmapIndex=" + fi.lightmapIndex;
        }

        public int Compare(GameObjectFilterInfo a, GameObjectFilterInfo b)
        {
            return b.lightmapIndex - a.lightmapIndex;
        }
    }
}



