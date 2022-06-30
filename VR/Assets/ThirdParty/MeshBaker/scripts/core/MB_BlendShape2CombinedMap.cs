using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigitalOpus.MB.Core
{
    public class MB_BlendShape2CombinedMap : MonoBehaviour
    {
        public SerializableSourceBlendShape2Combined srcToCombinedMap;

        public SerializableSourceBlendShape2Combined GetMap()
        {
            if (srcToCombinedMap == null)
            {
                srcToCombinedMap = new SerializableSourceBlendShape2Combined();
            }

            return srcToCombinedMap;
        }
    }
}
