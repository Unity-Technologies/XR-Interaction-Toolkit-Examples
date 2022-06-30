//----------------------------------------------
//            MeshBaker
// Copyright © 2011-2012 Ian Deane
//----------------------------------------------
using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using DigitalOpus.MB.Core;
using UnityEditor;

namespace DigitalOpus.MB.MBEditor
{
    [CustomEditor(typeof(MB3_MeshBaker))]
    [CanEditMultipleObjects]
    public class MB3_MeshBakerEditor : Editor
    {
        MB3_MeshBakerEditorInternal mbe = new MB3_MeshBakerEditorInternal();

        [MenuItem("GameObject/Create Other/Mesh Baker/TextureBaker and MeshBaker", false, 100)]
        public static GameObject CreateNewMeshBaker()
        {
            MB3_TextureBaker[] mbs = (MB3_TextureBaker[])GameObject.FindObjectsOfType(typeof(MB3_TextureBaker));
            Regex regex = new Regex(@"\((\d+)\)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
            int largest = 0;
            try
            {
                for (int i = 0; i < mbs.Length; i++)
                {
                    Match match = regex.Match(mbs[i].name);
                    if (match.Success)
                    {
                        int val = Convert.ToInt32(match.Groups[1].Value);
                        if (val >= largest)
                            largest = val + 1;
                    }
                }
            }
            catch (Exception e)
            {
                if (e == null) e = null; //Do nothing supress compiler warning
            }
            GameObject nmb = new GameObject("TextureBaker (" + largest + ")");
            nmb.transform.position = Vector3.zero;
            MB3_TextureBaker tb = nmb.AddComponent<MB3_TextureBaker>();
            tb.packingAlgorithm = MB2_PackingAlgorithmEnum.MeshBakerTexturePacker;
            MB3_MeshBakerGrouper mbg = nmb.AddComponent<MB3_MeshBakerGrouper>();
            GameObject meshBaker = new GameObject("MeshBaker");
            MB3_MeshBaker mb = meshBaker.AddComponent<MB3_MeshBaker>();
            meshBaker.transform.parent = nmb.transform;
            mb.meshCombiner.settingsHolder = mbg;
            return nmb.gameObject;
        }

        [MenuItem("GameObject/Create Other/Mesh Baker/MeshBaker", false, 100)]
        public static GameObject CreateNewMeshBakerOnly()
        {
            MB3_MeshBaker[] mbs = (MB3_MeshBaker[])GameObject.FindObjectsOfType(typeof(MB3_MeshBaker));
            Regex regex = new Regex(@"\((\d+)\)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
            int largest = 0;
            try
            {
                for (int i = 0; i < mbs.Length; i++)
                {
                    Match match = regex.Match(mbs[i].name);
                    if (match.Success)
                    {
                        int val = Convert.ToInt32(match.Groups[1].Value);
                        if (val >= largest)
                            largest = val + 1;
                    }
                }
            }
            catch (Exception e)
            {
                if (e == null) e = null; //Do nothing supress compiler warning
            }
            GameObject meshBaker = new GameObject("MeshBaker (" + largest + ")");
            meshBaker.AddComponent<MB3_MeshBaker>();
            return meshBaker.gameObject;
        }

        void OnEnable()
        {
            mbe.OnEnable(serializedObject);
        }

        void OnDisable()
        {
            mbe.OnDisable();
        }

        public override void OnInspectorGUI()
        {
            mbe.OnInspectorGUI(serializedObject, (MB3_MeshBakerCommon)target, targets, typeof(MB3_MeshBakerEditorWindow));
        }


    }
}
