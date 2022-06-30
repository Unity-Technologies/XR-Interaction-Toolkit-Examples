using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DigitalOpus.MB.Core;

namespace DigitalOpus.MB.MBEditor
{
    [System.Serializable]
    public class MB_ReplacePrefabsSettings : ScriptableObject
    {
        [System.Serializable]
        public class PrefabPair
        {
            public bool enabled = true;
            public GameObject srcPrefab;
            public GameObject targPrefab;
            public List<MB_ReplacePrefabsInScene.Error> objsWithErrors = new List<MB_ReplacePrefabsInScene.Error>();
        }

        public PrefabPair[] prefabsToSwitch = new PrefabPair[0];

        public bool reverseSrcAndTarg;

        public bool enforceSrcAndTargHaveSameStructure = true;
    }
}