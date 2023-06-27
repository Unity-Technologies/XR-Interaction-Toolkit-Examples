/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// An UpdateDriverGroup updates a set of IUpdateDrivers a specified number of times per update.
    /// It acts as the root driver for a provided set of IUpdateDrivers.
    /// </summary>
    public class UpdateDriverGroup : MonoBehaviour, IUpdateDriver
    {
        public bool IsRootDriver { get; set; } = true;

        [SerializeField, Interface(typeof(IUpdateDriver))]
        private List<UnityEngine.Object> _updateDrivers;
        protected List<IUpdateDriver> Drivers;

        [SerializeField, Min(1)]
        private int _iterations = 3;

        #region Properties

        public int Iterations {
            get
            {
                return _iterations;
            }
            set
            {
                _iterations = value;
            }
        }

        #endregion

        protected virtual void Awake()
        {
            Drivers = _updateDrivers.ConvertAll(mono => mono as IUpdateDriver);
        }

        // Start is called before the first frame update
        protected virtual void Start()
        {
            this.AssertCollectionItems(Drivers, nameof(Drivers));
            this.AssertIsTrue(_iterations > 0, $"{AssertUtils.Nicify(nameof(_iterations))} must be bigger than {0}.");
        }

        // Update is called once per frame
        protected virtual void Update()
        {
            if (!IsRootDriver)
            {
                return;
            }

            Drive();
        }

        public void Drive()
        {
            for (int i = 0; i < _iterations; i++)
            {
                foreach (IUpdateDriver driver in Drivers)
                {
                    driver.Drive();
                }
            }
        }

        #region Inject

        public void InjectAllUpdateDriverGroup(List<IUpdateDriver> updateDrivers)
        {
            InjectUpdateDrivers(updateDrivers);
        }

        public void InjectUpdateDrivers(List<IUpdateDriver> updateDrivers)
        {
            Drivers = updateDrivers;
            _updateDrivers = updateDrivers.ConvertAll(driver => driver as UnityEngine.Object);
        }

        #endregion
    }
}
