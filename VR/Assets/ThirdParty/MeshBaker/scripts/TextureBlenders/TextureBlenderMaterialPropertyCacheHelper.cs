using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace DigitalOpus.MB.Core
{
    public class TextureBlenderMaterialPropertyCacheHelper
    {
        private struct MaterialPropertyPair
        {
            public Material material;
            public string property;

            public MaterialPropertyPair(Material m, string prop)
            {
                material = m;
                property = prop;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is MaterialPropertyPair)) return false;
                MaterialPropertyPair b = (MaterialPropertyPair)obj;
                if (!material.Equals(b.material)) return false;
                if (property != b.property) return false;
                return true;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        private Dictionary<MaterialPropertyPair, object> nonTexturePropertyValuesForSourceMaterials = new Dictionary<MaterialPropertyPair, object>();

        private bool AllNonTexturePropertyValuesAreEqual(string prop)
        {
            bool foundFirst = false;
            object firstVal = null;
            foreach (MaterialPropertyPair k in nonTexturePropertyValuesForSourceMaterials.Keys)
            {
                if (k.property.Equals(prop))
                {
                    if (!foundFirst)
                    {
                        firstVal = nonTexturePropertyValuesForSourceMaterials[k];
                        foundFirst = true;
                    }
                    else
                    {
                        if (!firstVal.Equals(nonTexturePropertyValuesForSourceMaterials[k]))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public void CacheMaterialProperty(Material m, string property, object value)
        {
            nonTexturePropertyValuesForSourceMaterials[new MaterialPropertyPair(m, property)] = value;
        }

        public object GetValueIfAllSourceAreTheSameOrDefault(string property, object defaultValue)
        {
            if (AllNonTexturePropertyValuesAreEqual(property))
            {
                foreach (MaterialPropertyPair k in nonTexturePropertyValuesForSourceMaterials.Keys)
                {
                    if (k.property.Equals(property))
                    {
                        return nonTexturePropertyValuesForSourceMaterials[k];
                    }
                }
            }

            return defaultValue;
        }
    }
}