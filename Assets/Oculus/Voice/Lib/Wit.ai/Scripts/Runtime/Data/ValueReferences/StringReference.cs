/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Meta.WitAi.Data.ValueReferences
{
    using UnityEngine;

    [System.Serializable]
    public class StringReference<T> : IStringReference where T : ScriptableObject, IStringReference
    {
        [SerializeField] private string stringValue;
        [SerializeField] private T stringObject;

        public string Value
        {
            get => stringObject ? stringObject.Value : stringValue;
            set
            {
                stringObject = null;
                stringValue = value;
            }
        }
    }

    public interface IStringReference
    {
        string Value { get; set; }
    }
}
