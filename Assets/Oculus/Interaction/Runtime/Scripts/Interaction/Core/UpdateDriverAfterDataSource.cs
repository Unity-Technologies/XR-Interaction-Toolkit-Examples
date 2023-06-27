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

using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Serialization;

namespace Oculus.Interaction
{
    /// <summary>
    /// An UpdateDriver that drives after an IDataSource has new input data available.
    /// </summary>
    public class UpdateDriverAfterDataSource : MonoBehaviour, IUpdateDriver
    {
        [SerializeField, Interface(typeof(IUpdateDriver))]
        private UnityEngine.Object _updateDriver;
        private IUpdateDriver UpdateDriver;

        [SerializeField, Interface(typeof(IDataSource))]
        private UnityEngine.Object _dataSource;
        private IDataSource DataSource;

        protected bool _started = false;

        protected virtual void Awake()
        {
            UpdateDriver = _updateDriver as IUpdateDriver;
            DataSource = _dataSource as IDataSource;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(UpdateDriver, nameof(UpdateDriver));
            this.AssertField(DataSource, nameof(DataSource));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                DataSource.InputDataAvailable += Drive;
                UpdateDriver.IsRootDriver = false;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                DataSource.InputDataAvailable -= Drive;
                UpdateDriver.IsRootDriver = true;
            }
        }

        public bool IsRootDriver { get; set; } = true;

        public void Drive()
        {
            UpdateDriver.Drive();
        }

        #region Inject

        public void InjectAllUpdateDriverAfterDataSource(IUpdateDriver updateDriver, IDataSource dataSource)
        {
            InjectUpdateDriver(updateDriver);
            InjectDataSource(dataSource);
        }

        public void InjectUpdateDriver(IUpdateDriver updateDriver)
        {
            UpdateDriver = updateDriver;
            _updateDriver = updateDriver as UnityEngine.Object;
        }

        public void InjectDataSource(IDataSource dataSource)
        {
            DataSource = dataSource;
            _dataSource = dataSource as UnityEngine.Object;
        }

        #endregion
    }
}
